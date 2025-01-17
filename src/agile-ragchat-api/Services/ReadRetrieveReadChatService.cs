﻿// Copyright (c) Microsoft. All rights reserved.

using Azure.Core;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

namespace MinimalApi.Services;
#pragma warning disable SKEXP0011 // Mark members as static
#pragma warning disable SKEXP0001 // Mark members as static
public class ReadRetrieveReadChatService
{
    private readonly ISearchService _searchClient;
    private readonly Kernel _kernel;
    private readonly IConfiguration _configuration;
    private readonly IComputerVisionService? _visionService;
    //private readonly TokenCredential? _tokenCredential;


    private static string? GetEnvVar(string key) => Environment.GetEnvironmentVariable(key);

    public ReadRetrieveReadChatService(
        ISearchService searchClient,
        OpenAIClient client,
        IConfiguration configuration,
        IComputerVisionService? visionService = null,
        TokenCredential? tokenCredential = null)
    {



        _searchClient = searchClient;
        var kernelBuilder = Kernel.CreateBuilder();

        var deployedModelName = GetEnvVar("AZURE_OPENAI_API_DEPLOYMENT_NAME");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);
        var embeddingModelName = GetEnvVar("AZURE_OPENAI_API_EMBEDDINGS_DEPLOYMENT_NAME");
        if (!string.IsNullOrEmpty(embeddingModelName))
        {
            var endpoint = GetEnvVar("AZURE_OPENAI_ENDPOINT") ?? throw new ArgumentNullException() ;            
            var openAiAPIKey = GetEnvVar("AZURE_OPENAI_API_KEY") ?? throw new ArgumentNullException();
            
            //kernelBuilder = kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingModelName, endpoint, tokenCredential ?? new DefaultAzureCredential());
            kernelBuilder = kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingModelName, endpoint, openAiAPIKey);
            //kernelBuilder = kernelBuilder.AddAzureOpenAIChatCompletion(deployedModelName, endpoint, tokenCredential ?? new DefaultAzureCredential());
            kernelBuilder = kernelBuilder.AddAzureOpenAIChatCompletion(deployedModelName, endpoint, openAiAPIKey);
        }

        _kernel = kernelBuilder.Build();
        _configuration = configuration;
        _visionService = visionService;
        //_tokenCredential = tokenCredential;
    }


    public async Task<ChatAppResponse> ReplyAsync(
        ChatMessage[] historyMessageArray,
        RequestOverrides? overrides,
        CancellationToken cancellationToken = default)
    {
        var top = overrides?.Top ?? 3;
        var useSemanticCaptions = overrides?.SemanticCaptions ?? false;
        var useSemanticRanker = overrides?.SemanticRanker ?? false;
        var excludeCategory = overrides?.ExcludeCategory ?? null;
        var filter = excludeCategory is null ? null : $"category ne '{excludeCategory}'";
        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        var embeddingGenerationService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        float[]? embeddings = null;
        var questionString = historyMessageArray.LastOrDefault(m => m.IsUser)?.Content is { } userQuestion
            ? userQuestion
            : throw new InvalidOperationException("Use question is null");

        string[]? followUpQuestionList = null;
        if (overrides?.RetrievalMode != RetrievalMode.Text && embeddingGenerationService is not null)
        {
            embeddings = (await embeddingGenerationService.GenerateEmbeddingAsync(questionString, cancellationToken: cancellationToken)).ToArray();
        }

        // step 1
        // use llm to get query if retrieval mode is not vector
        string? searchQueryText = null;
        if (overrides?.RetrievalMode != RetrievalMode.Vector)
        {
            searchQueryText = await GenerateSearchQueryFromLLMAsync(chatCompletionService, questionString, searchQueryText, cancellationToken);
        }

        // step 2
        // use query to search related docs
        var documentContentList = await _searchClient.QueryDocumentsAsync(searchQueryText, embeddings, overrides, cancellationToken);

        string documentContentsString = string.Empty;
        if (documentContentList.Length == 0)
        {
            documentContentsString = "no source available.";
        }
        else
        {
            documentContentsString = string.Join("\r", documentContentList.Select(x =>$"{x.Title}:{x.Content}"));
        }

        //step 2.5
        // retrieve images if _visionService is available
        SupportingImageRecord[] ? images = default;
        if (_visionService is not null)
        {
            var queryEmbeddings = await _visionService.VectorizeTextAsync(searchQueryText ?? questionString, cancellationToken);
            images = await _searchClient.QueryImagesAsync(searchQueryText, queryEmbeddings.vector, overrides, cancellationToken);
        }

        // step 3
        // put together related docs and conversation history to generate answer
        var answerChatHistory = new ChatHistory(
            "You are a system assistant who helps the company employees with their questions. Be brief in your answers");

        // add chat history
        foreach (var message in historyMessageArray)
        {
            if (message.IsUser)
            {
                answerChatHistory.AddUserMessage(message.Content);
            }
            else
            {
                answerChatHistory.AddAssistantMessage(message.Content);
            }
        }

        
        if (false)
        {           
        }
        else
        {
            var prompt = @$" ## Source ##
{documentContentsString}
## End ##

You answer needs to be a json object with the following format.
{{
    ""answer"": // the answer to the question, add a source reference to the end of each sentence. e.g. Apple is a fruit [reference1.pdf][reference2.pdf]. If no source available, put the answer as I don't know.
    ""thoughts"": // brief thoughts on how you came up with the answer, e.g. what sources you used, what you thought about, etc.
}}";
            answerChatHistory.AddUserMessage(prompt);
        }

        var promptExecutingSetting = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1024,
            Temperature = overrides?.Temperature ?? 0.7,
            StopSequences = [],
        };

        // get answer
        ChatMessageContent answerChatMessageContent = await chatCompletionService.GetChatMessageContentAsync(
                       answerChatHistory,
                       promptExecutingSetting,
                       cancellationToken: cancellationToken);
        var answerJson = answerChatMessageContent.Content ?? throw new InvalidOperationException("Failed to get search query");
        var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
        var ans = answerObject.GetProperty("answer").GetString() ?? throw new InvalidOperationException("Failed to get answer");
        var thoughts = answerObject.GetProperty("thoughts").GetString() ?? throw new InvalidOperationException("Failed to get thoughts");

        // step 4
        // add follow up questions if requested
        if (overrides?.SuggestFollowupQuestions is true)
        {
            var followUpQuestionChat = new ChatHistory(@"You are a helpful AI assistant");
            followUpQuestionChat.AddUserMessage($@"Generate three follow-up question based on the answer you just generated.
# Answer
{ans}

# Format of the response
Return the follow-up question as a json string list. Don't put your answer between ```json and ```, return the json string directly.
e.g.
[
    ""What is the deductible?"",
    ""What is the co-pay?"",
    ""What is the out-of-pocket maximum?""
]");

            var followUpQuestions = await chatCompletionService.GetChatMessageContentAsync(
                followUpQuestionChat,
                promptExecutingSetting,
                cancellationToken: cancellationToken);

            var followUpQuestionsJson = followUpQuestions.Content ?? throw new InvalidOperationException("Failed to get search query");
            var followUpQuestionsObject = JsonSerializer.Deserialize<JsonElement>(followUpQuestionsJson);
            var followUpQuestionsList = followUpQuestionsObject.EnumerateArray().Select(x => x.GetString()!).ToList();
            foreach (var followUpQuestion in followUpQuestionsList)
            {
                ans += $" <<{followUpQuestion}>> ";
            }

            followUpQuestionList = followUpQuestionsList.ToArray();
        }

        var responseMessage = new ResponseMessage("assistant", ans);
        var responseContext = new ResponseContext(
            DataPointsContent: documentContentList.Select(x => new SupportingContentRecord(x.Title, x.Content)).ToArray(),
            DataPointsImages: images?.Select(x => new SupportingImageRecord(x.Title, x.Url)).ToArray(),
            FollowupQuestions: followUpQuestionList ?? Array.Empty<string>(),
            Thoughts: new[] { new Thoughts("Thoughts", thoughts) });

        var choice = new ResponseChoice(
            Index: 0,
            Message: responseMessage,
            Context: responseContext,
            CitationBaseUrl: _configuration.ToCitationBaseUrl());

        return new ChatAppResponse(new[] { choice });
    }

    private static async Task<string?> GenerateSearchQueryFromLLMAsync(IChatCompletionService chat, string question, string? query, CancellationToken cancellationToken)
    {
        var getQueryChatHistory = new ChatHistory(@"You are a helpful AI assistant, generate search query for followup question.
Make your respond simple and precise. Return the query only, do not return any other text.
e.g.
Northwind Health Plus AND standard plan.
standard plan AND dental AND employee benefit.
");

        getQueryChatHistory.AddUserMessage(question);

        var result = await chat.GetChatMessageContentAsync(
            getQueryChatHistory,
            cancellationToken: cancellationToken);

        query = result.Content ?? throw new InvalidOperationException("Failed to get search query");
        return query;
    }
}

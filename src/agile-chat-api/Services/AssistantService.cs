using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Cosmos;
using System.Reflection;
using Config = agile_chat_api.Configurations.AppConfigs;

public interface IAssistantService
{
    Task<IEnumerable<Assistant>> GetAllAsync();
    Task<Assistant?> GetByIdAsync(Guid id);
    Task CreateAsync(Assistant assistant);
    Task UpdateAsync(Assistant assistant);
    Task DeleteAsync(Guid id);
}

public class AssistantService : IAssistantService
{
    private readonly Container _container;
    private readonly ILogger<AssistantService> _logger;

    public AssistantService(ILogger<AssistantService> logger)
    {
        _logger = logger;
        const string containerName = "assistants";

        var cosmosClient = new CosmosClient(Config.CosmosEndpoint, Config.CosmosKey);
        _container = cosmosClient.GetContainer(Config.CosmosDBName, containerName);
    }

    public async Task<IEnumerable<Assistant>> GetAllAsync()
    {
        var results = new List<Assistant>();

        try
        {
            var query = _container.GetItemQueryIterator<Assistant>();

            while (query.HasMoreResults)
            {
                var response =await query.ReadNextAsync();
                results.AddRange([.. response]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all assistants.");
            throw;
        }

        return results;
    }

    public async Task< Assistant?> GetByIdAsync(Guid id)
    {
        try
        {
            var query = _container.GetItemLinqQueryable<Assistant>(true)
                .Where(t => t.Id == id)
                .AsEnumerable()
                .FirstOrDefault();

            return await Task.FromResult(query);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while retrieving the assistant with ID {id}.");
            throw;
        }
    }

    public async Task CreateAsync(Assistant assistant)
    {
        try
        {
            assistant.Id = Guid.NewGuid();
            assistant.CreatedAt = DateTime.UtcNow;
            assistant.UpdatedAt = DateTime.UtcNow;
            await _container.CreateItemAsync(assistant, new PartitionKey(assistant.CreatedBy));
        }
        catch (CosmosException ex)
        {
            HandleCosmosException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating an assistant.");
            throw;
        }
    }

    public async Task UpdateAsync(Assistant assistant)
    {
        var existingAssistant = await GetByIdAsync(assistant.Id);
        if (existingAssistant != null)
        {
            existingAssistant.Name = assistant.Name;
            existingAssistant.Description = assistant.Description;
            existingAssistant.Type = assistant.Type;
            existingAssistant.Status = assistant.Status;
            existingAssistant.Greeting = assistant.Greeting;
            existingAssistant.SystemMessage = assistant.SystemMessage;
            existingAssistant.Group = assistant.Group;
            existingAssistant.Index = assistant.Index;
            existingAssistant.Folder = assistant.Folder;
            existingAssistant.Temperature = assistant.Temperature;
            existingAssistant.TopP = assistant.TopP;
            existingAssistant.MaxResponseToken = assistant.MaxResponseToken;
            existingAssistant.Strictness = assistant.Strictness;
            existingAssistant.PastMessages = assistant.PastMessages;
            existingAssistant.DocumentLimit = assistant.DocumentLimit;
            existingAssistant.UpdatedAt = DateTime.UtcNow;
            existingAssistant.Tools = assistant.Tools;
            try
            {
                await _container.ReplaceItemAsync(existingAssistant, assistant.Id.ToString(),
                    new PartitionKey(existingAssistant.CreatedBy));
            }
            catch (CosmosException ex)
            {
                HandleCosmosException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating the assistant with ID {assistant.Id}.");
                throw;
            }
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var existingAssistant =await GetByIdAsync(id);
        if (existingAssistant != null)
        {
            try
            {
                await _container.DeleteItemAsync<Assistant>(id.ToString(), new PartitionKey(existingAssistant.CreatedBy));
            }
            catch (CosmosException ex)
            {
                HandleCosmosException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting the assistant with ID {id}.");
                throw;
            }
        }
    }

    private void HandleCosmosException(CosmosException ex)
    {
        _logger.LogError(ex, "A Cosmos DB error occurred.");
        throw new InvalidOperationException("An error occurred while accessing Cosmos DB.", ex);
    }
}
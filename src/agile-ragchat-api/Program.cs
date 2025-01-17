﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;


var builder = WebApplication.CreateBuilder(args);

// Ensure the environment variables are loaded so they can be referenced during configuration
DotNetEnv.Env.Load();

// Define the CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            //todo: load the allowed origins from the environment
            policy.WithOrigins("http://localhost:3000", "https://agilechat-dev-webapp.azurewebsites.net") // Allow specific origins
                  .AllowAnyMethod() // Allow all HTTP methods (GET, POST, etc.)
                  .AllowAnyHeader() // Allow all headers (Authorization, Content-Type, etc.)
                  .AllowCredentials(); // If needed, allow credentials (cookies, authorization headers)
        });
});


builder.Configuration.ConfigureAzureKeyVault();

// See: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//todo:adam - removed to resolve error
//builder.Services.AddSwaggerGen();
builder.Services.AddOutputCache();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
//builder.Services.AddCrossOriginResourceSharing(); //cors
builder.Services.AddAzureServices();
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN-HEADER"; options.FormFieldName = "X-CSRF-TOKEN-FORM"; });
builder.Services.AddHttpClient();

builder.Services.AddAzureAdAuth();

// Register services
builder.Services.AddScoped<EchoChatService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    static string? GetEnvVar(string key) => Environment.GetEnvironmentVariable(key);

    //todo:adam - removed to resolve error
    //builder.Services.AddStackExchangeRedisCache(options =>
    //{
    //    var name = builder.Configuration["AzureRedisCacheName"] +
    //        ".redis.cache.windows.net";
    //    var key = builder.Configuration["AzureRedisCachePrimaryKey"];
    //    var ssl = "true";


    //    if (GetEnvVar("REDIS_HOST") is string redisHost)
    //    {
    //        name = $"{redisHost}:{GetEnvVar("REDIS_PORT")}";
    //        key = GetEnvVar("REDIS_PASSWORD");
    //        ssl = "false";
    //    }

    //    if (GetEnvVar("AZURE_REDIS_HOST") is string azureRedisHost)
    //    {
    //        name = $"{azureRedisHost}:{GetEnvVar("AZURE_REDIS_PORT")}";
    //        key = GetEnvVar("AZURE_REDIS_PASSWORD");
    //        ssl = "false";
    //    }

    //    options.Configuration = $"""
    //        {name},abortConnect=false,ssl={ssl},allowAdmin=true,password={key}
    //        """;
    //    options.InstanceName = "content";


    //});

    // set application telemetry
    if (GetEnvVar("APPLICATIONINSIGHTS_CONNECTION_STRING") is string appInsightsConnectionString && !string.IsNullOrEmpty(appInsightsConnectionString))
    {
        //todo:adam - removed to resolve error
        //builder.Services.AddApplicationInsightsTelemetry((option) =>
        //{
        //    option.ConnectionString = appInsightsConnectionString;
        //});
    }
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    //todo:adam - removed to resolve error
    //app.UseSwagger();
    //app.UseSwaggerUI();

}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseOutputCache();
app.UseRouting();

//Add Auth
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

// Apply the CORS Policy
app.UseCors("AllowSpecificOrigins"); // Apply the CORS policy defined above globally to all routes


//app.UseAntiforgery();
app.MapRazorPages();
app.MapControllers();

//todo: adam - I added this. verify loatoin
app.MapGet("/echo/{prompt}", (string prompt) =>
{
    return Results.Ok(prompt);
});
app.MapGet("/echochat/{prompt}", async (string prompt, EchoChatService chatService) =>
{
    var history = new ChatMessage[]
    {
        new ChatMessage("user", prompt)
    };

    var response = await chatService.ReplyAsync(history, prompt, null);
    return Results.Ok(response);
});


app.MapGet("/simplechat/{prompt}", async (string prompt, SimpleChatService chatService) =>
{
    var history = new ChatMessage[]
    {
        new ChatMessage("user", prompt)
    };

    var response = await chatService.ReplyAsync(history);
    return Results.Ok(response);
});

//used to test resopnses - the service uses a post
app.MapGet("/chatoverdata/{prompt}", async (string prompt, ReadRetrieveReadChatService chatService) =>
{
    var history = new ChatMessage[]
    {
        new ChatMessage("user", prompt)
    };

    var response = await chatService.ReplyAsync(history, null);
    return Results.Ok(response);
});

//used to test resopnses - the service uses a post
app.MapPost("/chatoverdata", async (HttpContext context, ReadRetrieveReadChatService chatService) =>
{
     try
    {
        // Deserialize the request body into an appropriate object

        var history = await context.Request.ReadFromJsonAsync<ChatMessage[]>();
        //var history = request?.History;
       // var history = request?.History;

        var question = history?.LastOrDefault(m => m.IsUser)?.Content is { } userQuestion
            ? userQuestion
            : throw new InvalidOperationException("Use question is null");
        if (history == null || string.IsNullOrEmpty(question))
        {
            return Results.BadRequest("Invalid request. The message cannot be empty.");
        }

        // Create a ChatMessage array using the prompt received in the request
        // var history = new ChatMessage[]
        // {
        //     new ChatMessage("user", userQuestion)
        // };

        // Set up request overrides if any (for simplicity, using default for now)
       // var overrides = request.Overrides ?? new RequestOverrides();

        // Call the chat service to get a response
        var response = await chatService.ReplyAsync(history, null);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
});

//app.Use(next => context =>
//{
//    var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
//    var tokens = antiforgery.GetAndStoreTokens(context);
//    context.Response.Cookies.Append("XSRF-TOKEN", tokens?.RequestToken ?? string.Empty, new CookieOptions() { HttpOnly = false });
//    return next(context);
//});
app.MapFallbackToFile("index.html");

app.MapApi();

app.Run();

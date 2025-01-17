﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace agile_chat_api.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddAzureAdAuth(this IServiceCollection services)
    {
        var azureAdConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"AzureAd:Instance", "https://login.microsoftonline.com/"},
                {"AzureAd:ClientId", Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")},
                {"AzureAd:TenantId", Environment.GetEnvironmentVariable("AZURE_TENANT_ID")},
                {"AzureAd:AllowWebApiToBeAuthorizedByACL", "True"}
            })
            .Build();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(azureAdConfig);

        return services;
    }
}

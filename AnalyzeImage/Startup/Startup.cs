using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Extensions.DependencyInjection;
using RonyParm.Azure.AnalyzeImage;
using Azure.Storage.Blobs;
using System;
using Microsoft.Azure.Cosmos;

[assembly: FunctionsStartup(typeof(Startup))]

namespace RonyParm.Azure.AnalyzeImage
{
    /// <summary>
    /// Register services as singletons and utilize constructor based dependency injection
    /// https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
    /// </summary>
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton((s) => {
                string cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");
                return new CosmosClient(cosmosConnectionString);
            });

            builder.Services.AddSingleton((s) => {
                string blobConnectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
                return new BlobServiceClient(blobConnectionString);
            });

            builder.Services.AddSingleton((s) => {
                string computerVisionKey = Environment.GetEnvironmentVariable("ComputerVisionKey");
                string computerVisionEndpoint = Environment.GetEnvironmentVariable("ComputerVisionEndpoint");
                return new ComputerVisionClient(new ApiKeyServiceClientCredentials(computerVisionKey))
                {
                    Endpoint = computerVisionEndpoint
                };
            });
        }
    }
}
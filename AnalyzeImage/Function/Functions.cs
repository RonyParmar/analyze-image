using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Linq;
using Microsoft.Azure.Cosmos;

namespace RonyParm.Azure.AnalyzeImage
{
    public class Functions
    {
        private const string PersonDetectedObject = "person";
        private const string EventAnaylsisTypePeopleDocumentType = "people";

        private readonly ComputerVisionClient _computerVisionClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CosmosClient _cosmosClient;

        public Functions(ComputerVisionClient computerVisionClient, BlobServiceClient blobServiceClient, CosmosClient cosmosClient)
        {
            _computerVisionClient = computerVisionClient;
            _blobServiceClient = blobServiceClient;
            _cosmosClient = cosmosClient;
        }

        [FunctionName("DetectPeople")]
        [FixedDelayRetry(5, "00:00:10")]
        public void DetectPeople([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation("DetectPeople - function start");

            try
            {
                StorageBlobCreatedEventData blobCreatedEvent = eventGridEvent.Data.ToObjectFromJson<StorageBlobCreatedEventData>();

                string blobName = GetBlobNameFromUrl(blobCreatedEvent.Url);

                string blobContainerName = Environment.GetEnvironmentVariable("BlobContainerName");
                Stream blob = DownloadBlobContent(blobContainerName, blobName);

                // Analyze image for objects
                var visualFeatureTypes = new List<VisualFeatureTypes?>() { VisualFeatureTypes.Objects };
                ImageAnalysis imageAnaylsis = _computerVisionClient.AnalyzeImageInStreamAsync(blob, visualFeatureTypes).Result;

                // Get total count of detected people
                IList<DetectedObject> detectedObjects = imageAnaylsis.Objects;
                int totalPeople = detectedObjects.Where(o => o.ObjectProperty == PersonDetectedObject).Count();

                StoreImageAnalysisTypePeople(blobCreatedEvent.Url, totalPeople, log);

                log.LogInformation("DetectPeople - function end");
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
            }
        }

        private string GetBlobNameFromUrl(string blobUrl)
        {
            return new Uri(blobUrl).Segments.LastOrDefault();
        }

        private Stream DownloadBlobContent(string blobContainerName, string blobName)
        {
            BlobContainerClient blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobContainerName);

            BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

            var blobStream = new MemoryStream();
            blobClient.DownloadTo(blobStream);

            return blobStream;
        }

        private ImageAnalysisTypePeople StoreImageAnalysisTypePeople(string blobUrl, int totalPeople, ILogger log)
        {
            string cosmosDatabaseId = Environment.GetEnvironmentVariable("DatabaseId");
            string cosmosContainerId = Environment.GetEnvironmentVariable("ContainerId");
            Container container = _cosmosClient.GetContainer(cosmosDatabaseId, cosmosContainerId);

            ImageAnalysisTypePeople imageAnalysisTypePeople = new()
            {
                id = Guid.NewGuid().ToString(),
                blobUrl = blobUrl,
                totalPeople = totalPeople,
                type = EventAnaylsisTypePeopleDocumentType,
                createdDateTime = DateTime.UtcNow
            };

            return container.CreateItemAsync(imageAnalysisTypePeople, new PartitionKey(imageAnalysisTypePeople.id)).Result;
        }
    }
}

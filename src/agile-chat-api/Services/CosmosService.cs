﻿using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Models;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using Config = agile_chat_api.Configurations.AppConfigs;
using Constants = agile_chat_api.Configurations.Constants;

namespace Services
{
    public interface ICosmosService
    {
        /// <summary>
        /// Files the metadata exists asynchronous.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folder">The folder.</param>
        /// <returns></returns>
        Task<bool> FileMetadataExistsAsync(string fileName, string folder);

        /// <summary>
        /// Saves the file metadata to cosmos database asynchronous.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="blobUrl">The BLOB URL.</param>
        /// <param name="folderName">Name of the folder.</param>
        /// <returns></returns>
        Task SaveFileMetadataToCosmosDbAsync(IFormFile file, object blobUrl, string folderName);

        /// <summary>
        /// Gets the file by identifier asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        Task<FileMetadata?> GetFileByIdAsync(string id);

        /// <summary>
        /// Gets the bulk files asynchronous.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<FileMetadata?>> GetFileUploadsAsync();

        /// <summary>
        /// Deletes the file metadata from cosmos using file name asynchronous.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folder">The folder.</param>
        /// <returns></returns>
        Task DeleteFileByNameFromCosmosAsync(string fileName, string folder);

        /// <summary>
        /// Deletes the bulk file metadata from cosmos asynchronous.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <returns></returns>
        Task DeleteBulkFileMetadataFromCosmosAsync(IEnumerable<string> files);
    }
}

namespace Services
{
    public class CosmosService : ICosmosService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _cosmosContainer;

        public CosmosService()
        {
            _cosmosClient = new CosmosClient(Config.CosmosEndpoint, Config.CosmosKey);
            _cosmosContainer = EnsureCosmosContainerExists().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ensures the cosmos container exists.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private async Task<Container> EnsureCosmosContainerExists()
        {
            var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(Config.CosmosDBName);
            ContainerResponse containerResponse = await database.Database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = Config.FileContainerName,
                PartitionKeyPath = Constants.FileContainerPartitionKeyPath
            });
            return containerResponse.Container;
        }

        /// <summary>
        /// Files the metadata exists asynchronous.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folder">The folder.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task<bool> FileMetadataExistsAsync(string fileName, string folder)
        {
            var queryable = _cosmosContainer.GetItemLinqQueryable<FileMetadata>();

            // Use LINQ to filter documents based on FileName and Folder
            var filteredQuery = queryable
                .Where(file => file.FileName == fileName && file.Folder == folder)
                .ToFeedIterator();

            try
            {
                List<FileMetadata> results = [];
                while (filteredQuery.HasMoreResults)
                {
                    FeedResponse<FileMetadata> response = await filteredQuery.ReadNextAsync();
                    results.AddRange(response);
                }
                if (results.Count > 0)
                {
                    return true;
                }
                FileMetadata? data = results.FirstOrDefault();
                Console.WriteLine($"Document count: {data}");
                return false;

            }
            catch (Exception)
            {
                Console.WriteLine($"Error checking if file metadata exists for {fileName} in folder {folder}", fileName, folder);
                return false;
            }
        }

        /// <summary>
        /// Saves the file metadata to cosmos database asynchronous.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="blobUrl">The BLOB URL.</param>
        /// <param name="folderName">Name of the folder.</param>
        public async Task SaveFileMetadataToCosmosDbAsync(IFormFile file, object blobUrl, string folderName)
        {
            try
            {
                var dateTimeString = DateTimeOffset.Now.ToString("dd-MM-yyyy HH:mm:ss");
                var fileMetadata = new FileMetadata
                {
                    id = Guid.NewGuid().ToString(), // Unique identity ID
                    FileName = Path.GetFileName(file.FileName),
                    BlobUrl = blobUrl,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    Folder = folderName,
                    SubmittedOn = dateTimeString
                };
                await _cosmosContainer.CreateItemAsync(fileMetadata, new PartitionKey(fileMetadata.id));
            }
            catch (Exception)
            {
                Console.WriteLine($"Error saving file metadata for {file.FileName} in folder {folderName}", file.FileName, folderName);
            }
        }

        /// <summary>
        /// Gets the file by identifier asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public async Task<FileMetadata?> GetFileByIdAsync(string id)
        {
            try
            {
                var response = await _cosmosContainer.ReadItemAsync<FileMetadata>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"File with ID {id} not found.", id);
                return null;
            }
            catch (Exception)
            {
                Console.WriteLine($"Error retrieving file with ID {id}", id);
            }
            return null;
        }

        /// <summary>
        /// Gets the file upload asynchronous.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<FileMetadata?>> GetFileUploadsAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var results = new List<FileMetadata>();
            try
            {
                using var feedIterator = _cosmosContainer.GetItemQueryIterator<FileMetadata>(query);
                while (feedIterator.HasMoreResults)
                {
                    results.AddRange(await feedIterator.ReadNextAsync());
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Error retrieving all file metadata.");
                return []; // Ensure non-null return
            }
            return results;
        }

        /// <summary>
        /// Deletes the file with retry asynchronous.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="failedDeletions">The failed deletions.</param>
        private async Task DeleteFileWithRetryAsync(string fileId, ConcurrentBag<string> failedDeletions)
        {
            try
            {
                await _cosmosContainer.DeleteItemAsync<FileMetadata>(fileId, new PartitionKey(fileId));
                Console.WriteLine($"File with ID {fileId} deleted successfully.", fileId);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine($"Rate limit hit for ID {fileId}. Retrying after delay...", fileId);
                await Task.Delay(ex.RetryAfter ?? TimeSpan.FromSeconds(1));
                failedDeletions.Add(fileId);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"File with ID {fileId} not found. Skipping deletion.", fileId);
            }
            catch (Exception)
            {
                Console.WriteLine($"Error deleting file with ID {fileId}", fileId);
                failedDeletions.Add(fileId);
            }
        }

        /// <summary>
        /// Deletes the file metadata from cosmos using file name asynchronous.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folder">The folder.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task DeleteFileByNameFromCosmosAsync(string fileName, string folder)
        {
            var query = new QueryDefinition("SELECT c.id FROM c WHERE c.FolderName = @folder AND c.FileName = @fileName")
                .WithParameter("@folder", folder)
                .WithParameter("@fileName", fileName);
            try
            {
                using var feedIterator = _cosmosContainer.GetItemQueryIterator<dynamic>(query);
                while (feedIterator.HasMoreResults)
                {
                    var items = await feedIterator.ReadNextAsync();
                    var deleteTasks = items.Select(item => _cosmosContainer.DeleteItemAsync<FileMetadata>(item.id.ToString(), new PartitionKey(item.id.ToString())));
                    await Task.WhenAll((IEnumerable<Task>)deleteTasks);
                }

                Console.WriteLine($"File(s) with name {fileName} in folder {folder} deleted successfully.", fileName, folder);
            }
            catch (Exception)
            {
                Console.WriteLine($"Error deleting file by name {fileName} in folder {folder}", fileName, folder);
                throw;
            }
        }

        /// <summary>
        /// Deletes the bulk file metadata from cosmos asynchronous.
        /// </summary>
        /// <param name="files">The files.</param>
        public async Task DeleteBulkFileMetadataFromCosmosAsync(IEnumerable<string> files)
        {
            var failedDeletions = new ConcurrentBag<string>();
            var deleteTasks = files.Select(id => DeleteFileWithRetryAsync(id, failedDeletions));
            await Task.WhenAll(deleteTasks);
            if (!failedDeletions.IsEmpty)
            {
                Console.WriteLine($"Retrying failed deletions for {failedDeletions.Count} files.", failedDeletions.Count);
                foreach (var fileId in failedDeletions)
                {
                    await DeleteFileWithRetryAsync(fileId, failedDeletions);
                }
            }
        }
    }
}
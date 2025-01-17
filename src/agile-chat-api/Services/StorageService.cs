﻿using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Models;
using Config = agile_chat_api.Configurations.AppConfigs;
using Constants = agile_chat_api.Configurations.Constants;

namespace Services
{
    public interface IStorageService
    {
        /// <summary>
        /// Files the exists in BLOB asynchronous.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folderName">Name of the folder.</param>
        /// <returns></returns>
        /// TODO Edit XML Comment Template for FileExistsInBlobAsync
        Task<bool> FileExistsInBlobAsync(string fileName, string indexName, string folderName);

        /// <summary>
        /// Uploads the file to BLOB asynchronous.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        Task<string> UploadFileToBlobAsync(IFormFile file, string indexName, string folderName);

        /// <summary>
        /// Gets the BLOB URL asynchronous.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folder">The folder.</param>
        /// <returns></returns>
        string GetBlobURLAsync(string indexName, string fileName, string folder);

        /// <summary>
        /// Deletes the file from BLOB asynchronous.
        /// </summary>
        /// <param name="blobUrl">The BLOB URL.</param>
        /// <returns></returns>
        Task DeleteFileFromBlobAsync(FileMetadata file);
        
        Task DeleteAllFilesFromIndexAsync(string indexName);
        
        Task<List<string>> GetHighLevelFolders();
    }
}

namespace Services
{
    public class StorageService : IStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;
        private readonly ILogger<StorageService> _logger;

        public StorageService(ILogger<StorageService> logger)
        {
            _logger = logger;
            BlobServiceClient blobServiceClient = new(Config.BlobStorageConnectionString);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(Constants.BlobStorageContainerName);
            _blobContainerClient.CreateIfNotExists();
        }

        /// <summary>
        /// Files the exists in BLOB asynchronous.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folderName">Name of the folder.</param>
        /// <returns></returns>
        /// TODO Edit XML Comment Template for FileExistsInBlobAsync
        public async Task<bool> FileExistsInBlobAsync(string fileName, string indexName, string folderName)
        {
            string blobPath = $"{indexName}" + (!string.IsNullOrWhiteSpace(folderName) ? $"/{folderName}/{fileName}" : $"/{fileName}");
            BlobClient _blobClient = _blobContainerClient.GetBlobClient(blobPath);
            try
            {
                // Check if the blob exists
                return await _blobClient.ExistsAsync();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError("Error checking if file exists: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Gets the BLOB URL asynchronous.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="folder">The folder.</param>
        /// <returns></returns>
        public string GetBlobURLAsync(string indexName, string fileName, string folderName)
        {
            string blobPath = $"{indexName}" + (!string.IsNullOrWhiteSpace(folderName) ? $"/{folderName}/{fileName}" : $"/{fileName}");
            BlobClient _blobClient = _blobContainerClient.GetBlobClient(blobPath);
            return _blobClient.Uri.ToString();
        }

        /// <summary>
        /// Uploads the file to BLOB asynchronous.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Error uploading file: {file.FileName}</exception>
        public async Task<string> UploadFileToBlobAsync(IFormFile file, string indexName, string folderName)
        {
            try
            {
                string fileName = Path.GetFileName(file.FileName);
                string blobPath = $"{indexName}" + (!string.IsNullOrWhiteSpace(folderName) ? $"/{folderName}/{fileName}" : $"/{fileName}");
                BlobClient _blobClient = _blobContainerClient.GetBlobClient(blobPath);

                // Check if both folder and file exist
                bool fileExists = await _blobClient.ExistsAsync();
                bool folderExists = false;
                await foreach (BlobItem _blobItem in _blobContainerClient.GetBlobsAsync(prefix: folderName))
                {
                    folderExists = true;
                    break;
                }
                // If both folder and file exist, do nothing
                if (folderExists && fileExists)
                {
                    return null!;
                }
                await _blobClient.UploadAsync(file.OpenReadStream(), true);
                return _blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error uploading file: {file.FileName}", ex);
            }
        }

        /// <summary>
        /// Deletes the file from BLOB asynchronous.
        /// </summary>
        /// <param name="file"></param>
        /// <exception cref="System.InvalidOperationException">Error deleting file from blob storage: {file.FileName}</exception>
        public async Task DeleteFileFromBlobAsync(FileMetadata file)
        {
            try
            {
                string fileName = Path.GetFileName(file.FileName);
                string blobPath = $"{file.IndexName}" + (!string.IsNullOrWhiteSpace(file.Folder) ? $"/{file.Folder}/{fileName}" : $"/{fileName}");
                BlobClient _blobClient = _blobContainerClient.GetBlobClient(blobPath);
                await _blobClient.DeleteIfExistsAsync();
                _logger.LogInformation("Blob '{Name}' deleted successfully from container '{Path}'.", fileName, blobPath);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error deleting file from blob storage: {Name}, Message: {Message} StackTrace: {StackTrace}", file.FileName, ex.Message, ex.StackTrace);
                throw new InvalidOperationException($"Error deleting file from blob storage: {file.FileName}", ex);
            }
        }

        public async Task DeleteAllFilesFromIndexAsync(string indexName)
        {
            await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(prefix: indexName))
            {
                var blobClient = _blobContainerClient.GetBlobClient(blobItem.Name);
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
            }
        }
        
        public async Task<List<string>> GetHighLevelFolders()
        {
            var delimiter = "/";

            var results = _blobContainerClient.GetBlobsByHierarchyAsync(delimiter: delimiter);
            var folders = new List<string>();

            await foreach (var result in results)
            {
                if(result.IsPrefix)
                    folders.Add(result.Prefix.Replace("/", ""));
            }

            return folders;
        }
    }
}
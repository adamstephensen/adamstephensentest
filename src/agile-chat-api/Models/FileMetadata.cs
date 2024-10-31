﻿namespace Models
{
    public class FileMetadata
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public required string id { get; set; } // Unique identity identifier in Cosmos DB
        /// <summary>
        /// Gets or sets the file identifier.
        /// </summary>
        /// <value>
        /// The file identifier.
        /// </value>
        public required Guid FileId { get; set; } // Unique identity identifier in Cosmos DB
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public required string FileName { get; set; }
        /// <summary>
        /// Gets or sets the BLOB URL.
        /// </summary>
        /// <value>
        /// The BLOB URL.
        /// </value>
        public required object BlobUrl { get; set; }
        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        public string? ContentType { get; set; }
        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public long Size { get; set; }
        /// <summary>
        /// Gets or sets the folder.
        /// </summary>
        /// <value>
        /// The folder.
        /// </value>
        public string? Folder { get; set; }
        /// <summary>
        /// Gets or sets the submitted on.
        /// </summary>
        /// <value>
        /// The submitted on.
        /// </value>
        public string? SubmittedOn { get; set; }
    }
}
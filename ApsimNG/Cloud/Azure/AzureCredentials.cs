using System;

namespace ApsimNG.Cloud.Azure
{
    /// <summary>
    /// Represents the credentials required to connect to Azure Batch and Storage services.
    /// </summary>
    public class AzureCredentials
    {
        /// <summary>
        /// Gets the Azure Batch service URL.
        /// </summary>
        public string BatchUrl { get; }

        /// <summary>
        /// Gets the Azure Batch account name.
        /// </summary>
        public string BatchAccount { get; }

        /// <summary>
        /// Gets the Azure Batch account key.
        /// </summary>
        public string BatchKey { get; }

        /// <summary>
        /// Gets the Azure Storage account name.
        /// </summary>
        public string StorageAccount { get; }

        /// <summary>
        /// Gets the Azure Storage account key.
        /// </summary>
        public string StorageKey { get; }

        /// <summary>
        /// Creates a new instance of the AzureCredentials class.
        /// </summary>
        /// <param name="batchUrl">The Azure Batch service URL.</param>
        /// <param name="batchAccount">The Azure Batch account name.</param>
        /// <param name="batchKey">The Azure Batch account key.</param>
        /// <param name="storageAccount">The Azure Storage account name.</param>
        /// <param name="storageKey">The Azure Storage account key.</param>
        public AzureCredentials(string batchUrl, string batchAccount, string batchKey,
                              string storageAccount, string storageKey)
        {
            BatchUrl = batchUrl ?? throw new ArgumentNullException(nameof(batchUrl));
            BatchAccount = batchAccount ?? throw new ArgumentNullException(nameof(batchAccount));
            BatchKey = batchKey ?? throw new ArgumentNullException(nameof(batchKey));
            StorageAccount = storageAccount ?? throw new ArgumentNullException(nameof(storageAccount));
            StorageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Batch;
using Azure.ResourceManager.Batch.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Gtk;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using UserInterface.Views;

namespace ApsimNG.Cloud.Azure
{
    /// <summary>
    /// Provides modern authentication and resource management capabilities for Azure 
    /// services. This service handles the creation and management of Azure resources 
    /// required for APSIM batch processing.
    /// </summary>
    /// <remarks>
    /// This service uses Azure.Identity for authentication, which supports multiple 
    /// authentication methods including:
    /// - Azure CLI credentials
    /// - Visual Studio credentials
    /// - Azure PowerShell credentials
    /// - Managed Identity
    /// 
    /// The service will attempt to use existing resources when possible and create new 
    /// ones when needed. All resources are created with secure defaults and follow 
    /// Azure best practices.
    /// </remarks>
    public class ModernAzureAuthService
    {
        /// <summary>
        /// Maximum length of azure resource names, as dicated by the API.
        /// </summary>
        private const int maxNameLength = 23;

        /// <summary>
        /// Options for configuring how Azure resources are set up and managed.
        /// </summary>
        /// <remarks>
        /// This class provides options to control whether new Azure resources should be 
        /// created or existing ones should be used. When creating new resources, it 
        /// also allows specifying the preferred region.
        /// </remarks>
        public class SetupOptions
        {
            /// <summary>
            /// If true, new resources will be created if they don't exist. If
            /// false, an exception will be thrown if resources don't exist.
            /// Furthermore, if false, the properties below must be non-null, or
            /// an exception will be thrown.
            /// </summary>
            public bool CreateNew { get; set; } = true;

            /// <summary>
            /// Gets or sets the name of an existing resource group to use.
            /// If null, and CreateNew is true, a new resource group will be created.
            /// If null, and CreateNew is false, an exception will be thrown.
            /// </summary>
            public string ExistingResourceGroup { get; set; }

            /// <summary>
            /// Gets or sets the name of an existing batch account to use.
            /// If null, and CreateNew is true, a new batch account will be created.
            /// If null, and CreateNew is false, an exception will be thrown.
            /// </summary>
            public string ExistingBatchAccount { get; set; }

            /// <summary>
            /// Gets or sets the name of an existing storage account to use.
            /// If null, and CreateNew is true, a new storage account will be created.
            /// If null, and CreateNew is false, an exception will be thrown.
            /// </summary>
            public string ExistingStorageAccount { get; set; }

            /// <summary>
            /// Gets or sets the preferred Azure region for new resources.
            /// </summary>
            public string PreferredRegion { get; set; } = "australiasoutheast";
        }

        private readonly InteractiveBrowserCredential browserCredential;
        private ArmClient armClient;
        private const string tokenCacheName = "apsim.azure";

        /// <summary>
        /// Creates a new instance of the ModernAzureAuthService class.
        /// </summary>
        /// <remarks>
        /// The constructor will attempt to authenticate using InteractiveBrowserCredential, 
        /// which tries multiple authentication methods in sequence. The user must be 
        /// authenticated through one of the supported methods for this to succeed.
        /// </remarks>
        public ModernAzureAuthService()
        {
            // Initial client for tenant discovery - no specific tenant
            var opts = new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = tokenCacheName }
            };
            browserCredential = new InteractiveBrowserCredential(opts);

            armClient = new ArmClient(browserCredential);
        }

        private void WriteMessage(string message)
        {
            Console.WriteLine(message);
            MainView.MasterView.ShowMessage(message, Models.Core.MessageType.Information, false, false, false);
        }

        /// <summary>
        /// Create a credential object which reuses an AuthenticationRecord from
        /// a successful previous authentication.
        /// </summary>
        /// <param name="auth">The authentication record.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The credential.</returns>
        /// <remarks>
        /// If we don't reuse the auth record, we would need to re-authenticate.
        /// If relying on the interactive browser approach, this would mean that
        /// when we use the client created from the credential returned from
        /// this method, a browser would open again, and the user would need to
        /// manually login again.
        /// </remarks>
        private TokenCredential GetCredential(AuthenticationRecord auth, string tenantId = null)
        {
            var browserOptions = new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = tokenCacheName },
                AuthenticationRecord = auth,
                TenantId = tenantId
            };

            return new InteractiveBrowserCredential(browserOptions);
        }

        /// <summary>
        /// Gets Azure credentials using modern authentication methods. This will either 
        /// use existing Azure resources or create new ones as needed.
        /// </summary>
        /// <param name="options">Optional setup options. If not provided, new resources 
        /// will be created.</param>
        /// <returns>Azure credentials for both Batch and Storage services.</returns>
        /// <remarks>
        /// This method requires the user to be authenticated with Azure through one of:
        /// - Azure CLI
        /// - Visual Studio
        /// - Azure PowerShell
        /// - Managed Identity
        /// </remarks>
        public async Task<AzureCredentials> GetCredentialsAsync(SetupOptions options = null)
        {
            options ??= new SetupOptions();

            // Validate setup options.
            if (!options.CreateNew && string.IsNullOrEmpty(options.ExistingResourceGroup))
                throw new ArgumentException("Must specify an existing resource group when not creating a new batch account");
            if (!options.CreateNew && string.IsNullOrEmpty(options.ExistingBatchAccount))
                throw new ArgumentException("Must specify an existing batch account when not creating a new batch account");
            if  (!options.CreateNew && string.IsNullOrEmpty(options.ExistingStorageAccount))
                throw new ArgumentException("Must specify an existing storage account when not creating a new batch account");
            if (string.IsNullOrEmpty(options.PreferredRegion))
                throw new ArgumentException("Must specify a preferred region");

            // Authenticate once, and reuse the auth record in the creation of
            // future clients.
            WriteMessage("Authenticating...");
            AuthenticationRecord auth = await browserCredential.AuthenticateAsync();

            // First get all available tenants
            WriteMessage("Checking available Azure tenants...");

            var tenants = armClient.GetTenants().ToList();
            WriteMessage($"Discovered {tenants.Count} tenants");

            if (tenants.Count == 0)
                throw new InvalidOperationException("No Azure tenants found. Please ensure you have access to at least one Azure Active Directory tenant.");

            // Get subscriptions across all tenants using their specific TenantIds
            List<SubscriptionResource> allSubscriptions = new List<SubscriptionResource>();
            
            foreach (var tenant in tenants)
            {
                try
                {
                    Guid? tenantId = tenant.Data.TenantId;
                    WriteMessage($"Discovering subscriptions for tenant {tenantId} ({tenant.Data.DisplayName})");
                    TokenCredential tenantCredential = GetCredential(auth, tenantId.ToString());
                    var tenantClient = new ArmClient(tenantCredential);
                    var tenantSubscriptions = tenantClient.GetSubscriptions().ToList();
                    allSubscriptions.AddRange(tenantSubscriptions);
                }
                catch (Exception ex)
                {
                    // Log but continue - some tenants might not be accessible
                    WriteMessage($"Warning: Could not access subscriptions for tenant {tenant.Data.DisplayName}: {ex.Message}");
                }
            }

            allSubscriptions = allSubscriptions.DistinctBy(s => s.Id).ToList();
            WriteMessage($"Discovered {allSubscriptions.Count} subscriptions");

            if (allSubscriptions.Count == 0)
                throw new InvalidOperationException("No Azure subscriptions found. Please ensure you have access to at least one subscription.");

            if (allSubscriptions.Count > 1)
            {
                // TODO: need to implement subscription selection.
                var subscriptionNames = string.Join("\n", allSubscriptions.Select(s => $"- {s.Data.DisplayName} ({s.Data.SubscriptionId})"));
                throw new InvalidOperationException(
                    $"Multiple Azure subscriptions found. Please select one using the Azure portal or Azure CLI.\n" +
                    $"Available subscriptions:\n{subscriptionNames}\n" +
                    $"TODO: implement selection of susbcription via GUI.");
            }

            SubscriptionResource subscription = allSubscriptions[0];
            WriteMessage($"Using subscription: {subscription.Data.DisplayName}");

            // Create or discover the resource group.
            ResourceGroupResource resourceGroup = await EnsureResourceGroupExistsAsync(
                subscription,
                options.ExistingResourceGroup ?? GetResourceGroupName(),
                options.PreferredRegion,
                options.CreateNew);

            // Create or discover a storage account.
            StorageAccountResource storageAccount = await EnsureStorageAccountExistsAsync(
                resourceGroup,
                options.ExistingStorageAccount ?? GetStorageAccountName());

            // Create or discover the batch account.
            BatchAccountResource batchAccount = await EnsureBatchAccountExistsAsync(
                resourceGroup,
                options.ExistingBatchAccount ?? GetBatchAccountName(),
                storageAccount);

            WriteMessage("Getting batch account keys...");
            Response<BatchAccountKeys> keys = await batchAccount.GetKeysAsync();

            WriteMessage("Getting storage account keys...");
            Page<StorageAccountKey> keyPage = await storageAccount.GetKeysAsync()
                .AsPages()
                .FirstAsync();
            StorageAccountKey firstKey = keyPage.Values.First();

            return new AzureCredentials(
                batchUrl: batchAccount.Data.AccountEndpoint,
                batchAccount: batchAccount.Data.Name,
                batchKey: keys.Value.Primary,
                storageAccount: storageAccount.Data.Name,
                storageKey: firstKey.Value);
        }

        /// <summary>
        /// Ensures a resource group exists, creating it if necessary.
        /// </summary>
        /// <param name="subscription">The subscription to create the group in.</param>
        /// <param name="resourceGroupName">Name of the resource group.</param>
        /// <param name="location">Azure region for the resource group.</param>
        /// <param name="createNew">Whether to create a new resource group if the specified one does not exist.</param>
        /// <returns>The existing or newly created resource group.</returns>
        private async Task<ResourceGroupResource> EnsureResourceGroupExistsAsync(
            SubscriptionResource subscription,
            string resourceGroupName,
            string location,
            bool createNew)
        {
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
            try
            {
                return await resourceGroups.GetAsync(resourceGroupName);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                if (!createNew)
                    throw new Exception($"Resource group {resourceGroupName} does not exist", ex);

                return await CreateResourceGroupAsync(
                    subscription,
                    resourceGroups,
                    resourceGroupName,
                    location);
            }
        }

        /// <summary>
        /// Ensures a storage account exists, creating it if necessary.
        /// </summary>
        /// <param name="group">Resource group to create the account in.</param>
        /// <param name="accountName">Name of the storage account.</param>
        /// <returns>The existing or newly created storage account.</returns>
        private async Task<StorageAccountResource> EnsureStorageAccountExistsAsync(
            ResourceGroupResource group,
            string accountName)
        {
            StorageAccountCollection storageAccounts = group.GetStorageAccounts();
            try
            {
                return await storageAccounts.GetAsync(accountName);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return await CreateStorageAccountAsync(group, storageAccounts, accountName);
            }
        }

        /// <summary>
        /// Ensures a batch account exists, creating it if necessary.
        /// </summary>
        /// <param name="group">Resource group to create the account in.</param>
        /// <param name="batchAccountName">Name of the batch account.</param>
        /// <param name="storageAccount">Storage account to associate with the batch 
        /// account.</param>
        /// <returns>The existing or newly created batch account.</returns>
        private async Task<BatchAccountResource> EnsureBatchAccountExistsAsync(
            ResourceGroupResource group,
            string batchAccountName,
            StorageAccountResource storageAccount)
        {
            BatchAccountCollection batchAccounts = group.GetBatchAccounts();
            try
            {
                return await batchAccounts.GetAsync(batchAccountName);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return await CreateBatchAccountAsync(group, batchAccounts, batchAccountName, storageAccount);
            }
        }

        private async Task<ResourceGroupResource> CreateResourceGroupAsync(
            SubscriptionResource subscription,
            ResourceGroupCollection resourceGroups,
            string name,
            string location)
        {
            WriteMessage($"Resource group does not exist. Creating a new group: {name}...");
            try
            {
                // Create new resource group.
                ResourceGroupData parameters = new ResourceGroupData(location);
                ArmOperation<ResourceGroupResource> operation = await resourceGroups
                    .CreateOrUpdateAsync(
                        WaitUntil.Completed,
                        name,
                        parameters);
                return operation.Value;
            }
            catch (RequestFailedException ex)
            {
                throw new Exception($"Failed to create resource group {name}", ex);
            }
        }

        /// <summary>
        /// Creates a storage account.
        /// </summary>
        /// <param name="group">The resource group to create the account in.</param>
        /// <param name="storageAccounts">The collection of storage accounts in the group.</param>
        /// <param name="name">The name of the storage account to create.</param>
        /// <returns>The newly created storage account.</returns>
        /// <exception cref="Exception">If the creation fails.</exception>
        private async Task<StorageAccountResource> CreateStorageAccountAsync(
            ResourceGroupResource group,
            StorageAccountCollection storageAccounts,
            string name)
        {
            WriteMessage($"Storage account does not exist. Creating storage account: {name}...");
            try
            {
                // Account doesn't exist, create it
                StorageAccountCreateOrUpdateContent parameters = 
                    new StorageAccountCreateOrUpdateContent(
                        new StorageSku(StorageSkuName.StandardLrs),
                        StorageKind.StorageV2,
                        group.Data.Location)
                    {
                        EnableHttpsTrafficOnly = true,
                        MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_2
                    };

                ArmOperation<StorageAccountResource> operation = 
                    await storageAccounts.CreateOrUpdateAsync(
                        WaitUntil.Completed,
                        name,
                        parameters);
                return operation.Value;
            }
            catch (Exception error)
            {
                throw new Exception($"Failed to create storage account: {name}", error);
            }
        }

        /// <summary>
        /// Create a batch account with the specified name.
        /// </summary>
        /// <param name="group">The resource group to which the account will be added.</param>
        /// <param name="batchAccounts">The collection of batch accounts in the group.</param>
        /// <param name="name">The name of the batch account to create.</param>
        /// <param name="storageAccount">The storage account to associate with the batch account.</param>
        /// <returns>The newly created batch account.</returns>
        private async Task<BatchAccountResource> CreateBatchAccountAsync(
            ResourceGroupResource group,
            BatchAccountCollection batchAccounts,
            string name,
            StorageAccountResource storageAccount)
        {
            WriteMessage($"Batch account does not exist. Creating batch account: {name}...");
            try
            {
                BatchAccountCreateOrUpdateContent parameters = 
                        new BatchAccountCreateOrUpdateContent(group.Data.Location)
                        {
                            AutoStorage = new BatchAccountAutoStorageBaseConfiguration(
                                storageAccount.Id)
                        };

                    ArmOperation<BatchAccountResource> operation = 
                        await batchAccounts.CreateOrUpdateAsync(
                            WaitUntil.Completed,
                            name,
                            parameters);
                    return operation.Value;
            }
            catch (Exception error)
            {
                throw new Exception($"Failed to create batch account {name}", error);
            }
        }

        /// <summary>
        /// Get the name of the default resource group used by apsim if running
        /// in use-existing mode.
        /// </summary>
        /// <returns>The name of the default resource group.</returns>
        private static string GetResourceGroupName()
        {
            // 0-23 alphanumeric chars.
            string name = $"apsimresources{Environment.UserName.ToLowerInvariant()}";
            if (name.Length > maxNameLength)
                name = name.Substring(0, maxNameLength);
            return name;
        }

        /// <summary>
        /// Gets the name of the default batch account used by apsim if running
        /// in use-existing mode.
        /// </summary>
        /// <returns>The name of the default batch account.</returns>
        private static string GetBatchAccountName()
        {
            // 0-23 alphanumeric chars.
            string name = $"apsimbatch{Environment.UserName.ToLowerInvariant()}";
            if (name.Length > maxNameLength)
                name = name.Substring(0, maxNameLength);
            return name;
        }

        /// <summary>
        /// Gets the name of the default storage account used by apsim if running
        /// in use-existing mode.
        /// </summary>
        /// <returns>The name of the default storage account.</returns>
        private static string GetStorageAccountName()
        {
            // 0-23 alphanumeric chars.
            string name = $"apsimstorage{Environment.UserName.ToLowerInvariant()}";
            if (name.Length > maxNameLength)
                name = name.Substring(0, maxNameLength);
            return name;
        }
    }
}

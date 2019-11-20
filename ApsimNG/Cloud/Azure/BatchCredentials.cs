namespace ApsimNG.Cloud.Azure
{
    public class BatchAuth
    {
        /// <summary>
        /// URL of the batch account.
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Azure Batch account name.
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Azure Batch account key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Generates an instance of BatchCredentials from the settings saved in ApsimNG.Cloud.AzureSettings.
        /// </summary>
        /// <returns></returns>
        public static BatchAuth FromConfiguration()
        {
            return new BatchAuth
            {
                Url = (string)AzureSettings.Default["BatchUrl"],
                Account = (string)AzureSettings.Default["BatchAccount"],
                Key = (string)AzureSettings.Default["BatchKey"]
            };
        }
    }
}

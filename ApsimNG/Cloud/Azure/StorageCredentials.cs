namespace ApsimNG.Cloud.Azure
{
    public class StorageAuth
    {
        //this class should perhaps be renamed so as not to be identical to the Azure-provided one
        public string Account { get; set; }
        public string Key { get; set; }
        public static StorageAuth FromConfiguration()
        {
            return new StorageAuth
            {
                Account = (string)AzureSettings.Default["StorageAccount"],
                Key = (string)AzureSettings.Default["StorageKey"]
            };
        }
    }
}

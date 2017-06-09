namespace SpyStudio.COM.Controls
{
    public class ClsidRegistryInfo
    {
        public ClsidRegistryInfo()
        {
            Description = "";
            ServerPath = "";
        }
        public ClsidRegistryInfo(string description, string serverPath)
        {
            Description = description;
            ServerPath = serverPath;
        }

        public string Description { get; set; }

        public string ServerPath { get; set; }
    }
}
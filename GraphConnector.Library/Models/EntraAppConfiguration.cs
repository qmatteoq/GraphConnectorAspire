namespace GraphConnector.Library.Models
{
    public class EntraAppConfiguration
    {
        public string appId { get; set; }
        public string objectId { get; set; }
        public string tenantId { get; set; }
        public Secret[] secrets { get; set; }
    }

    public class Secret
    {
        public string displayName { get; set; }
        public string value { get; set; }
    }

}

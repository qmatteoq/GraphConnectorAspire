namespace GraphConnector.Library.Responses
{
    public class QueueConnectionRequest
    {
        public string? Action { get; set; }
        public string? ConnectorId { get; set; }
        public string? ConnectorName { get; set; }
        public string? ConnectorDescription { get; set; }
        public string? FeedUrl { get; set; }
    }
}

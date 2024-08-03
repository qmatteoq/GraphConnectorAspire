namespace GraphConnector.Library.Requests
{
    public class QueueContentRequest
    {
        public string? Action { get; set; }
        public string? FeedUrl { get; set; }
        public string? ConnectorId { get; set; }
    }
}

using GraphConnector.Library.Enums;

namespace GraphConnector.Library.Messages
{
    public class ConnectionMessage
    {
        public ConnectionAction Action { get; set; }
        public string? ConnectorId { get; set; }

        public string? ConnectorDescription { get; set; }
        public string? ConnectorName { get; set; }

        public string? ConnectorTicket { get; set; }
        public string? Location { get; set; }

        public string? FeedUrl { get; set; }
    }
}

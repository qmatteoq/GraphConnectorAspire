﻿namespace GraphConnector.Library.Requests
{
    public class QueueConnectionRequest
    {
        public string? Action { get; set; }
        public string? ConnectorId { get; set; }
        public string? ConnectorName { get; set; }
        public string? ConnectorDescription { get; set; }
    }
}

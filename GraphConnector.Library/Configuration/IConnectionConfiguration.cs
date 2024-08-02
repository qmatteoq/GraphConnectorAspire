using Microsoft.Graph.Models.ExternalConnectors;

namespace GraphConnector.Library.Configuration
{
    public interface IConnectionConfiguration
    {
        ExternalConnection GetExternalConnection(string connectorId, string connectorName, string connectorDescription);
        Schema GetSchema();
    }
}
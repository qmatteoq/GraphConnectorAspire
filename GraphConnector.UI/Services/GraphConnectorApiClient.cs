﻿using GraphConnector.Library.Requests;

namespace GraphConnector.UI.Services
{
    public class GraphConnectorApiClient
    {
        private readonly HttpClient _client;

        public GraphConnectorApiClient(HttpClient client)
        {
            _client = client;
        }

        public async Task QueueConnectionRequestAsync(QueueConnectionRequest request)
        {
            var response = await _client.PostAsJsonAsync("createConnection", request);
            response.EnsureSuccessStatusCode();
        }

        public async Task QueueSchemaRequestAsync(QueueSchemaRequest request)
        {
            var response = await _client.PostAsJsonAsync("createSchema", request);
            response.EnsureSuccessStatusCode();
        }
    }
}
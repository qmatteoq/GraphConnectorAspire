using GraphConnector.Library.Responses;

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

        public async Task QueueContentRequestAsync(QueueContentRequest request)
        {
            var response = await _client.PostAsJsonAsync("uploadContent", request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<OperationStatusResponse> CheckOperationprogressAsync()
        {
            var response = await _client.GetFromJsonAsync<OperationStatusResponse>("checkOperationProgress");
            return response;
        }
    }
}
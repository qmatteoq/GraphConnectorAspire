using GraphConnector.Library.Enums;
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
        public async Task<OperationStatusResponse> CheckOperationprogressAsync()
        {
            try
            {
                var response = await _client.GetFromJsonAsync<OperationStatusResponse>("checkOperationProgress");
                return response;
            }
            catch
            {
                return new OperationStatusResponse
                {
                    Status = OperationStatus.InProgress,
                    LastStatusDate = DateTimeOffset.Now
                };
            }
        }
    }
}
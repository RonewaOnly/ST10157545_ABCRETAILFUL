using ABCRETAIL.WEB.Models;
using System.Net;

namespace ABCRETAIL.WEB.Services.FunctionClients
{
    public class QueueStorageFunctionClient : IQueueStorageService
    {
        private readonly HttpClient _http;

        public QueueStorageFunctionClient(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("AzureFunctions");
        }

        //The function creates its queue the first time it runs 
        public Task InitializeAsync() => Task.CompletedTask;
        public async Task SendOrderMessageAsync(OrderMessage order)
        {
            var response = await _http.PostAsJsonAsync("queue", order, FunctionJsonOptions.Default);
            response.EnsureSuccessStatusCode();
        }
        public async Task<IEnumerable<OrderMessage>> PeekMessagesAsync(int maxMessages = 32)
        {
            var result = await _http.GetFromJsonAsync<List<OrderMessage>>($"queue/peek?max={maxMessages}", FunctionJsonOptions.Default);
            return result ?? new List<OrderMessage>();
        }
        public async Task<OrderMessage?> ProcessNextMessageAsync()
        {
            var response = await _http.PostAsync("queue/process", null);
            if (response.StatusCode == HttpStatusCode.NoContent) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OrderMessage>(FunctionJsonOptions.Default);
        }
        public async Task<int> GetApproximateMessageCountAsync()
        {
            var result = await _http.GetFromJsonAsync<CountResult>("queue/count", FunctionJsonOptions.Default);
            return result?.Count ?? 0;
        }
        private class CountResult
        {
            public int Count { get; set; }
        }
    }
}

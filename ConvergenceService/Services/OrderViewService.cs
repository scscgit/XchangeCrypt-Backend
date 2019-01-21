using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IO.Swagger.Models;
using Newtonsoft.Json;

namespace XchangeCrypt.Backend.ConvergenceService.Services
{
    public class OrderViewService : IDisposable
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string _remoteUrl;

        public OrderViewService()
        {
            // TODO
            var domainName = "192.168.99.100";
            var port = "8003";
            _remoteUrl = $"https://{domainName}:{port}/api/v1/view/";
        }

        public List<Execution> GetExecutions(string user, string accountId, string instrument, int? maxCount)
        {
            return JsonConvert.DeserializeObject<List<Execution>>(
                _client.GetStringAsync(Uri(
                    "executions",
                    new Dictionary<string, string>
                    {
                        {"user", user},
                        {"accountId", accountId},
                        {"instrument", instrument},
                        {"maxCount", maxCount.ToString()},
                    }
                )).Result
            );
        }

        public List<Order> GetOrders(string user, string accountId)
        {
            return JsonConvert.DeserializeObject<List<Order>>(
                _client.GetStringAsync(Uri(
                    "orders",
                    new Dictionary<string, string>
                    {
                        {"user", user},
                        {"accountId", accountId},
                    }
                )).Result
            );
        }

        public Order GetOrder(string user, string accountId, string orderId)
        {
            return JsonConvert.DeserializeObject<Order>(
                _client.GetStringAsync(Uri(
                    "order",
                    new Dictionary<string, string>
                    {
                        {"user", user},
                        {"accountId", accountId},
                        {"orderId", orderId},
                    }
                )).Result
            );
        }

        private string Uri(string name, IDictionary<string, string> parameters)
        {
            var uri = $"{_remoteUrl}{name}?";
            uri = parameters.Aggregate(
                uri,
                (current, keyValuePair) => keyValuePair.Value != null && !keyValuePair.Value.Equals("")
                    ? current + $"{keyValuePair.Key}={keyValuePair.Value}&"
                    : current
            );
            return uri.Substring(0, uri.Length - 1);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}

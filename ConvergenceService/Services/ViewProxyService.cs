using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IO.Swagger.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using XchangeCrypt.Backend.ConvergenceService.Areas.User.Models;

namespace XchangeCrypt.Backend.ConvergenceService.Services
{
    public class ViewProxyService : IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _remoteUrl;

        public ViewProxyService(IConfiguration configuration)
        {
            _client = new HttpClient();
            _remoteUrl =
                $"https://{configuration["ViewService:Domain"] ?? throw new ArgumentException("ViewService:Domain")}:{configuration["ViewService:Port"] ?? throw new ArgumentException("ViewService:Port")}/api/v1/view/";
        }

        public ViewProxyService(HttpClient mockedClient)
        {
            _client = mockedClient;
            _remoteUrl = "api/v1/view/";
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
                        {"maxCount", maxCount?.ToString()},
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

        public List<Order> GetOrdersHistory(string user, string accountId, int? maxCount)
        {
            return JsonConvert.DeserializeObject<List<Order>>(
                _client.GetStringAsync(Uri(
                    "ordersHistory",
                    new Dictionary<string, string>
                    {
                        {"user", user},
                        {"accountId", accountId},
                        {"maxCount", maxCount?.ToString()},
                    }
                )).Result
            );
        }

        public IEnumerable<WalletDetails> GetWallets(string user, string accountId)
        {
            return JsonConvert.DeserializeObject<IEnumerable<WalletDetails>>(
                _client.GetStringAsync(Uri(
                    "wallets",
                    new Dictionary<string, string>
                    {
                        {"user", user},
                        {"accountId", accountId},
                    }
                )).Result
            );
        }

        public Depth GetDepth(string instrument)
        {
            return JsonConvert.DeserializeObject<Depth>(
                _client.GetStringAsync(Uri(
                    "depth",
                    new Dictionary<string, string>
                    {
                        {"instrument", instrument},
                    }
                )).Result
            );
        }

        public BarsArrays GetHistoryBars(
            string instrument, string resolution, decimal? from, decimal? to, decimal? countback)
        {
            return JsonConvert.DeserializeObject<BarsArrays>(
                _client.GetStringAsync(Uri(
                    "historyBars",
                    new Dictionary<string, string>
                    {
                        {"instrument", instrument},
                        {"resolution", resolution},
                        {"from", from.HasValue ? $"{from}" : null},
                        {"to", to.HasValue ? $"{to}" : null},
                        {"countback", countback.HasValue ? $"{countback}" : null},
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
            // We cannot dispose of an injected mock client during test process
            //_client?.Dispose();
        }
    }
}

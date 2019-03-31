using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.ConvergenceService;
using XchangeCrypt.Backend.ConvergenceService.Services;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using Xunit;

namespace IntegrationTests
{
    public class ConvergenceControllerIntegrationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TestingConnectionString =
            "mongodb+srv://test:test@cluster0-8dep5.azure.mongodb.net/test?retryWrites=true";

        //private const string ViewPrefix = "/api/v1/view/";
        private const string ApiPrefix = "/api/v1/trading/";
        private readonly ILogger _logger;
        private readonly EventHistoryRepository _eventHistoryRepository;
        private readonly HttpClient _client;

        private string _instrument = "ETH_BTC";

        public ConvergenceControllerIntegrationTest(
            WebApplicationFactory<Startup> factory)
        {
            _logger = new Logger<ConvergenceControllerIntegrationTest>(new LoggerFactory());

            // Wipe the testing DB
            _eventHistoryRepository = new EventHistoryRepository(new DataAccess(TestingConnectionString));
            _eventHistoryRepository.Events().DeleteMany(MongoDB.Driver.Builders<EventEntry>.Filter.Where(e => true));

            // Start a view service as a direct client, bypassing convergence service proxy middleman
            // (This is required because the started client doesn't expose itself - we would have to inject it somehow)
            var viewClient = new WebApplicationFactory<XchangeCrypt.Backend.ViewService.Startup>().CreateClient();

            // Start other queue-based supporting micro-services
            new WebApplicationFactory<XchangeCrypt.Backend.TradingService.Startup>().CreateClient();
            new WebApplicationFactory<XchangeCrypt.Backend.WalletService.Startup>().CreateClient();

            // Prepare Convergence Service with injected View Service client
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddTransient(service => new ViewProxyService(viewClient));
                });
            }).CreateClient();
        }

        [Fact]
        public async Task CanCreateAndMatchLimitOrders()
        {
            // Assume test users have large balance

            // Make sure the depth is empty (and make sure the ViewService is running)
            var depth = await GetDepth();
            Assert.Empty(depth.Asks);
            Assert.Empty(depth.Bids);

            // We are using test users
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_1");

            // Prepare a buy order of 3.5 at price 0.2
            await LimitOrder(OrderSide.Buy, 2.5, 0.2);
            _logger.LogInformation("Placed limit buy 2.5 @ 0.2");
            await LimitOrder(OrderSide.Buy, 1, 0.2);
            _logger.LogInformation("Placed limit buy 1 @ 0.2");
            // Consume both buy orders at price 0.2, and add a new sell order of 1 at price 0.1
            await LimitOrder(OrderSide.Sell, 4.5, 0.1);
            _logger.LogInformation("Placed limit sell 4.5 @ 0.1");

            // Wait for Trading Service to finish processing events
            await Task.Delay(5000);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_2");

            // Make sure the second test user doesn't own any orders
            var orders = await GetOrders();
            Assert.Empty(orders);

            // Check the new depth state
            depth = await GetDepth();
            var ask = Assert.Single(depth.Asks);
            // Price
            Assert.Equal(0.1m, ask[0]);
            // Volume
            Assert.Equal(1, ask[1]);
            Assert.Empty(depth.Bids);

            // Consume the sell order of 1 at price 0.1, and add a new buy order of 1 at price 0.8
            await LimitOrder(OrderSide.Buy, 2, 0.8);
            _logger.LogInformation("Placed limit buy 2 @ 1");

            // Wait for Trading Service to finish processing events
            await Task.Delay(5000);

            // Make sure there is the one partially filled expected order for the second test user
            orders = await GetOrders();
            var singleOrder = Assert.Single(orders);
            Assert.Equal(0.8m, singleOrder.LimitPrice);
            Assert.Equal(2, singleOrder.Qty);
            Assert.Equal(1, singleOrder.FilledQty);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_1");

            // Make sure first user has no dangling orders
            orders = await GetOrders();
            Assert.Empty(orders);

            // Check the last closed order of the first user (qty 4.5)
            var oneOrderHistory = Assert.Single(await GetOrdersHistory(1));
            Assert.Equal(4.5m, oneOrderHistory.Qty);
            Assert.Equal(4.5m, oneOrderHistory.FilledQty);
            Assert.Equal(0.1m, oneOrderHistory.LimitPrice);

            // Check the previous closed order of the first user (qty 1), the most recent should be ordered first
            var ordersHistory = await GetOrdersHistory(2);
            Assert.Equal(2, ordersHistory.Count());
            Assert.Equal(1, ordersHistory.Last().Qty);
            Assert.Equal(1, ordersHistory.Last().FilledQty);
            Assert.Equal(0.2m, ordersHistory.Last().LimitPrice);

            // Cleanup
            await LimitOrder(OrderSide.Sell, 1, 0.8);
            _logger.LogInformation("Placed limit sell 1 @ 0.8");

            // Wait for Trading Service to finish processing events
            await Task.Delay(5000);

            // Make sure the depth is empty at the end
            depth = await GetDepth();
            Assert.Empty(depth.Asks);
            Assert.Empty(depth.Bids);
        }

        private async Task<T> RequestContentsOrError<T>(HttpResponseMessage response)
        {
            var asString = await response.Content.ReadAsStringAsync();
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                // Errors like invalid parameters must be printed out.
                throw new Exception($"{nameof(HttpRequestException)}: {e.Message}\nResponse message: {asString}", e);
            }

            IDictionary<string, JToken> dictionary;
            try
            {
                dictionary = JsonConvert.DeserializeObject<IDictionary<string, JToken>>(asString);
            }
            catch (JsonReaderException e)
            {
                throw new Exception($"{nameof(JsonReaderException)}: {e.Message}\nResponse message: {asString}", e);
            }

            if ("ok" != dictionary["s"].ToString())
            {
                // Print the error message as a fail result
                Assert.Null(dictionary["errmsg"].ToString());
                Assert.Equal("ok", dictionary["s"].ToString());
            }

            return dictionary["d"].ToObject<T>();
        }

        private async Task<Depth> GetDepth()
        {
            return await RequestContentsOrError<Depth>(
                await _client.GetAsync($"{ApiPrefix}depth?symbol={_instrument}")
//                await _viewClient.GetAsync($"{ViewPrefix}depth?instrument={_instrument}")
            );
        }

        private async Task<IEnumerable<Order>> GetOrders()
        {
            return await RequestContentsOrError<IEnumerable<Order>>(
                await _client.GetAsync($"{ApiPrefix}accounts/0/orders")
//                await _viewClient.GetAsync($"{ViewPrefix}accounts/0/orders")
            );
        }

        private async Task<IEnumerable<Order>> GetOrdersHistory(int maxCount)
        {
            return await RequestContentsOrError<IEnumerable<Order>>(
                await _client.GetAsync($"{ApiPrefix}accounts/0/ordersHistory?maxCount={maxCount}")
//                await _viewClient.GetAsync($"{ViewPrefix}accounts/0/ordersHistory?maxCount={maxCount}")
            );
        }

        private async Task<InlineResponse2005D> LimitOrder(OrderSide orderSide, double qty, double limitPrice)
        {
            string side;
            switch (orderSide)
            {
                case OrderSide.Buy:
                    side = MessagingConstants.OrderSides.BuySide;
                    break;
                case OrderSide.Sell:
                    side = MessagingConstants.OrderSides.SellSide;
                    break;
                default:
                    throw new ArgumentException(nameof(orderSide));
            }

            var parameters = new Dictionary<string, string>
            {
                {"instrument", _instrument},
                {"qty", $"{qty}"},
                {"side", side},
                {"type", MessagingConstants.OrderTypes.LimitOrder},
                {"limitPrice", $"{limitPrice}"},
                {"stopPrice", null},
                {"durationType", null},
                {"durationDateTime", null},
                {"stopLoss", null},
                {"takeProfit", null},
                {"digitalSignature", null},
                {"requestId", null},
            };
            var encodedContent = new FormUrlEncodedContent(parameters);
            return await RequestContentsOrError<InlineResponse2005D>(
                await _client.PostAsync($"{ApiPrefix}accounts/0/orders", encodedContent)
            );
        }
    }
}

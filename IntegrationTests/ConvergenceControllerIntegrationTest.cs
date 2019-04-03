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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.ConvergenceService;
using XchangeCrypt.Backend.ConvergenceService.Areas.User.Models;
using XchangeCrypt.Backend.ConvergenceService.Services;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Providers.ETH;
using XchangeCrypt.Backend.WalletService.Services;
using Xunit;
using Xunit.Sdk;

namespace XchangeCrypt.Backend.Tests.IntegrationTests
{
    [TestCaseOrderer("XchangeCrypt.Backend.Tests.IntegrationTests.AlphabeticalOrderer", "XchangeCrypt.Backend")]
    public class ConvergenceControllerIntegrationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private class MockedEthereumProvider : EthereumProvider
        {
            public MockedEthereumProvider(
                ILogger<EthereumProvider> logger,
                WalletOperationService walletOperationService,
                EventHistoryService eventHistoryService,
                RandomEntropyService randomEntropyService,
                VersionControl versionControl)
                : base(logger, walletOperationService, eventHistoryService, randomEntropyService, versionControl)
            {
                ProviderLookup[ThisCoinSymbol] = this;
            }

            public readonly Dictionary<string, decimal> MockedBalances = new Dictionary<string, decimal>();

            public override Task<decimal> GetBalance(string publicKey)
            {
                if (!MockedBalances.ContainsKey(publicKey))
                {
                    MockedBalances.Add(publicKey, 100);
                }

                return Task.FromResult(MockedBalances[publicKey]);
            }

            public override async Task<bool> Withdraw(string walletPublicKeyUserReference, string withdrawToPublicKey,
                decimal value)
            {
                return true;
            }
        }

        private class MockedBitcoinProvider : BitcoinProvider
        {
            public MockedBitcoinProvider(
                ILogger<EthereumProvider> logger,
                WalletOperationService walletOperationService,
                EventHistoryService eventHistoryService,
                RandomEntropyService randomEntropyService,
                VersionControl versionControl)
                : base(logger, walletOperationService, eventHistoryService, randomEntropyService, versionControl)
            {
                ProviderLookup[ThisCoinSymbol] = this;
            }

            public readonly Dictionary<string, decimal> MockedBalances = new Dictionary<string, decimal>();

            public override Task<decimal> GetBalance(string publicKey)
            {
                if (!MockedBalances.ContainsKey(publicKey))
                {
                    MockedBalances.Add(publicKey, 80);
                }

                return Task.FromResult(MockedBalances[publicKey]);
            }

            public override async Task<bool> Withdraw(string walletPublicKeyUserReference, string withdrawToPublicKey,
                decimal value)
            {
                return true;
            }
        }

        private const string TestingConnectionString =
            "mongodb+srv://test:test@cluster0-8dep5.azure.mongodb.net/test?retryWrites=true";

        private const string TradingPrefix = "/api/v1/trading/";
        private const string UserPrefix = "/api/v1/user/";
        private readonly ILogger _logger;
        private readonly EventHistoryRepository _eventHistoryRepository;
        private readonly HttpClient _client;

        private MockedEthereumProvider _ethProvider;
        private MockedBitcoinProvider _btcProvider;

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
            new WebApplicationFactory<XchangeCrypt.Backend.WalletService.Startup>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Replace works the same way as AddSingleton, but it still crashes at
                    // HostedServiceExecutor#ExecuteAsync with logged NullReferenceException, yet it works properly.
                    // This crash seems to be fixed by removing the IHostedService replacement,
                    // letting it receive a replaced injected singleton provider instance.

                    services.Replace(ServiceDescriptor.Singleton<EthereumProvider>(service =>
                        _ethProvider = new MockedEthereumProvider(
                            service.GetService<ILogger<EthereumProvider>>(),
                            service.GetService<WalletOperationService>(),
                            service.GetService<EventHistoryService>(),
                            service.GetService<RandomEntropyService>(),
                            service.GetService<VersionControl>()))
                    );

//                    services.Replace(ServiceDescriptor.Singleton<IHostedService, EthereumProvider>(
//                        serviceProvider => serviceProvider.GetService<MockedEthereumProvider>()
//                    ));

                    services.Replace(ServiceDescriptor.Singleton<BitcoinProvider>(service =>
                        _btcProvider = new MockedBitcoinProvider(
                            service.GetService<ILogger<EthereumProvider>>(),
                            service.GetService<WalletOperationService>(),
                            service.GetService<EventHistoryService>(),
                            service.GetService<RandomEntropyService>(),
                            service.GetService<VersionControl>()))
                    );

//                    services.Replace(ServiceDescriptor.Singleton<IHostedService, BitcoinProvider>(
//                        serviceProvider => serviceProvider.GetService<MockedBitcoinProvider>()
//                    ));
                });

                // NOTE: issue: a hosted service doesn't ensure the initialization is done before processing requests!
            }).CreateClient();

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
        public async Task T1_CanWithdrawWithOpenOrdersAndTheyClose()
        {
            // Make sure the depth is empty (and make sure the ViewService is running)
            var depth = await GetDepth();
            Assert.Empty(depth.Asks);
            Assert.Empty(depth.Bids);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_1");

            // Generate the wallets
            Try(10, () =>
            {
                var wallets = GetWallets().Result;
                // Lets not flood it with too many duplicate generation requests
                Task.Delay(1000).Wait();
                Assert.Equal(100,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                    ).Balance);
                Assert.Equal(80,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                    ).Balance);
            });

            // Try to properly withdraw
            Assert.Null(await WalletWithdraw(MockedEthereumProvider.ETH, "mockedPublicKey", 50));

            // Try to withdraw unavailable funds
            Assert.Null(await WalletWithdraw(MockedEthereumProvider.ETH, "mockedPublicKey", 60));

            // Make sure the balance gets updated
            Try(10, () =>
            {
                var wallets = GetWallets().Result;
                Assert.Equal(50,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                    ).Balance);
            });
        }

        [Fact]
        public async Task T2_CanCreateAndMatchLimitOrders()
        {
            // Make sure the depth is empty (and make sure the ViewService is running)
            var depth = await GetDepth();
            Assert.Empty(depth.Asks);
            Assert.Empty(depth.Bids);

            // We are using test users
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_1");

            // Assume test users have large balance: we generate wallets, and then wait for a mocked balance population
            Try(10, () =>
            {
                var wallets = GetWallets().Result;
                // Lets not flood it with too many duplicate generation requests
                Task.Delay(1000).Wait();
                Assert.Equal(100,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                    ).Balance);
                Assert.Equal(80,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                    ).Balance);
            });

            // Prepare a buy order of 3.5 at price 0.2
            await LimitOrder(OrderSide.Buy, 2.5, 0.2);
            _logger.LogInformation("Placed limit buy 2.5 @ 0.2");
            await LimitOrder(OrderSide.Buy, 1, 0.2);
            _logger.LogInformation("Placed limit buy 1 @ 0.2");
            // Consume both buy orders at price 0.2, and add a new sell order of 1 at price 0.1
            await LimitOrder(OrderSide.Sell, 4.5, 0.1);
            _logger.LogInformation("Placed limit sell 4.5 @ 0.1");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_2");

            // Make sure the second test user doesn't own any orders
            var orders = await GetOrders();
            Assert.Empty(orders);

            // Wait for Trading Service to finish processing events
            Try(10, () =>
            {
                // Check the new depth state
                depth = GetDepth().Result;
                var ask = Assert.Single(depth.Asks);
                // Price
                Assert.Equal(0.1m, ask[0]);
                // Volume
                Assert.Equal(1, ask[1]);
                Assert.Empty(depth.Bids);
            });

            // Consume the sell order of 1 at price 0.1, and add a new buy order of 1 at price 0.8
            await LimitOrder(OrderSide.Buy, 2, 0.8);
            _logger.LogInformation("Placed limit buy 2 @ 1");

            // Wait for Trading Service to finish processing events
            Try(10, () =>
            {
                // Make sure there is the one partially filled expected order for the second test user
                orders = GetOrders().Result;
                var singleOrder = Assert.Single(orders);
                Assert.Equal(0.8m, singleOrder.LimitPrice);
                Assert.Equal(2, singleOrder.Qty);
                Assert.Equal(1, singleOrder.FilledQty);
            });

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
            Try(10, () =>
            {
                // Make sure the depth is empty at the end
                depth = GetDepth().Result;
                Assert.Empty(depth.Asks);
                Assert.Empty(depth.Bids);
            });
        }

        private async Task<T> RequestContentsOrError<T>(HttpResponseMessage response, bool directlyInsteadOfD = false)
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

            if (directlyInsteadOfD)
            {
                return JsonConvert.DeserializeObject<T>(asString);
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

        private async Task<IEnumerable<WalletDetails>> GetWallets()
        {
            return await RequestContentsOrError<IEnumerable<WalletDetails>>(
                await _client.GetAsync($"{UserPrefix}accounts/0/wallets")
                , true
            );
        }

        private async Task<string> WalletWithdraw(
            string coinSymbol, string recipientPublicKey, decimal withdrawalAmount)
        {
            var parameters = new Dictionary<string, string>
            {
                {"recipientPublicKey", recipientPublicKey},
                {"withdrawalAmount", $"{withdrawalAmount}"},
            };
            var encodedContent = new FormUrlEncodedContent(parameters);
            return await RequestContentsOrError<string>(
                await _client.PostAsync(
                    $"{UserPrefix}accounts/0/wallets/{coinSymbol}/withdraw",
                    encodedContent
                ), true
            );
        }

        private async Task<Depth> GetDepth()
        {
            return await RequestContentsOrError<Depth>(
                await _client.GetAsync($"{TradingPrefix}depth?symbol={_instrument}")
            );
        }

        private async Task<IEnumerable<Order>> GetOrders()
        {
            return await RequestContentsOrError<IEnumerable<Order>>(
                await _client.GetAsync($"{TradingPrefix}accounts/0/orders")
            );
        }

        private async Task<IEnumerable<Order>> GetOrdersHistory(int maxCount)
        {
            return await RequestContentsOrError<IEnumerable<Order>>(
                await _client.GetAsync($"{TradingPrefix}accounts/0/ordersHistory?maxCount={maxCount}")
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
                await _client.PostAsync($"{TradingPrefix}accounts/0/orders", encodedContent)
            );
        }

        private void Try(int maxSeconds, Action action)
        {
            for (var i = 0; i < maxSeconds; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (XunitException e)
                {
                    _logger.LogInformation($"Try count {i + 1}/{maxSeconds} after {e.Message}");
                    Task.Delay(1000).Wait();
                }
            }

            action();
        }
    }
}

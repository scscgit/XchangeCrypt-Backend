using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.ConvergenceService;
using XchangeCrypt.Backend.ConvergenceService.Areas.User.Models;
using XchangeCrypt.Backend.ConvergenceService.Services;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models;
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
    [TestCaseOrderer("XchangeCrypt.Backend.Tests.IntegrationTests.AlphabeticalOrderer", "IntegrationTests")]
    public class ConvergenceControllerIntegrationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private class TemporalEventHistoryService : EventHistoryService
        {
            public TemporalEventHistoryService(
                EventHistoryRepository eventHistoryRepository,
                VersionControl versionControl,
                ILogger<EventHistoryService> logger
            ) : base(eventHistoryRepository, versionControl, logger)
            {
            }

            public bool Stopped;
            public static DateTime? MockedCurrentTime;

            protected override DateTime CurrentTime()
            {
                return MockedCurrentTime ?? base.CurrentTime();
            }

            public override Task<IList<EventEntry>> Persist(IEnumerable<EventEntry> eventTransaction,
                long? alreadyLockedVersionNumber = null)
            {
                if (Stopped)
                {
                    throw new Exception("Already stopped");
                }

                return base.Persist(eventTransaction, alreadyLockedVersionNumber);
            }
        }

        private class MockedEthereumProvider : EthereumProvider
        {
            public MockedEthereumProvider(
                ILogger<MockedEthereumProvider> logger,
                WalletOperationService walletOperationService,
                EventHistoryService eventHistoryService,
                RandomEntropyService randomEntropyService,
                VersionControl versionControl,
                IConfiguration configuration)
                : base(logger, walletOperationService, eventHistoryService, randomEntropyService, versionControl,
                    configuration)
            {
                ProviderLookup[ThisCoinSymbol] = this;
            }

            public readonly Dictionary<string, decimal> MockedBalances = new Dictionary<string, decimal>();
            public bool FirstDeposit = true;

            public override Task<decimal> GetBalance(string publicKey)
            {
                if (!MockedBalances.ContainsKey(publicKey))
                {
                    MockedBalances.Add(publicKey, FirstDeposit ? 100 : 0);
                    FirstDeposit = false;
                }

                return Task.FromResult(MockedBalances[publicKey]);
            }

            public override async Task<bool> Withdraw(
                string walletPublicKeyUserReference, string withdrawToPublicKey, decimal valueExclFee)
            {
                if (!MockedBalances.ContainsKey(walletPublicKeyUserReference)
                    || MockedBalances[walletPublicKeyUserReference] < valueExclFee + Fee())
                {
                    _logger.LogWarning(
                        $"Mocked {ThisCoinSymbol} wallet has denied withdrawal of {valueExclFee} + {Fee()} from wallet {walletPublicKeyUserReference} - the current balance is {MockedBalances[walletPublicKeyUserReference]}");
                    return false;
                }

                MockedBalances[walletPublicKeyUserReference] -= valueExclFee + Fee();

                // Simulate the consolidation too
                if (!MockedBalances.ContainsKey(withdrawToPublicKey))
                {
                    MockedBalances.Add(withdrawToPublicKey, 0);
                }

                MockedBalances[withdrawToPublicKey] += valueExclFee;
                return true;
            }
        }

        private class MockedBitcoinProvider : BitcoinProvider
        {
            public MockedBitcoinProvider(
                ILogger<MockedBitcoinProvider> logger,
                WalletOperationService walletOperationService,
                EventHistoryService eventHistoryService,
                RandomEntropyService randomEntropyService,
                VersionControl versionControl,
                IConfiguration configuration)
                : base(logger, walletOperationService, eventHistoryService, randomEntropyService, versionControl,
                    configuration)
            {
                ProviderLookup[ThisCoinSymbol] = this;
            }

            public readonly Dictionary<string, decimal> MockedBalances = new Dictionary<string, decimal>();
            public bool FirstDeposit = true;

            public override Task<decimal> GetBalance(string publicKey)
            {
                if (!MockedBalances.ContainsKey(publicKey))
                {
                    MockedBalances.Add(publicKey, FirstDeposit ? 80 : 0);
                    FirstDeposit = false;
                }

                return Task.FromResult(MockedBalances[publicKey]);
            }

            public override async Task<bool> Withdraw(
                string walletPublicKeyUserReference, string withdrawToPublicKey, decimal valueExclFee)
            {
                if (!MockedBalances.ContainsKey(walletPublicKeyUserReference)
                    || MockedBalances[walletPublicKeyUserReference] < valueExclFee + Fee())
                {
                    _logger.LogWarning(
                        $"Mocked {ThisCoinSymbol} wallet has denied withdrawal of {valueExclFee} + {Fee()} from wallet {walletPublicKeyUserReference} - the current balance is {MockedBalances[walletPublicKeyUserReference]}");
                    return false;
                }

                MockedBalances[walletPublicKeyUserReference] -= valueExclFee + Fee();

                // Simulate the consolidation too
                if (!MockedBalances.ContainsKey(withdrawToPublicKey))
                {
                    MockedBalances.Add(withdrawToPublicKey, 0);
                }

                MockedBalances[withdrawToPublicKey] += valueExclFee;
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
        private HttpClient _viewClient;

        private MockedEthereumProvider _ethProvider;
        private MockedBitcoinProvider _btcProvider;
        private TemporalEventHistoryService _walletServiceEventHistoryService;
        private TemporalEventHistoryService _tradingServiceEventHistoryService;

        private string _instrument = "ETH_BTC";
        private WebApplicationFactory<TradingService.Startup> _tradingService;
        private WebApplicationFactory<WalletService.Startup> _walletService;

        public ConvergenceControllerIntegrationTest(
            WebApplicationFactory<Startup> factory)
        {
            // Start a view service as a direct client, bypassing convergence service proxy middleman
            // (This is required because the started client doesn't expose itself - we would have to inject it somehow)
            _viewClient = new WebApplicationFactory<XchangeCrypt.Backend.ViewService.Startup>().CreateClient();

            // Prepare Convergence Service with injected View Service client
            var clientFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Somehow this works too, even though there are now two services configured
                    services.AddTransient(service => new ViewProxyService(_viewClient));
                    services.AddTransient<Logger<ConvergenceControllerIntegrationTest>>();
                });
            });

            _client = clientFactory.CreateClient();
            _logger = clientFactory.Server.Host.Services.GetService<Logger<ConvergenceControllerIntegrationTest>>();

            _logger.LogInformation("Wiping test DB");
            // Wipe the testing DB
            _eventHistoryRepository = new EventHistoryRepository(new DataAccess(TestingConnectionString));
            _eventHistoryRepository.Events().DeleteMany(Builders<EventEntry>.Filter.Where(e => true));
            new WalletRepository(new DataAccess(TestingConnectionString))
                .HotWallets()
                .DeleteMany(Builders<HotWallet>.Filter.Where(e => true));
            _logger.LogInformation("Wiped test DB, preparing a new test run");

            // Start other queue-based supporting micro-services
            _tradingService = new WebApplicationFactory<XchangeCrypt.Backend.TradingService.Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.Replace(ServiceDescriptor.Singleton<EventHistoryService>(service =>
                            _tradingServiceEventHistoryService = new TemporalEventHistoryService(
                                service.GetService<EventHistoryRepository>(),
                                service.GetService<VersionControl>(),
                                service.GetService<ILogger<TemporalEventHistoryService>>()
                            )
                        ));
                    });
                });
            _tradingService.CreateClient();
            _walletService = new WebApplicationFactory<XchangeCrypt.Backend.WalletService.Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.Replace(ServiceDescriptor.Singleton<EventHistoryService>(service =>
                            _walletServiceEventHistoryService = new TemporalEventHistoryService(
                                service.GetService<EventHistoryRepository>(),
                                service.GetService<VersionControl>(),
                                service.GetService<ILogger<TemporalEventHistoryService>>()
                            )
                        ));

                        // Replace works the same way as AddSingleton, but it still crashes at
                        // HostedServiceExecutor#ExecuteAsync with logged NullReferenceException, yet it works properly.
                        // This crash seems to be fixed by removing the IHostedService replacement,
                        // letting it receive a replaced injected singleton provider instance.

                        services.Replace(ServiceDescriptor.Singleton<EthereumProvider>(service =>
                            _ethProvider = new MockedEthereumProvider(
                                service.GetService<ILogger<MockedEthereumProvider>>(),
                                service.GetService<WalletOperationService>(),
                                service.GetService<EventHistoryService>(),
                                service.GetService<RandomEntropyService>(),
                                service.GetService<VersionControl>(),
                                service.GetService<IConfiguration>()))
                        );

//                    services.Replace(ServiceDescriptor.Singleton<IHostedService, EthereumProvider>(
//                        serviceProvider => serviceProvider.GetService<MockedEthereumProvider>()
//                    ));

                        services.Replace(ServiceDescriptor.Singleton<BitcoinProvider>(service =>
                            _btcProvider = new MockedBitcoinProvider(
                                service.GetService<ILogger<MockedBitcoinProvider>>(),
                                service.GetService<WalletOperationService>(),
                                service.GetService<EventHistoryService>(),
                                service.GetService<RandomEntropyService>(),
                                service.GetService<VersionControl>(),
                                service.GetService<IConfiguration>()))
                        );

//                    services.Replace(ServiceDescriptor.Singleton<IHostedService, BitcoinProvider>(
//                        serviceProvider => serviceProvider.GetService<MockedBitcoinProvider>()
//                    ));
                    });

                    // NOTE: issue: a hosted service doesn't ensure the initialization is done before processing requests!
                });
            _walletService.CreateClient();

            // Make sure this is okay
            Assert.Equal(0, _eventHistoryRepository.Events().Find(e => true).CountDocuments());
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
            _ethProvider.FirstDeposit = true;
            _btcProvider.FirstDeposit = true;
            // Integrate all wallets + 2 deposits
            Integrate(() => GetWallets().Wait(), GlobalConfiguration.Currencies.Length + 2);
            //Try(10, () =>
            //{
            var wallets = GetWallets().Result;
            Assert.Equal(100,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                ).Balance);
            Assert.Equal(80,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                ).Balance);
            //});

            // Try to properly withdraw
            Integrate(async () =>
                Assert.Null(await WalletWithdraw(MockedEthereumProvider.ETH, "mockedPublicKey", 50))
            );
            // Try to withdraw unavailable funds; this would fail asynchronously without error message if there were enough funds for consolidation
            Assert.StartsWith(
                "There are probably not enough collective funds",
                await WalletWithdraw(MockedEthereumProvider.ETH, "mockedPublicKey", 60)
            );

            // Give some time to the second withdrawal, as its failure would still make the test pass
            Task.Delay(3000).Wait();

            // Make sure the balance gets updated
            Try(10, () =>
            {
                wallets = GetWallets().Result;
                Assert.Equal(50,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                    ).Balance);
            });

            AfterTest();
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

            _ethProvider.FirstDeposit = true;
            _btcProvider.FirstDeposit = true;
            // Integrate all wallets + 2 deposits
            Integrate(() => GetWallets().Wait(), GlobalConfiguration.Currencies.Length + 2);
            // Assume test users have large balance: we generate wallets, and then wait for a mocked balance population
            //Try(10, () =>
            //{
            var wallets = GetWallets().Result;
            Assert.Equal(100,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                ).Balance);
            Assert.Equal(80,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                ).Balance);
            //});

            // Second user will need to own generated wallets too later on when trading
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_2");

            _ethProvider.FirstDeposit = true;
            _btcProvider.FirstDeposit = true;
            // Integrate all wallets + 2 deposits
            Integrate(() => GetWallets().Wait(), GlobalConfiguration.Currencies.Length + 2);
            // Assume test users have large balance: we generate wallets, and then wait for a mocked balance population
            //Try(10, () =>
            //{
            wallets = GetWallets().Result;
            Assert.Equal(100,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                ).Balance);
            Assert.Equal(80,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                ).Balance);
            //});

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_1");

            // There is usually a large timeout following
            Task.Delay(4000).Wait();

            // Prepare a buy order of 3.5 at price 0.2
            Integrate(async () => await LimitOrder(OrderSide.Buy, 2.5m, 0.2m));
            _logger.LogInformation("Placed limit buy 2.5 @ 0.2");
            Integrate(async () => await LimitOrder(OrderSide.Buy, 1, 0.2m));
            _logger.LogInformation("Placed limit buy 1 @ 0.2");
            // Consume both buy orders at price 0.2, and add a new sell order of 1 at price 0.1
            Integrate(async () => await LimitOrder(OrderSide.Sell, 4.5m, 0.1m));
            _logger.LogInformation("Placed limit sell 4.5 @ 0.1");

            Task.Delay(3000).Wait();
            // Wait for Trading Service to finish processing events
            //Try(10, () =>
            //{
            // Expect the user balances to stay unchanged after equal buy and sell of 3.5 * price 0.2 = 0.7
            wallets = GetWallets().Result;
            Assert.Equal(100,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                ).Balance);
            Assert.Equal(80,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                ).Balance);
            //});

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
            await LimitOrder(OrderSide.Buy, 2, 0.8m);
            _logger.LogInformation("Placed limit buy 2 @ 0.8");

            // Wait for Trading Service to finish processing events
            Try(10, () =>
            {
                // Make sure there is the one partially filled expected order for the second test user
                orders = GetOrders().Result;
                var singleOrder = Assert.Single(orders);
                Assert.Equal(0.8m, singleOrder.LimitPrice);
                Assert.Equal(2, singleOrder.Qty);
                Assert.Equal(1, singleOrder.FilledQty);

                // Expect the user balance to change after an ETH buy of 1 * price 0.1 = 0.1
                wallets = GetWallets().Result;
                Assert.Equal(101,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                    ).Balance);
                Assert.Equal(79.9m,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                    ).Balance);
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

            // Slowly prepare to cleanup by consuming half (0.5) of the remaining position @ 0.8
            await LimitOrder(OrderSide.Sell, 0.5m, 0.8m);
            _logger.LogInformation("Placed limit sell 0.5 @ 0.8");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_2");

            // Close the remaining order
            await CloseOrder((await GetOrders()).Single());
            _logger.LogInformation("Closed limit sell 0.5/2 @ 0.8");
            Task.Delay(2000).Wait();

            //Make sure it's closed
            Try(10, () =>
            {
                Assert.Empty(GetOrders().Result);
                ordersHistory = GetOrdersHistory(1).Result;
                Assert.Equal(2, ordersHistory.Single().Qty);
                Assert.Equal(1.5m, ordersHistory.Single().FilledQty);
                Assert.Equal(0.8m, ordersHistory.Single().LimitPrice);
                Assert.Equal(StatusEnum.CancelledEnum, ordersHistory.Single().Status);
            });

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_1");

            // Wait for Trading Service to finish processing events
            Try(10, () =>
            {
                // Make sure the depth is empty at the end
                depth = GetDepth().Result;
                Assert.Empty(depth.Asks);
                Assert.Empty(depth.Bids);

                // Expect the user balance to change after an ETH sell of 1 at 0.1 and another sell of 0.5 at 0.8
                wallets = GetWallets().Result;
                Assert.Equal(98.5m,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                    ).Balance);
                Assert.Equal(80.5m,
                    Assert.Single(
                        wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                    ).Balance);
            });

            AfterTest();
        }

        [Fact]
        public async Task T99_PopulateMockHistory2_2()
        {
            await T99_PopulateMockHistory(2, 2);
        }

        [Fact]
        public async Task T99_PopulateMockHistory6_50()
        {
            await T99_PopulateMockHistory(6, 50);
        }

//        [Theory]
//        // Fast test to check consolidation
//        [InlineData(2, 2)]
//        // Long test that populates history
//        [InlineData(6, 50)]
        public async Task T99_PopulateMockHistory(int days, int eventsPerDay)
        {
            // Generate the wallets

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_1");
            _ethProvider.FirstDeposit = true;
            _btcProvider.FirstDeposit = true;
            // Integrate all wallets + 2 deposits
            Integrate(() => GetWallets().Wait(), GlobalConfiguration.Currencies.Length + 2);
            //Try(10, () =>
            //{
            var wallets = GetWallets().Result;
            Assert.Equal(100,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                ).Balance);
            Assert.Equal(80,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                ).Balance);
            //});

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test_2");
            _ethProvider.FirstDeposit = true;
            _btcProvider.FirstDeposit = true;
            // Integrate all wallets + 2 deposits
            Integrate(() => GetWallets().Wait(), GlobalConfiguration.Currencies.Length + 2);
            //Try(10, () =>
            //{
            wallets = GetWallets().Result;
            Assert.Equal(100,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedEthereumProvider.ETH)
                ).Balance);
            Assert.Equal(80,
                Assert.Single(
                    wallets, wallet => wallet.CoinSymbol.Equals(MockedBitcoinProvider.BTC)
                ).Balance);
            //});

            const int secondsPerDay = 24 * 60 * 60;
            var pseudoRandom = new Random(1234567890);
            var now = DateTime.Now;
            decimal[] orders = {0, 0};
            for (var day = 0; day < days; day++)
            {
                var time = now.Subtract(TimeSpan.FromDays(days - day));
                for (var i = 0; i < eventsPerDay; i++)
                {
                    var seconds = i * secondsPerDay / (float) eventsPerDay
                                  + (pseudoRandom.Next(200) / 100f - 1) * (secondsPerDay / (float) eventsPerDay);
                    time = time.AddSeconds(seconds);

                    _client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", $"test_{i % 2 + 1}");
                    var qty = pseudoRandom.Next(4000) / 1000m - 2;
                    if (qty == 0)
                    {
                        qty += 0.0001m;
                    }

                    var limit = pseudoRandom.Next(1000) / 1000m;

                    // Reverse the order if the wallet is becoming overdrawn
                    if (orders[i % 2] > 30 && qty > 0
                        || orders[i % 2] < -30 && qty < 0)
                    {
                        qty = -qty;
                    }

                    orders[i % 2] += qty * limit;

                    try
                    {
                        TemporalEventHistoryService.MockedCurrentTime = time;
                        await LimitOrder(qty > 0 ? OrderSide.Buy : OrderSide.Sell, qty > 0 ? qty : -qty, limit);
                    }
                    catch (Exception)
                    {
                        // If cannot put an order, take the existing order of the opposite side
                        await LimitOrder(
                            qty < 0 ? OrderSide.Buy : OrderSide.Sell,
                            qty > 0 ? qty : -qty,
                            GetDepth().Result.Asks[0][0].Value);
                    }
                }
            }

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"test_1");

            // If the order count keeps changing, then we still have to wait a little (this is assert of timeout < 5s)
            IEnumerable<Order> openOrders;
            do
            {
                openOrders = GetOrders().Result;
                Task.Delay(5000).Wait();
            }
            while (GetOrders().Result.Count() != openOrders.Count());

            foreach (var openOrder in openOrders)
            {
                await CloseOrder(openOrder);
            }

            wallets = await GetWallets();
            foreach (var wallet in wallets)
            {
                // TODO: safely handle also very small values of balance < fee
                if (wallet.Balance == 0)
                {
                    continue;
                }

                Integrate(async () =>
                    Assert.Null(await WalletWithdraw(wallet.CoinSymbol, "mockedCleanupKey", wallet.Balance))
                );
            }

            Task.Delay(2000).Wait();

            foreach (var wallet in await GetWallets())
            {
                Assert.Equal(0, wallet.Balance);
            }

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"test_2");
            foreach (var openOrder in GetOrders().Result)
            {
                await CloseOrder(openOrder);
            }

            // Note that consolidation also happens here
            wallets = await GetWallets();
            foreach (var wallet in wallets)
            {
                // TODO: safely handle also very small values of balance < fee
                if (wallet.Balance == 0)
                {
                    continue;
                }

                Integrate(async () =>
                    Assert.Null(await WalletWithdraw(wallet.CoinSymbol, "mockedCleanupKey", wallet.Balance))
                );
            }

            Task.Delay(2000).Wait();

            foreach (var wallet in await GetWallets())
            {
                Assert.Equal(0, wallet.Balance);
            }

            AfterTest();
        }

        private void AfterTest()
        {
            // TODO: somehow make sure the previous hosted background service is stopped,
            // because there is a hazard of previous test creating a high-versioned event
            _tradingServiceEventHistoryService.Stopped = true;
            _walletServiceEventHistoryService.Stopped = true;
            _tradingService.Server.Host.StopAsync();
            _walletService.Server.Host.StopAsync();
//            _tradingService.Dispose();
//            _walletService.Dispose();
//            _client.Dispose();
//            _viewClient.Dispose();
            Task.Delay(4000).Wait();
            _logger.LogInformation("Test ended");
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
                await _client.GetAsync($"{UserPrefix}accounts/0/wallets"),
                true
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
                ),
                true
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

        private async Task<InlineResponse2005D> LimitOrder(OrderSide orderSide, decimal qty, decimal limitPrice)
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

        private async Task<InlineResponse2007> CloseOrder(Order openOrder)
        {
            var result = await RequestContentsOrError<InlineResponse2007>(
                await _client.DeleteAsync($"{TradingPrefix}accounts/0/orders/{openOrder.Id}"),
                true
            );
            Assert.Null(result.Errmsg);
            Assert.Equal(Status.OkEnum, result.S);
            return result;
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

        private void Integrate(Action action, int events = 1)
        {
            var oldVersion = _eventHistoryRepository
                                 .Events()
                                 .Find(e => true)
                                 .SortByDescending(e => e.VersionNumber)
                                 .FirstOrDefault()
                                 ?.VersionNumber ?? 0;
            action();
            _tradingServiceEventHistoryService.VersionControl.WaitForIntegration(oldVersion + events);
            _walletServiceEventHistoryService.VersionControl.WaitForIntegration(oldVersion + events);
        }
    }
}

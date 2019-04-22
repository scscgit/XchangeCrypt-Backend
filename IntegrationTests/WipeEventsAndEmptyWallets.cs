using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Providers;
using Xunit;

namespace XchangeCrypt.Backend.Tests.IntegrationTests
{
    public class WipeEventsAndEmptyWallets
    {
        private class FakeEventHistoryService : EventHistoryService
        {
            public FakeEventHistoryService(
                EventHistoryRepository eventHistoryRepository,
                VersionControl versionControl,
                ILogger<EventHistoryService> logger
            ) : base(eventHistoryRepository, versionControl, logger)
            {
            }

            public override async Task<IList<EventEntry>> Persist(
                IEnumerable<EventEntry> eventTransaction,
                long? alreadyLockedVersionNumber = null)
            {
                return null;
            }

            public override async Task<IList<EventEntry>> LoadMissingEvents(
                long currentVersionNumber,
                long? maxVersionNumber = null)
            {
                return new List<EventEntry>();
            }
        }

        private const string TestingConnectionString =
            "mongodb+srv://test:test@cluster0-8dep5.azure.mongodb.net/test?retryWrites=true";

        public WipeEventsAndEmptyWallets()
        {
        }

        [Fact]
        public async Task WipeEventsRestoringHotWallets()
        {
            var walletService = new WebApplicationFactory<WalletService.Startup>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.Replace(ServiceDescriptor.Singleton<EventHistoryService>(service =>
                            new FakeEventHistoryService(
                                null,
                                service.GetService<VersionControl>(),
                                service.GetService<Logger<EventHistoryService>>()
                            )
                        )
                    );
                });
            });
            walletService.CreateClient();

            // Wipe the testing DB
            var eventHistoryRepository = new EventHistoryRepository(new DataAccess(TestingConnectionString));
            eventHistoryRepository.Events().DeleteMany(Builders<EventEntry>.Filter.Where(e => true));

            var walletRepo = new WalletRepository(new DataAccess(TestingConnectionString)).HotWallets();
            var versionNumber = 1;
            walletRepo.Find(e => true)
                .ToList()
                .ForEach(hotwallet =>
                {
                    Task<decimal> balance;
                    try
                    {
                        balance = AbstractProvider.ProviderLookup[hotwallet.CoinSymbol]
                            .GetBalance(hotwallet.PublicKey);
                        balance.Wait(2_000);
                        if (balance.IsCompletedSuccessfully && balance.Result == 0)
                        {
                            walletRepo.DeleteOne(e => e.Id.Equals(hotwallet.Id));
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        // When we are not sure, we keep the wallet (you can breakpoint this)
                        var error = e.Message.Trim();
                    }

                    walletRepo.UpdateOne(
                        Builders<HotWallet>.Filter.Eq(e => e.Id, hotwallet.Id),
                        Builders<HotWallet>.Update.Set(
                            e => e.CreatedOnVersionNumber, versionNumber)
                    );

                    var now = DateTime.Now;
                    eventHistoryRepository.Events().InsertMany(new EventEntry[]
                    {
                        new WalletGenerateEventEntry
                        {
                            VersionNumber = versionNumber,
                            User = hotwallet.User,
                            AccountId = hotwallet.AccountId,
                            EntryTime = now,
                            CoinSymbol = hotwallet.CoinSymbol,
                            LastWalletPublicKey = hotwallet.PublicKey,
                            NewSourcePublicKeyBalance = 0,
                        },
                        new TransactionCommitEventEntry
                        {
                            VersionNumber = versionNumber++,
                            EntryTime = now,
                        }
                    });
                });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using XchangeCrypt.Backend.TradingBackend.Models;
using XchangeCrypt.Backend.TradingBackend.Models.Enums;
using XchangeCrypt.Backend.TradingBackend.Repositories;
using XchangeCrypt.Backend.TradingBackend.Services;

namespace XchangeCrypt.Backend.TradingBackend.Controllers
{
    [Route("api/v1/monitorapi/")]
    public class MonitorController : Controller
    {
        private readonly MonitorService _monitorService;
        private readonly ActivityHistoryRepository _activityHistoryRepository;

        public MonitorController(MonitorService monitorService, ActivityHistoryRepository activityHistoryRepository)
        {
            _monitorService = monitorService;
            _activityHistoryRepository = activityHistoryRepository;
        }

        [HttpGet("errors")]
        public IEnumerable<string> Errors()
        {
            return _monitorService.GetErrors();
        }

        [HttpGet("last_message")]
        public string LastMessage()
        {
            return _monitorService.LastMessage;
        }

        [HttpGet("order_history")]
        public IEnumerable<ActivityHistoryOrderEntry> OrderHistory(
            [FromQuery]int? count)
        {
            // Debug testing
            _activityHistoryRepository.Orders().InsertOne(new ActivityHistoryOrderEntry
            {
                EntryTime = DateTime.Now,
                User = "test@testuser",
                AccountId = "0",
                Instrument = "BTC:USD",
                Qty = 0.335m,
                Side = OrderSide.Buy,
                Type = OrderType.Limit,
                LimitPrice = 9_770.56m,
            });

            return _activityHistoryRepository
                .Orders()
                .Find(ActivityHistoryRepository.OrdersFilter)
                .Sort(Builders<ActivityHistoryOrderEntry>.Sort.Descending(e => e.EntryTime))
                .Limit(count)
                .ToList();
        }

        [HttpGet("account_history")]
        public IEnumerable<ActivityHistoryWalletOperationEntry> AccountHistory(
            [FromQuery]int? count)
        {
            // Debug testing
            _activityHistoryRepository.WalletOperations().InsertOne(new ActivityHistoryWalletOperationEntry
            {
                EntryTime = DateTime.Now,
                User = "test@testuser",
                AccountId = "0",
                CoinSymbol = "BTC",
                DepositType = "Wallet backend",
                WithdrawalType = null,
                Amount = 23.1M,
            });

            return _activityHistoryRepository
                .WalletOperations()
                .Find(ActivityHistoryRepository.WalletOperationsFilter)
                .Sort(Builders<ActivityHistoryWalletOperationEntry>.Sort.Descending(e => e.EntryTime))
                .Limit(count)
                .ToList();
        }

        [HttpGet("purge_test")]
        public void PurgeTest()
        {
            _activityHistoryRepository
                .Orders()
                .DeleteMany(ActivityHistoryRepository.OrdersFilter & Builders<ActivityHistoryOrderEntry>.Filter.Where(e => e.User.Equals("test@testuser")));
            _activityHistoryRepository
                .WalletOperations()
                .DeleteMany(ActivityHistoryRepository.WalletOperationsFilter & Builders<ActivityHistoryWalletOperationEntry>.Filter.Where(e => e.User.Equals("test@testuser")));
        }
    }
}

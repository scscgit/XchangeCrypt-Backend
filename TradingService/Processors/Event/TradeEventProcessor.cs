using System;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.TradingService.Services;

namespace XchangeCrypt.Backend.TradingService.Processors.Event
{
    public class TradeEventProcessor
    {
        private readonly TradingOrderService _tradingOrderService;
        private readonly ILogger<TradeEventProcessor> _logger;

        public TradeEventProcessor(
            TradingOrderService tradingOrderService,
            ILogger<TradeEventProcessor> logger)
        {
            _tradingOrderService = tradingOrderService;
            _logger = logger;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        // ReSharper disable once UnusedParameter.Global
        public void ProcessEvent(EventEntry eventEntry)
        {
            throw new NotSupportedException();
        }

        public void ProcessEvent(CancelOrderEventEntry eventEntry)
        {
            _tradingOrderService.CancelOrder(eventEntry);
        }

        public void ProcessEvent(CreateOrderEventEntry eventEntry)
        {
            _tradingOrderService.CreateOrder(eventEntry);
        }

        public void ProcessEvent(MatchOrderEventEntry eventEntry)
        {
            _tradingOrderService.MatchOrder(eventEntry);
        }

        public void ProcessEvent(TransactionCommitEventEntry eventEntry)
        {
            // Ignored
        }

        public void ProcessEvent(WalletDepositEventEntry eventEntry)
        {
            throw new NotImplementedException();
        }

        public void ProcessEvent(WalletRevokeEventEntry eventEntry)
        {
            throw new NotImplementedException();
        }

        public void ProcessEvent(WalletWithdrawalEventEntry eventEntry)
        {
            throw new NotImplementedException();
        }
    }
}

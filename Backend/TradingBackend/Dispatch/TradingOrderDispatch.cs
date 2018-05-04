using Microsoft.Azure.ServiceBus;
using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Services;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingBackend.Dispatch
{
    public class TradingOrderDispatch
    {
        private readonly LimitOrderService _limitOrderService;
        private readonly StopOrderService _stopOrderService;
        private readonly MarketOrderService _marketOrderService;

        public TradingOrderDispatch(
            LimitOrderService limitOrderService,
            StopOrderService stopOrderService,
            MarketOrderService marketOrderService)
        {
            _limitOrderService = limitOrderService;
            _stopOrderService = stopOrderService;
            _marketOrderService = marketOrderService;
        }

        /// <summary>
        /// Dispatches proper handlers to handle a trading order operation.
        /// </summary>
        /// <param name="message">Message to be procesed</param>
        /// <param name="reportInvalidMessage">Error handler to call if the intended handler experienced error. Parameter is error message</param>
        internal Task Dispatch(Message message, Func<string, Task> reportInvalidMessage)
        {
            switch (message.UserProperties[ParameterNames.Type])
            {
                case OrderTypes.LimitOrder:
                    switch (message.UserProperties[ParameterNames.Side])
                    {
                        case OrderSides.BuySide:
                            return _limitOrderService.Buy(
                                (string)message.UserProperties[ParameterNames.User],
                                (string)message.UserProperties[ParameterNames.AccountId],
                                (string)message.UserProperties[ParameterNames.Instrument],
                                (decimal?)message.UserProperties[ParameterNames.Quantity],
                                (string)message.UserProperties[ParameterNames.Side],
                                (string)message.UserProperties[ParameterNames.Type],
                                (decimal?)message.UserProperties[ParameterNames.LimitPrice],
                                (string)message.UserProperties[ParameterNames.DurationType],
                                (decimal?)message.UserProperties[ParameterNames.Duration],
                                (decimal?)message.UserProperties[ParameterNames.StopLoss],
                                (decimal?)message.UserProperties[ParameterNames.TakeProfit],
                                (string)message.UserProperties[ParameterNames.RequestId]);

                        case OrderSides.SellSide:
                            return _limitOrderService.Sell(
                                (string)message.UserProperties[ParameterNames.User],
                                (string)message.UserProperties[ParameterNames.AccountId],
                                (string)message.UserProperties[ParameterNames.Instrument],
                                (decimal?)message.UserProperties[ParameterNames.Quantity],
                                (string)message.UserProperties[ParameterNames.Side],
                                (string)message.UserProperties[ParameterNames.Type],
                                (decimal?)message.UserProperties[ParameterNames.LimitPrice],
                                (string)message.UserProperties[ParameterNames.DurationType],
                                (decimal?)message.UserProperties[ParameterNames.Duration],
                                (decimal?)message.UserProperties[ParameterNames.StopLoss],
                                (decimal?)message.UserProperties[ParameterNames.TakeProfit],
                                (string)message.UserProperties[ParameterNames.RequestId]);

                        default:
                            return reportInvalidMessage($"Unrecognized limit order side {message.UserProperties[ParameterNames.Side]}");
                    }

                case OrderTypes.StopOrder:
                    switch (message.UserProperties[ParameterNames.Side])
                    {
                        case OrderSides.BuySide:
                            return _stopOrderService.Buy(
                                (string)message.UserProperties[ParameterNames.User],
                                (string)message.UserProperties[ParameterNames.AccountId],
                                (string)message.UserProperties[ParameterNames.Instrument],
                                (decimal?)message.UserProperties[ParameterNames.Quantity],
                                (string)message.UserProperties[ParameterNames.Side],
                                (string)message.UserProperties[ParameterNames.Type],
                                (decimal?)message.UserProperties[ParameterNames.StopPrice],
                                (string)message.UserProperties[ParameterNames.DurationType],
                                (decimal?)message.UserProperties[ParameterNames.Duration],
                                (decimal?)message.UserProperties[ParameterNames.StopLoss],
                                (decimal?)message.UserProperties[ParameterNames.TakeProfit],
                                (string)message.UserProperties[ParameterNames.RequestId]);

                        case OrderSides.SellSide:
                            return _stopOrderService.Sell(
                                (string)message.UserProperties[ParameterNames.User],
                                (string)message.UserProperties[ParameterNames.AccountId],
                                (string)message.UserProperties[ParameterNames.Instrument],
                                (decimal?)message.UserProperties[ParameterNames.Quantity],
                                (string)message.UserProperties[ParameterNames.Side],
                                (string)message.UserProperties[ParameterNames.Type],
                                (decimal?)message.UserProperties[ParameterNames.StopPrice],
                                (string)message.UserProperties[ParameterNames.DurationType],
                                (decimal?)message.UserProperties[ParameterNames.Duration],
                                (decimal?)message.UserProperties[ParameterNames.StopLoss],
                                (decimal?)message.UserProperties[ParameterNames.TakeProfit],
                                (string)message.UserProperties[ParameterNames.RequestId]);

                        default:
                            return reportInvalidMessage($"Unrecognized stop order side {message.UserProperties[ParameterNames.Side]}");
                    }

                case OrderTypes.MarketOrder:
                    switch (message.UserProperties[ParameterNames.Side])
                    {
                        case OrderSides.BuySide:
                            return _marketOrderService.Buy(
                                (string)message.UserProperties[ParameterNames.User],
                                (string)message.UserProperties[ParameterNames.AccountId],
                                (string)message.UserProperties[ParameterNames.Instrument],
                                (decimal?)message.UserProperties[ParameterNames.Quantity],
                                (string)message.UserProperties[ParameterNames.Side],
                                (string)message.UserProperties[ParameterNames.Type],
                                (string)message.UserProperties[ParameterNames.DurationType],
                                (decimal?)message.UserProperties[ParameterNames.Duration],
                                (decimal?)message.UserProperties[ParameterNames.StopLoss],
                                (decimal?)message.UserProperties[ParameterNames.TakeProfit],
                                (string)message.UserProperties[ParameterNames.RequestId]);

                        case OrderSides.SellSide:
                            return _marketOrderService.Sell(
                                (string)message.UserProperties[ParameterNames.User],
                                (string)message.UserProperties[ParameterNames.AccountId],
                                (string)message.UserProperties[ParameterNames.Instrument],
                                (decimal?)message.UserProperties[ParameterNames.Quantity],
                                (string)message.UserProperties[ParameterNames.Side],
                                (string)message.UserProperties[ParameterNames.Type],
                                (string)message.UserProperties[ParameterNames.DurationType],
                                (decimal?)message.UserProperties[ParameterNames.Duration],
                                (decimal?)message.UserProperties[ParameterNames.StopLoss],
                                (decimal?)message.UserProperties[ParameterNames.TakeProfit],
                                (string)message.UserProperties[ParameterNames.RequestId]);

                        default:
                            return reportInvalidMessage($"Unrecognized market order side {message.UserProperties[ParameterNames.Side]}");
                    }

                default:
                    return reportInvalidMessage($"Unrecognized order type {message.UserProperties[ParameterNames.Type]}");
            }
        }
    }
}

namespace XchangeCrypt.Backend.ConstantsLibrary
{
    public class MessagingConstants
    {
        public class ParameterNames
        {
            // Main
            public const string MessageType = "MessageType";

            public const string User = "User";
            public const string AccountId = "AccountId";
            public const string Instrument = "Instrument";

            // Trading
            public const string Quantity = "Quantity";

            public const string Side = "Side";
            public const string Type = "Type";
            public const string LimitPrice = "LimitPrice";
            public const string StopPrice = "StopPrice";
            public const string DurationType = "DurationType";
            public const string Duration = "Duration";
            public const string StopLoss = "StopLoss";
            public const string TakeProfit = "TakeProfit";

            // Misc trading
            public const string RequestId = "RequestId";
        }

        public class MessageTypes
        {
            public const string TradingOrder = "TradingOrder";
        }

        public class OrderTypes
        {
            public const string LimitOrder = "limit";
            public const string StopOrder = "stop";
            public const string MarketOrder = "market";
        }

        public class OrderSides
        {
            public const string BuySide = "buy";
            public const string SellSide = "sell";
        }
    }
}

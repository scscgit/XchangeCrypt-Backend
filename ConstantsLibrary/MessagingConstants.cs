namespace XchangeCrypt.Backend.ConstantsLibrary
{
    public static class MessagingConstants
    {
        public static class ParameterNames
        {
            // Main

            public const string MessageType = "MessageType";
            public const string User = "User";
            public const string AccountId = "AccountId";

            // Trade order

            public const string Instrument = "Instrument";
            public const string Quantity = "Quantity";

            public const string Side = "Side";
            public const string OrderType = "OrderType";
            public const string LimitPrice = "LimitPrice";
            public const string StopPrice = "StopPrice";
            public const string DurationType = "DurationType";
            public const string Duration = "Duration";
            public const string StopLoss = "StopLoss";
            public const string TakeProfit = "TakeProfit";

            // Wallet operation

            public const string WalletCommandType = "WalletCommandType";
            public const string CoinSymbol = "CoinSymbol";
            public const string WithdrawalTargetPublicKey = "WithdrawalTargetPublicKey";
            public const string Amount = "Amount";
            public const string WalletEventIdReference = "WalletEventIdReference";

            // Misc

            public const string RequestId = "RequestId";

            // Command answers

            public const string AnswerQueuePostfix = "AnswerQueuePostfix";
            public const string ErrorIfAny = "ErrorIfAny";
        }

        public static class MessageTypes
        {
            public const string TradeOrder = "TradeOrder";
            public const string WalletOperation = "WalletOperation";
        }

        public static class OrderTypes
        {
            public const string LimitOrder = "limit";
            public const string StopOrder = "stop";
            public const string MarketOrder = "market";
        }

        public static class OrderSides
        {
            public const string BuySide = "buy";
            public const string SellSide = "sell";
        }

        public static class WalletCommandTypes
        {
            public const string Generate = "Generate";
            public const string Deposit = "Deposit";
            public const string Withdrawal = "Withdrawal";
            public const string RevokeDeposit = "RevokeDeposit";
            public const string RevokeWithdrawal = "RevokeWithdrawal";
        }
    }
}

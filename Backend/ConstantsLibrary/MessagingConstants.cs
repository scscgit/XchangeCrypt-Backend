namespace XchangeCrypt.Backend.ConstantsLibrary
{
    public class MessagingConstants
    {
        public class ParameterNames
        {
            public const string MessageType = "MessageType";
            public const string Side = "Side";
        }

        public class MessageTypes
        {
            public const string LimitOrder = "LimitOrder";
        }

        public class LimitOrderSides
        {
            public const string Buy = "buy";
            public const string Sell = "sell";
        }
    }
}

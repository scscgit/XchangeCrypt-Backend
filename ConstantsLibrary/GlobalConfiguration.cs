namespace XchangeCrypt.Backend.ConstantsLibrary
{
    public static class GlobalConfiguration
    {
        public static readonly string[] Currencies =
        {
            "ETH",
            "BTC",
            "LTC",
        };

        public static readonly (string, string)[] Instruments =
        {
            ("ETH", "BTC"),
            ("LTC", "BTC"),
            ("LTC", "ETH"),
        };
    }
}

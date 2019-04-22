using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace XchangeCrypt.Backend.WalletService.Providers.XCT
{
    [Function("transfer", "bool")]
    public class XctTransferFunction : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; }

        [Parameter("address", "to", 2)]
        public virtual string To { get; set; }

        [Parameter("uint256", "value", 3)]
        public virtual BigInteger Value { get; set; }
    }
}

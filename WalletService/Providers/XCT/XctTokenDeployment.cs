using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.StandardTokenEIP20;
using Nethereum.Util;
using Nethereum.Web3;

namespace XchangeCrypt.Backend.WalletService.Providers.XCT
{
    public class XctTokenDeployment : Nethereum.StandardTokenEIP20.ContractDefinition.EIP20Deployment
    {
        public XctTokenDeployment() : base(BYTECODE)
        {
        }

        public static async Task<TransactionReceipt> Deploy(Web3 web3)
        {
            const int initialAmount = 200;
            const byte decimalUnits = 18;
            return await web3.Eth
                .GetContractDeploymentHandler<XctTokenDeployment>()
                .SendRequestAndWaitForReceiptAsync(new XctTokenDeployment
                {
                    InitialAmount =
                        BigInteger.Parse($"{new BigDecimal(initialAmount, decimalUnits, false).ToString()}"),
                    TokenName = "XchangeCryptTestToken",
                    DecimalUnits = decimalUnits,
                    TokenSymbol = "XCT",
                });
        }
    }
}

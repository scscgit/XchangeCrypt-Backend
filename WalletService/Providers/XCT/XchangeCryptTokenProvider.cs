using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.XCT
{
    public class XchangeCryptTokenProvider : EthereumTokenProvider //<XctTransferFunction>
    {
        public XchangeCryptTokenProvider(
            ILogger<XchangeCryptTokenProvider> logger,
            WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService,
            RandomEntropyService randomEntropyService,
            VersionControl versionControl,
            IConfiguration configuration)
            : base(
                "XCT",
                "0x58ddc05087f17F84fC7E7DC41a0c67C1003658b3",
                logger,
                walletOperationService,
                eventHistoryService,
                randomEntropyService,
                versionControl,
                configuration)
        {
            if (GetType() == typeof(XchangeCryptTokenProvider))
            {
                // Do not implicitly call in (mocked) subclasses
                ProviderLookup[ThisCoinSymbol] = this;
            }
        }

        protected XctTransferFunction WithdrawalFunction(
            string walletPublicKeyUserReference, string withdrawToPublicKey, decimal valueExclFee)
        {
            return new XctTransferFunction
            {
                From = walletPublicKeyUserReference,
                //FromAddress = walletPublicKeyUserReference,
                To = withdrawToPublicKey,
                Value = Web3.Convert.ToWei(valueExclFee),
                GasPrice = Web3.Convert.ToWei(_withdrawalGasPriceGwei * Gwei),
                //Gas = Web3.Convert.ToWei(EthFee()),
            };
        }

        public override async Task<bool> Withdraw(
            string walletPublicKeyUserReference, string withdrawToPublicKey, decimal valueExclFee)
        {
            var function = WithdrawalFunction(walletPublicKeyUserReference, withdrawToPublicKey, valueExclFee);
            var transferHandler = new Web3(
                new Account(
                    await GetPrivateKeyFromHdWallet(
                        _walletOperationService.GetHotWallet(walletPublicKeyUserReference, ThisCoinSymbol).HdSeed
                    )
                ),
                Web3Url
            ).Eth.GetContractTransactionHandler<XctTransferFunction>();
            var result = await transferHandler.SendRequestAndWaitForReceiptAsync(_contractAddress, function);
            return result.HasErrors() ?? true;
        }
    }
}

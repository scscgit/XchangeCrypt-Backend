using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers
{
    public abstract class BitcoinForkProvider : SimpleProvider
    {
        public int AltCoinKeyNumber;
        protected readonly Network net;
        protected Node node;
        private WalletOperationService _walletOperationService;
        private decimal _withdrawalFee;

        /// <param name="altCoinKeyNumber">https://github.com/satoshilabs/slips/blob/master/slip-0044.md</param>
        public BitcoinForkProvider(
            string thisCoinSymbol,
            int altCoinKeyNumber,
            Network network,
            ILogger logger,
            WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService,
            RandomEntropyService randomEntropyService,
            VersionControl versionControl,
            IConfiguration configuration)
            : base(
                thisCoinSymbol,
                logger,
                walletOperationService,
                eventHistoryService,
                randomEntropyService,
                versionControl,
                configuration)
        {
            _walletOperationService = walletOperationService;
            AltCoinKeyNumber = altCoinKeyNumber;
            net = network;

            _withdrawalFee = decimal.Parse(
                configuration[$"{thisCoinSymbol}:WithdrawalFee"] ??
                throw new ArgumentException($"{thisCoinSymbol}:WithdrawalFee"));

            ProviderLookup[ThisCoinSymbol] = this;
        }

        public void DeriveHdWalletKeys(string hdSeed, int keyNumber, out string publicAddress, out string privateKey)
        {
            ExtKey masterKey = new Mnemonic(hdSeed).DeriveExtKey();
            ExtKey key = masterKey.Derive(new KeyPath("m/44'/0'/0'/0/" + keyNumber));
            publicAddress = key.PrivateKey.PubKey.GetAddress(net).ToString();
            privateKey = key.PrivateKey.GetBitcoinSecret(net).ToString();
        }

        public void GetBtcBalance(
            string publicKey, bool isUnspentOnly, out decimal entireBalance, out decimal confirmedBalance)
        {
            entireBalance = 0.0M;
            confirmedBalance = 0.0M;
            QBitNinjaClient client = new QBitNinjaClient(net);
            BitcoinPubKeyAddress bitcoinPublicAddress;
            try
            {
                bitcoinPublicAddress = new BitcoinPubKeyAddress(publicKey, net);
            }
            catch (Exception)
            {
                // For development purposes, we weaken this criteria and don't throw a fatal error on invalid wallet
                _logger.LogWarning("Unsupported wallet registered as a Bitcoin-type wallet");
                entireBalance = confirmedBalance = GetCurrentlyCachedBalance(publicKey).Result;
                return;
            }

            var balance = client.GetBalance(bitcoinPublicAddress, isUnspentOnly).Result;

            if (balance.Operations.Count > 0)
            {
                var unspentCoins = new List<Coin>();
                var unspentCoinsConfirmed = new List<Coin>();
                foreach (var operation in balance.Operations)
                {
                    unspentCoins.AddRange(operation.ReceivedCoins.Select(coin => coin as Coin));
                    if (operation.Confirmations > 0)
                    {
                        unspentCoinsConfirmed.AddRange(operation.ReceivedCoins.Select(coin => coin as Coin));
                    }
                }

                entireBalance = unspentCoins.Sum(x => x.Amount.ToDecimal(MoneyUnit.BTC));
                confirmedBalance = unspentCoinsConfirmed.Sum(x => x.Amount.ToDecimal(MoneyUnit.BTC));
            }
        }

        public string BtcTransfer(string privateKey, decimal amount, string destinationPublicKey, decimal minerFee)
        {
            var transaction = Transaction.Create(net);
            var bitcoinPrivateKey = new BitcoinSecret(privateKey);
            var fromAddress = bitcoinPrivateKey.GetAddress().ToString();

            GetBtcBalance(
                fromAddress,
                true,
                out decimal addressBalance,
                out decimal addressBalanceConfirmed);

            if (addressBalanceConfirmed <= amount)
            {
                _logger.LogError("The address doesn't have enough funds!");
                return null;
            }

            QBitNinjaClient client = new QBitNinjaClient(net);
            var balance = client.GetBalance(new BitcoinPubKeyAddress(fromAddress, net), true).Result;

            //Add trx in
            //Get all transactions in for that address
            int txsIn = 0;
            if (balance.Operations.Count > 0)
            {
                var unspentCoins = new List<Coin>();
                foreach (var operation in balance.Operations)
                {
                    //string transaction = operation.TransactionId.ToString();
                    foreach (Coin receivedCoin in operation.ReceivedCoins)
                    {
                        OutPoint outpointToSpend = receivedCoin.Outpoint;
                        transaction.Inputs.Add(new TxIn {PrevOut = outpointToSpend});
                        transaction.Inputs[txsIn].ScriptSig = bitcoinPrivateKey.ScriptPubKey;
                        txsIn = txsIn + 1;
                    }
                }
            }

            //add address to send money
            var toPubKeyAddress = new BitcoinPubKeyAddress(destinationPublicKey, net);
            TxOut toAddressTxOut = new TxOut
            {
                Value = new Money((decimal) amount, MoneyUnit.BTC),
                ScriptPubKey = toPubKeyAddress.ScriptPubKey
            };
            transaction.Outputs.Add(toAddressTxOut);

            //add address to send change
            decimal change = addressBalance - amount - minerFee;
            if (change > 0)
            {
                var fromPubKeyAddress = new BitcoinPubKeyAddress(fromAddress, net);
                TxOut changeAddressTxOut = new TxOut
                {
                    Value = new Money((decimal) change, MoneyUnit.BTC),
                    ScriptPubKey = fromPubKeyAddress.ScriptPubKey
                };
                transaction.Outputs.Add(changeAddressTxOut);
            }

            transaction.Sign(bitcoinPrivateKey, false);
            BroadcastResponse broadcastResponse = client.Broadcast(transaction).Result;
            if (!broadcastResponse.Success)
            {
                _logger.LogError(
                    "Error broadcasting transaction " + broadcastResponse.Error.ErrorCode + " : " +
                    broadcastResponse.Error.Reason);
                return null;
            }

            var transactionId = transaction.GetHash().ToString();
            return transactionId;
        }

        public override async Task<decimal> GetBalance(string publicKey)
        {
            GetBtcBalance(publicKey, true, out _, out var confirmedBalance);
            return confirmedBalance;
        }

        public override async Task<string> GetPublicKeyFromHdWallet(string hdSeed)
        {
            DeriveHdWalletKeys(hdSeed, AltCoinKeyNumber, out var publicKey, out _);
            return publicKey;
        }

        public override async Task<string> GetPrivateKeyFromHdWallet(string hdSeed)
        {
            DeriveHdWalletKeys(hdSeed, AltCoinKeyNumber, out _, out var privateKey);
            return privateKey;
        }

        public override async Task<bool> Withdraw(
            string walletPublicKeyUserReference, string withdrawToPublicKey, decimal valueExclFee)
        {
            var privateKey = await GetPrivateKeyFromHdWallet(
                _walletOperationService.GetHotWallet(walletPublicKeyUserReference, ThisCoinSymbol).HdSeed);
            var transactionId = BtcTransfer(privateKey, valueExclFee, withdrawToPublicKey, Fee());
            return true;
        }

        public override decimal Fee()
        {
            return _withdrawalFee;
        }

        public override void Dispose()
        {
            base.Dispose();
            node?.Dispose();
        }
    }
}

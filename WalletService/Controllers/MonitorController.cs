using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using XchangeCrypt.Backend.WalletService.Providers.BTC;
using XchangeCrypt.Backend.WalletService.Providers.ETH;

namespace XchangeCrypt.Backend.WalletService.Controllers
{
    [Route("api/v1/monitor/")]
    [ApiController]
    public class MonitorController : ControllerBase
    {
        private readonly BitcoinProvider _bitcoinProvider;
        private readonly EthereumProvider _ethereumProvider;

        public MonitorController(BitcoinProvider bitcoinProvider, EthereumProvider ethereumProvider)
        {
            _bitcoinProvider = bitcoinProvider;
            _ethereumProvider = ethereumProvider;
        }

        [HttpGet]
        public ActionResult<string> Get()
        {
            return _bitcoinProvider.GetBalance("mtZPYjqc1PEx2AQyHY2wXCfyEQMe6QNfRZ")
                //return _ethereumProvider.GetBalance("0xde0b295669a9fd93d5f28d9ec85e40f4cb697bae")
                .Result
                .ToString(CultureInfo.CurrentCulture);
        }
    }
}

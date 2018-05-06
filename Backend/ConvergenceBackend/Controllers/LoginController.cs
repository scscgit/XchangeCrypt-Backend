using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using XchangeCrypt.Backend.ConvergenceBackend.Extensions.Authentication;
using XchangeCrypt.Backend.ConvergenceBackend.Models.Authentication;

namespace XchangeCrypt.Backend.ConvergenceBackend.Controllers
{
    /// <summary>
    /// Handles local authorization.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LoginController : Controller
    {
        private AzureAdB2COptions _azureAdB2COptions;

        /// <summary>
        /// </summary>
        public LoginController(IOptions<AzureAdB2COptions> azureAdB2COptions)
        {
            _azureAdB2COptions = azureAdB2COptions.Value;
        }

        /// <summary>
        /// Displays the session choices to the user.
        /// </summary>
        [Route("login")]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Redirects the user to a log in page and prints a response.
        /// </summary>
        [Route("api")]
        [Route("signin-oidc")]
        [Authorize]
        public async Task<IActionResult> Api()
        {
            string responseString = "";
            try
            {
                // Retrieve the token with the specified scopes
                var scope = _azureAdB2COptions.ApiScopes.Split(' ');
                string signedInUserID = User.GetIdentifier();
                TokenCache userTokenCache = new MSALSessionCache(signedInUserID, HttpContext).GetMsalCacheInstance();
                ConfidentialClientApplication cca = new ConfidentialClientApplication(
                    _azureAdB2COptions.ClientId,
                    _azureAdB2COptions.Authority,
                    _azureAdB2COptions.RedirectUri,
                    new ClientCredential(_azureAdB2COptions.ClientSecret),
                    userTokenCache, null);

                AuthenticationResult result = await cca.AcquireTokenSilentAsync(scope, cca.Users.FirstOrDefault(), _azureAdB2COptions.Authority, false);

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _azureAdB2COptions.ApiUrl);

                // Add token to the Authorization header and make the request
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = await client.SendAsync(request);

                // Handle the response
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        responseString = await response.Content.ReadAsStringAsync();
                        break;

                    case HttpStatusCode.Unauthorized:
                        responseString = $"Please sign in again. {response.ReasonPhrase}";
                        break;

                    default:
                        responseString = $"Error calling API. StatusCode=${response.StatusCode}";
                        break;
                }
            }
            catch (MsalUiRequiredException ex)
            {
                responseString = $"Session has expired. Please sign in again. {ex.Message}";
            }
            catch (Exception ex)
            {
                responseString = $"Error calling API: {ex.Message}";
            }

            ViewData["Payload"] = $"{responseString}";
            ViewData["Message"] = String.Format("Claims available for the user {0}", (User.FindFirst("name")?.Value));
            return Login();
        }
    }
}

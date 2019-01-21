using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace XchangeCrypt.Backend.ConvergenceService.Filters.Authentication
{
    public class AuthenticatedTestRequestMiddleware
    {
        private const bool AnyValue = false;
        private const string TestingCookieAuthentication = "TestCookieAuthentication";
        private const string AuthorizationHeader = "Authorization";
        private const string TestingHeaderValue = "Bearer: test";

        private const string TestUserName = "Testing user";
        private const string TestUserId = "1";

        private readonly RequestDelegate _next;

        public AuthenticatedTestRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.Keys.Contains(AuthorizationHeader) &&
                (AnyValue || context.Request.Headers[AuthorizationHeader][0].Equals(TestingHeaderValue)))
            {
                var claimsIdentity = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, TestUserName),
                    new Claim(ClaimTypes.NameIdentifier, TestUserId),
                }, TestingCookieAuthentication);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                context.User = claimsPrincipal;
            }

            await _next(context);
        }
    }
}

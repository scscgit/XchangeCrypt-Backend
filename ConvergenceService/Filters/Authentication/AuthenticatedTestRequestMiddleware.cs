using System;
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
        private const string TestingHeaderValue = "Bearer test_";

        private const string TestUserName = "Testing user";
        private const string TestUserId = "TEST_";

        private readonly RequestDelegate _next;

        public AuthenticatedTestRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.Keys.Contains(AuthorizationHeader) &&
                (AnyValue || context.Request.Headers[AuthorizationHeader][0].StartsWith(TestingHeaderValue)))
            {
                var testHeaderValue = context.Request.Headers[AuthorizationHeader][0];
                var claimsIdentity = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, TestUserName),
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        TestUserId + testHeaderValue.Remove(0, TestingHeaderValue.Length)
                    ),
                }, TestingCookieAuthentication);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                context.User = claimsPrincipal;
            }

            await _next(context);
        }
    }
}

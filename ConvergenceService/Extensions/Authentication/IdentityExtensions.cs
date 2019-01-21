using System;
using System.Security.Claims;

namespace XchangeCrypt.Backend.ConvergenceService.Extensions.Authentication
{
    public static class IdentityExtensions
    {
        public static string GetIdentifier(this ClaimsPrincipal user)
        {
            try
            {
                return user.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            catch (NullReferenceException e)
            {
                throw new Exception(
                    "Couldn't find authorized User's Identifier. Are you logged in and does the REST mapping use [Authorize]?",
                    e);
            }
        }
    }
}

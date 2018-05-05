using System.Security.Claims;

namespace XchangeCrypt.Backend.ConvergenceBackend.Extensions.Authentication
{
    public static class IdentityExtensions
    {
        public static string GetIdentifier(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier).Value;
        }
    }
}

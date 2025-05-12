using Microsoft.AspNetCore.Components.Authorization;
using MongoDB.Bson;
using System.Security.Claims;

namespace Noting.Helpers
{
    public static class AuthenticationStateProviderHelper
    {
        public static ObjectId? GetUserId(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var Id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return ObjectId.TryParse(Id, out var userId) ? userId : null;
        }
        public static async ValueTask<ObjectId?> GetUserId(this AuthenticationStateProvider auth)
        {
            var state = await auth.GetAuthenticationStateAsync();
            return state.User.GetUserId();
        }
    }
}

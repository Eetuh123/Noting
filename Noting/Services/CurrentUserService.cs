using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using Noting.Helpers;


namespace Noting.Services
{
    public interface ICurrentUserService
    {
        ValueTask<ObjectId?> GetUserIdAsync();
    }
    public class CurrentUserService : ICurrentUserService
    {
        private readonly AuthenticationStateProvider _auth;
        private readonly IHttpContextAccessor _httpCtx;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider authProvider)
        {
            _httpCtx = httpContextAccessor;
            _auth = authProvider;
        }


        public async ValueTask<ObjectId?> GetUserIdAsync()
        {
            var user = _httpCtx.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
                return user.GetUserId();

            var state = await _auth.GetAuthenticationStateAsync();
            return state.User.GetUserId();
        }
    }
}

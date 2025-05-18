using Microsoft.AspNetCore.Components.Authorization;
using MongoDB.Bson;
using Noting.Helpers;


namespace Noting.Services
{
    public interface ICurrentUserService
    {
        ValueTask<ObjectId?> GetUserIdAsync();
    }
    public class MvcCurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpCtx;
        public MvcCurrentUserService(IHttpContextAccessor httpContextAccessor)
            => _httpCtx = httpContextAccessor;

        public ValueTask<ObjectId?> GetUserIdAsync()
        {
            var user = _httpCtx.HttpContext?.User;
            return new ValueTask<ObjectId?>(user?.GetUserId());
        }
    }

    public class BlazorCurrentUserService : ICurrentUserService
    {
        private readonly AuthenticationStateProvider _auth;
        public BlazorCurrentUserService(AuthenticationStateProvider authProvider)
            => _auth = authProvider;

        public async ValueTask<ObjectId?> GetUserIdAsync()
        {
            var state = await _auth.GetAuthenticationStateAsync();
            return state.User.GetUserId();
        }
    }

}


using System.Security.Claims;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IAuthService
{
    Task<User?> GetUserAsync(ClaimsPrincipal user);
}
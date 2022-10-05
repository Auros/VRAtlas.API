namespace VRAtlas.Services;

public interface IUserPermissionService
{
    Task<IEnumerable<string>> GetUserPermissions(Guid userId);
    Task Clear(Guid userId);
    Task ClearAll();
}
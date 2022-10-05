namespace VRAtlas;

public static class AtlasConstants
{
    public const string IdentifierClaimType = "vratlas:identifier";

    public const string AdministratorRoleName = "Administrator";

    public const string DefaultRoleName = "User";

    public const int MainDatabase = 0;

    /// <summary>
    /// The Id for the Redis database which stores cached user permission data
    /// </summary>
    public const int PermissionDatabase = 1;

    #region Permissions

    /// <summary>
    /// A special permission which bypasses all permission checks.
    /// </summary>
    public const string SpecialAdministrator = "special.administrator";

    /// <summary>
    /// Allows a user to create an event.
    /// </summary>
    public const string UserEventCreate = "user.event.create";

    /// <summary>
    /// Allows a user to edit an event they manage.
    /// </summary>
    public const string UserEventEdit = "user.event.edit";

    /// <summary>
    /// Allows a user to delete an event they manage.
    /// </summary>
    public const string UserEventDelete = "user.event.delete";

    /// <summary>
    /// Allows a user to edit any event.
    /// </summary>
    public const string ModerationEventEdit = "moderation.event.edit";

    /// <summary>
    /// Allows a user to delete any event.
    /// </summary>
    public const string ModerationEventDelete = "moderation.event.delete";

    #endregion
}
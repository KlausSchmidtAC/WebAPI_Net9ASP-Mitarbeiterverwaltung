namespace Domain.Constants;

/// <summary>
/// Zentrale Konstanten für Identity und Authentifizierung
/// </summary>
public static class IdentityData
{
    /// <summary>
    /// JWT Claims für Token
    /// </summary>
    public static class Claims
    {
        public const string UserId = "userId";
        public const string Email = "email";
        public const string Role = "role";
        public const string AdminRole = "admin";
        public const string UserRole = "user";
    }

    /// <summary>
    /// Authorization Policies
    /// </summary>
    public static class Policies
    {
        public const string AdminOnly = "AdminPolicy";
        public const string UserOnly = "UserPolicy";
        public const string AuthenticatedUser = "AuthenticatedUserPolicy";
    }
    
    
    /// <summary>
    /// Benutzer-Rollen
    /// </summary>
    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string User = "User";
        public const string Guest = "Guest";
    }
}
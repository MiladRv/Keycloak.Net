namespace Keycloak.Net.Sdk.Users.Contracts;

/// <summary>Common required action IDs. Custom action IDs can also be passed to ExecuteActionsEmailAsync.</summary>
public static class UserRequiredActions
{
    public const string VerifyEmail = "VERIFY_EMAIL";
    public const string UpdatePassword = "UPDATE_PASSWORD";
    public const string UpdateProfile = "UPDATE_PROFILE";
    public const string ConfigureTotp = "CONFIGURE_TOTP";
    public const string TermsAndConditions = "TERMS_AND_CONDITIONS";
}

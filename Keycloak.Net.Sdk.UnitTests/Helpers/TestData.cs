namespace Keycloak.Net.Sdk.UnitTests.Helpers;

public static class TestData
{
    public const string RealmName    = "test-realm";
    public const string ClientId     = "test-client";
    public const string ClientSecret = "test-secret";
    public const string ClientUuid   = "client-uuid-123";
    public const string UserId       = "user-id-abc123";
    public const string Username     = "testuser";
    public const string Password     = "password123";
    public const string RoleId       = "role-id-xyz";
    public const string RoleName     = "test-role";
    public const string AccessToken  = "eyJhbGciOiJSUzI1NiJ9.test.token";
    public const string RefreshToken = "eyJhbGciOiJIUzUxMiJ9.refresh.token";

    public static string SigninResponse => $$"""
        {
            "access_token": "{{AccessToken}}",
            "expires_in": 300,
            "refresh_expires_in": 1800,
            "refresh_token": "{{RefreshToken}}",
            "token_type": "Bearer",
            "not-before-policy": 0,
            "session_state": "sess-123",
            "scope": "profile email"
        }
        """;

    public static string UserInfoResponse => $$"""
        {
            "id": "{{UserId}}",
            "username": "{{Username}}",
            "emailVerified": false,
            "createdTimestamp": 1700000000000,
            "enabled": true,
            "totp": false,
            "notBefore": 0
        }
        """;

    public static string UserListResponse => $"[{UserInfoResponse}]";

    public static string ClientRolesResponse => $$"""
        [
            {
                "id": "{{RoleId}}",
                "name": "{{RoleName}}",
                "description": "Test role",
                "composite": false,
                "clientRole": true,
                "containerId": "{{ClientUuid}}"
            }
        ]
        """;

    public static string ClientScopesResponse => """
        [
            {
                "id": "scope-id-1",
                "name": "profile",
                "description": "OpenID Connect built-in scope: profile",
                "protocol": "openid-connect"
            }
        ]
        """;

    public static string ClientsResponse => """
        [
            {
                "id": "client-abc",
                "clientId": "test-client",
                "name": "Test Client",
                "enabled": true,
                "publicClient": false,
                "serviceAccountsEnabled": true
            }
        ]
        """;
}

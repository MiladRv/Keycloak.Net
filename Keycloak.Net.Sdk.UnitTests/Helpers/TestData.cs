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

    public static string UserWithAttributesResponse => $$"""
        {
            "id": "{{UserId}}",
            "username": "{{Username}}",
            "emailVerified": false,
            "createdTimestamp": 1700000000000,
            "enabled": true,
            "totp": false,
            "notBefore": 0,
            "attributes": {
                "department": ["engineering"]
            }
        }
        """;

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

    public const string ClientScopeId = "scope-id-1";

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

    public static string ClientScopeResponse => $$"""
        {
            "id": "{{ClientScopeId}}",
            "name": "profile",
            "description": "OpenID Connect built-in scope: profile",
            "protocol": "openid-connect"
        }
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

    public const string GroupId   = "group-id-abc";
    public const string GroupName = "test-group";
    public const string GroupPath = "/test-group";

    public static string GroupResponse => $$"""
        {
            "id": "{{GroupId}}",
            "name": "{{GroupName}}",
            "path": "{{GroupPath}}",
            "subGroups": []
        }
        """;

    public static string GroupsResponse => $"[{GroupResponse}]";

    public const string RealmRoleId   = "realm-role-id-xyz";
    public const string RealmRoleName = "test-realm-role";

    public static string RealmRoleResponse => $$"""
        {
            "id": "{{RealmRoleId}}",
            "name": "{{RealmRoleName}}",
            "description": "Test realm role",
            "composite": false,
            "clientRole": false,
            "containerId": "{{RealmName}}"
        }
        """;

    public static string RealmRolesResponse => $"[{RealmRoleResponse}]";

    public const string SessionId = "session-id-xyz";

    public static string UserSessionResponse => $$"""
        {
            "id": "{{SessionId}}",
            "username": "{{Username}}",
            "userId": "{{UserId}}",
            "ipAddress": "127.0.0.1",
            "start": 1700000000000,
            "lastAccess": 1700000001000,
            "rememberMe": false,
            "clients": {
                "{{ClientUuid}}": "{{ClientId}}"
            }
        }
        """;

    public static string UserSessionsResponse => $"[{UserSessionResponse}]";

    public const string ProtocolMapperId   = "mapper-id-1";
    public const string ProtocolMapperName = "test-mapper";

    public static string ProtocolMapperResponse => $$"""
        {
            "id": "{{ProtocolMapperId}}",
            "name": "{{ProtocolMapperName}}",
            "protocol": "openid-connect",
            "protocolMapper": "oidc-usermodel-attribute-mapper",
            "consentRequired": false,
            "config": {
                "user.attribute": "department",
                "claim.name": "department"
            }
        }
        """;

    public static string ProtocolMappersResponse => $"[{ProtocolMapperResponse}]";

    public const string RealmId          = "realm-id-xyz";
    public const string RealmDisplayName = "Test Realm";

    public static string RealmResponse => $$"""
        {
            "id": "{{RealmId}}",
            "realm": "{{RealmName}}",
            "displayName": "{{RealmDisplayName}}",
            "enabled": true,
            "sslRequired": "external",
            "registrationAllowed": false,
            "loginWithEmailAllowed": true,
            "resetPasswordAllowed": false,
            "editUsernameAllowed": false,
            "verifyEmail": false,
            "rememberMe": false,
            "bruteForceProtected": false,
            "accessTokenLifespan": 300
        }
        """;

    public static string RealmsResponse => $"[{RealmResponse}]";

    public const string CredentialId = "credential-id-abc";

    public static string CredentialResponse => $$"""
        {
            "id": "{{CredentialId}}",
            "type": "password",
            "userLabel": "My password",
            "createdDate": 1700000000000
        }
        """;

    public static string CredentialsResponse => $"[{CredentialResponse}]";
}

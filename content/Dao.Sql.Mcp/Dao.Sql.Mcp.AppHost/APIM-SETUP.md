# Azure API Management Setup for DAB MCP Server

This guide explains how to configure Azure API Management (APIM) in front of your Data API Builder (DAB) MCP server with EntraID authentication and role-based access control.

## Architecture

```
Client App → APIM (Token Validation + Role Extraction) → DAB MCP Server (Role-based Permissions)
```

## Prerequisites

1. Azure API Management instance
2. Azure AD App Registration (Client App)
3. Azure AD App Registration (Resource/API)
4. DAB MCP Server endpoint URL (from Aspire deployment)
5. Tenant ID

## Step 1: Configure Azure AD App Registrations

### Resource App (DAB Backend) - Already Configured
- **Application ID**
- **Exposed API Scopes**: `MCP.Access`
- **App Roles**: Define roles that match your `dab-config.json` permissions:
  - `Developers` (for full access)
  - `authenticated` (for read-only access)

### Client App - Already Configured
- **Application ID**
- **API Permissions**: Granted `MCP.Access` scope

## Step 2: Create API in APIM

1. Navigate to your APIM instance in Azure Portal
2. Go to **APIs** → **Add API** → **Blank API**
3. Configure:
   - **Display name**: `DAB MCP Server`
   - **Name**: `dab-mcp-server`
   - **Web service URL**: `https://your-dab-server-url` (from Aspire deployment)
   - **API URL suffix**: `mcp` (or leave blank if you want APIM at root)

## Step 3: Configure Operations

DAB MCP exposes an SSE (Server-Sent Events) endpoint for MCP protocol:

1. **Add Operation**:
   - **Display name**: `MCP Protocol`
   - **URL**: `POST /mcp` (or adjust to match your DAB path)
   - **Description**: MCP protocol endpoint for tool execution

2. You may also want to expose Swagger UI:
   - **Display name**: `Swagger UI`
   - **URL**: `GET /swagger`

## Step 4: Apply the Policy

1. In your API, go to **All operations** (or specific operation)
2. In **Inbound processing**, click **</>** to edit policy code
3. Copy the contents of `apim-policy.xml` and paste it
4. Save the policy

### Key Policy Features

The policy (`apim-policy.xml`) performs:

1. **Token Validation**: Validates Azure AD JWT tokens
2. **Scope Verification**: Ensures `MCP.Access` scope is present
3. **Role Extraction**: Extracts roles from both `roles` and `groups` claims
4. **Header Forwarding**: Sets `X-MS-API-ROLE` header for DAB
5. **User Identity**: Optionally forwards `X-MS-CLIENT-PRINCIPAL-ID`

## Step 5: Role Mapping

### Option A: Using App Roles (Recommended)

In your Azure AD App Registration, define App Roles:

```json
{
  "allowedMemberTypes": ["User"],
  "description": "Developers have full access",
  "displayName": "Developer",
  "id": "guid-here",
  "isEnabled": true,
  "value": "Developers"
}
```

Users assigned this role will have the `roles` claim in their token with value `Developers`.

### Option B: Using Groups

Users who are members of Security Groups will have their group Object IDs in the `groups` claim. You can:

1. Use Group Object IDs directly as role names in `dab-config.json`
2. Or configure optional claims in App Registration to get group display names

## Step 6: Update DAB Configuration

Your `dab-config.json` is already configured correctly with:

```json
{
  "runtime": {
    "host": {
      "authentication": {
        "provider": "EntraID",
        "jwt": {
          "audience": "{{server guid}}",
          "issuer": "https://login.microsoftonline.com/{{tenant id here}}/v2.0"
        }
      }
    }
  },
  "entities": {
    "User": {
      "permissions": [
        {
          "role": "Developers",
          "actions": ["*"]
        }
      ]
    }
  }
}
```

## Step 7: Configure CORS (if needed)

If your client is a web application, you may need to configure CORS in APIM:

1. In APIM Policy, add to `<inbound>`:

```xml
<cors allow-credentials="false">
    <allowed-origins>
        <origin>https://your-client-app.com</origin>
    </allowed-origins>
    <allowed-methods>
        <method>GET</method>
        <method>POST</method>
        <method>OPTIONS</method>
    </allowed-methods>
    <allowed-headers>
        <header>*</header>
    </allowed-headers>
</cors>
```

## Step 8: Test the Setup


## Consuming from Your MCP Client

Update your MCP client configuration to use the APIM URL instead of direct DAB URL:

```csharp
// In McpClientService.cs or appsettings.json
var mcpServerUrl = "https://your-apim.azure-api.net/mcp";
```

The client will:
1. Acquire token from Azure AD with correct audience
2. Send requests to APIM with `Authorization: Bearer {token}`
3. APIM validates token and forwards with role headers
4. DAB enforces permissions based on roles

## Troubleshooting

### 401 Unauthorized
- Verify token audience matches
- Check token has `MCP.Access` scope
- Verify tenant ID in policy matches your Azure AD

### 403 Forbidden (from DAB)
- Check that user has appropriate role (`Developers` or mapped group)
- Verify role claim is in token (decode at jwt.ms)
- Check entity permissions in `dab-config.json`

### No roles extracted
- Verify App Roles are assigned to users in Azure AD
- Check optional claims configuration if using groups
- Review APIM trace logs to see role extraction

## Security Best Practices

1. **Least Privilege**: Only grant necessary roles to users
2. **Token Expiration**: Set appropriate token lifetime
3. **Rate Limiting**: Add rate limiting policy in APIM
4. **Logging**: Enable APIM diagnostic logging
5. **Production Mode**: Set DAB `mode` to `production` in `dab-config.json`

## Monitoring

Enable Application Insights for both APIM and DAB:
- Track failed authentication attempts
- Monitor role usage patterns
- Alert on suspicious activity

## References

- [DAB EntraID Authentication](https://learn.microsoft.com/en-us/azure/data-api-builder/concept/security/how-to-authenticate-entra)
- [APIM Authentication Policies](https://learn.microsoft.com/en-us/azure/api-management/api-management-authentication-policies)
- [Azure AD App Roles](https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps)

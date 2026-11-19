# Yaara Frames & Gifts Admin

The Admin app hosts the protected catalog and administrator APIs and serves the management UI. It shares the main application's EF Core model, migrations, and SQL Server database; it does not create another database or connect to SQL Server from the browser.

## Configuration

- `ConnectionStrings__DefaultConnection` must be the same SQL Server connection string used by `YaaraFramesGifts`. In Development, the Admin app also loads the main app's adjacent `appsettings.json` files.
- `AdminApi__BaseUrl` is the Admin app's own root URL. The Development profile uses HTTP locally; use HTTPS in every non-Development environment.
- `AdminAuth__KeyRingPath` must be a persistent directory accessible to both apps in non-Development environments. Both apps use the same Data Protection application name and key ring so the main app can validate bearer tokens issued by the Admin API.
- Configure bootstrap administrators through environment variables or a secret store, never checked-in settings files. For each indexed user, provide:
  - `AdminBootstrap__Users__0__Username`
  - `AdminBootstrap__Users__0__Password`
  - `AdminBootstrap__Users__0__FullName`
  - `AdminBootstrap__Users__0__Email`

Additional administrators can use indices `1`, `2`, and so on. Bootstrap accounts are created only when their normalized username does not already exist. Passwords must be at least 12 characters and contain lowercase, uppercase, numeric, and non-alphanumeric characters. Passwords are stored as hashes.

## IIS deployment

The main and Admin apps both fail startup outside Development if `AdminAuth__KeyRingPath` is missing. Create a persistent directory outside either publish folder, such as `C:\ProgramData\YaaraFramesGifts\KeyRing`, and grant Modify permission on it to both IIS application-pool identities. For example, replace the pool names below with the names configured in IIS:

```powershell
New-Item -ItemType Directory -Force C:\ProgramData\YaaraFramesGifts\KeyRing
icacls C:\ProgramData\YaaraFramesGifts\KeyRing /grant `
  "IIS AppPool\YaaraFramesGifts:(OI)(CI)M" `
  "IIS AppPool\YaaraFramesGiftsAdmin:(OI)(CI)M"
```

Set `AdminAuth__KeyRingPath` to that exact same directory for both sites. One option is to add it to each published site's `web.config`, inside the existing `<aspNetCore>` element:

```xml
<environmentVariables>
  <environmentVariable name="AdminAuth__KeyRingPath"
                       value="C:\ProgramData\YaaraFramesGifts\KeyRing" />
</environmentVariables>
```

Keep the directory outside the publish folders so deployments do not replace the keys. The main and Admin apps must use the same key directory; separate keys prevent the main app from validating Admin API tokens.

## Database migration

The `AddAdminUsers` migration adds the administrator account table, and `PersistCategoryImages` adds an optional image URL to Categories. They apply to the existing database; neither creates another database. From the `YaaraFramesGifts` project directory, apply pending migrations with:

```powershell
dotnet ef database update `
  --project ..\YaaraFramesGifts.Shared\YaaraFramesGifts.Shared.csproj `
  --startup-project .\YaaraFramesGifts.csproj
```

Development startup applies pending migrations automatically. Apply migrations deliberately before production deployment; production startup does not migrate the database.

## Catalog administration

The Admin UI manages Categories and Products through `/api/admin/categories` and `/api/admin/products`. These Admin-hosted APIs reuse the main application's catalog services and database. They provide paginated, searchable, sortable lists and use the existing duplicate, foreign-key, and dependent-record deletion checks. Categories and Products do not have persisted active/inactive fields, so no status toggle is offered.

Administrators can upload JPG, JPEG, PNG, and WEBP catalog images up to 5 MB through the protected `/api/admin/images` endpoint. Images are stored under the Admin app's `wwwroot/uploads/catalog` directory and served as static files. Ensure the Admin app identity can write to this directory and that deployments preserve it; otherwise uploaded image URLs will stop working after a deployment.

Admin login posts username and password to `/api/admin/auth/login`. The API validates credentials against `AdminUsers`, rejects inactive accounts, and returns a short-lived protected bearer token. Admin UI pages use a local secure cookie, while both Admin APIs and the main app's protected catalog writes enforce the administrator policy server-side.

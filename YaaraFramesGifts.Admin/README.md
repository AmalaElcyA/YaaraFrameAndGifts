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

## Database migration

The `AddAdminUsers` migration adds the administrator account table to the existing database; it does not create another database or modify catalog tables. From the `YaaraFramesGifts` project directory, apply it with:

```powershell
dotnet ef database update `
  --project ..\YaaraFramesGifts.Shared\YaaraFramesGifts.Shared.csproj `
  --startup-project .\YaaraFramesGifts.csproj
```

Development startup applies pending migrations automatically. Apply migrations deliberately before production deployment; production startup does not migrate the database.

## Catalog administration

The Admin UI manages Categories and Products through `/api/admin/categories` and `/api/admin/products`. These Admin-hosted APIs reuse the main application's catalog services and database. They provide paginated, searchable, sortable lists and use the existing duplicate, foreign-key, and dependent-record deletion checks. Categories and Products do not have persisted active/inactive fields, so no status toggle is offered.

Admin login posts username and password to `/api/admin/auth/login`. The API validates credentials against `AdminUsers`, rejects inactive accounts, and returns a short-lived protected bearer token. Admin UI pages use a local secure cookie, while both Admin APIs and the main app's protected catalog writes enforce the administrator policy server-side.

# Aspire Entity Framework Example

This is an example of how to use Entity Framework Core in an Aspire project with Migrations setup.

## Adding a new migration

```bash
cd Aspire.Ef/Aspire.Ef.MigrationService
dotnet ef migrations add <MigrationName> --startup-project . --project .
```
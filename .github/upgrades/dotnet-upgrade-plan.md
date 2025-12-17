# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade CadExportX\CadExportX.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

No projects are excluded from the upgrade.

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### CadExportX\CadExportX.csproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style format
  - Target framework should be changed from `net48` to `net8.0-windows`

NuGet packages changes:
  - No NuGet package changes detected during analysis

Feature upgrades:
  - Convert project from old .csproj format to modern SDK-style format
  - Update target framework from .NET Framework 4.8 to .NET 8.0

Other changes:
  - Review and update any .NET Framework-specific APIs that may need to be replaced with .NET 8.0 equivalents
  - Verify compatibility of all dependencies with .NET 8.0

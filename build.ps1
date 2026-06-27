# Convenience wrapper. Deploy is automatic: the DiscoAccess project copies the plugin into
# BepInEx/plugins and prism.dll next to disco.exe on every Debug build (see DiscoAccess.csproj).
# Close the game first, or the dll copy is skipped (file locked) and you'll run a stale build.
$ErrorActionPreference = "Stop"
dotnet build "$PSScriptRoot\DiscoAccess.slnx" -c Debug

# BepInEx (vendored loader)

Mod loader for the game. Source: https://github.com/BepInEx/BepInEx

**Version: 6.0.0-pre.2**, build `BepInEx-Unity.IL2CPP-win-x64` (Disco Elysium is Unity 2020.3.12f1, IL2CPP, x64). Bundles Il2CppInterop and HarmonyX.

## Install (into the game folder)
Extract `BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip` over
`C:\Program Files (x86)\Steam\steamapps\common\Disco Elysium\` (writable without admin on this machine).
This drops the `winhttp.dll` doorstop plus `BepInEx/` and `dotnet/`. Fully reversible (delete those).

## First launch
Launch the game **through Steam** (running `disco.exe` directly exits on the DRM check). The first
run is slow: BepInEx runs Il2CppInterop to generate managed proxy assemblies under
`BepInEx/interop/` - those are our actual compile targets (Il2Cpp-prefixed types), not the read-only
Cpp2IL dummies in `/decompiled`. It also writes `BepInEx/config/` and `BepInEx/LogOutput.log`.

## Notes
The mod assemblies run on **.NET 6** (the bundled `dotnet/` CoreCLR). Target `net6.0`.

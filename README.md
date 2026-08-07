# FamilyOwned

FamilyOwned is a small MelonLoader compatibility mod for **BOXROOM** that launches Steam-backed custom games through `steam://` URLs.

It is designed to work with family-shared games imported by **BR-QImport**.

## Why It Is Needed

BR-QImport imports Steam Family titles as BOXROOM custom games. These games use stable negative BOXROOM App IDs beginning at `-500000`, while retaining their original Steam App IDs in the launch path:

```text
steam://run//<STEAM-APP-ID>
```

BOXROOM normally treats a custom game's `LaunchExePath` as a local executable and checks it with `File.Exists`. A Steam URL is not a local file, so BOXROOM refuses to launch it.

FamilyOwned intercepts that specific launch request and passes the URL to Unity's normal URL launcher instead.

## Features

- Launches BR-QImport family games through Steam
- Supports `steam://run//<APP-ID>` custom-game paths
- Only affects custom games with negative App IDs
- Leaves ordinary Steam games and local custom games unchanged
- Requires no JSON configuration
- Does not override Steam ownership or subscription checks
- Compatible with Windows and Linux through Proton

## Requirements

- BOXROOM
- MelonLoader
- BR-QImport for importing Steam Family games
- Access to the shared game through Steam Families
- The Steam client must be installed and able to handle `steam://` URLs

## Installation

1. Install MelonLoader for BOXROOM.
2. Copy `FamilyOwned.dll` into BOXROOM's `Mods` directory.
3. Use BR-QImport to import your library with **Include Family Shared** enabled.
4. Restart BOXROOM.

Typical Windows installation path:

```text
<BOXROOM installation>/Mods/FamilyOwned.dll
```

When running BOXROOM through Proton on Linux, install the DLL in the same `Mods` directory beside the Proton game installation.

## BR-QImport Metadata

BR-QImport generates family-game metadata resembling:

```json
{
  "AppType": "custom",
  "Name": "Example Family Game",
  "LaunchExePath": "steam://run//123456",
  "LaunchArguments": "",
  "AppId": -500000
}
```

The two IDs serve different purposes:

- `AppId` is BOXROOM's custom negative ID.
- The ID inside `LaunchExePath` is the game's real Steam App ID.

BR-QImport keeps these negative IDs stable between imports whenever possible.

## How It Works

The mod patches:

```csharp
SteamLibrarySystem.LaunchGame(SteamGameData data)
```

When all of the following are true:

- The game has a negative custom App ID
- `LaunchExePath` begins with `steam://run//`
- The launch path is not empty

FamilyOwned opens the Steam URL and skips BOXROOM's local-file launch logic.

Every other launch continues through BOXROOM's original method unchanged.

## No Configuration File

Version 2.0 no longer uses:

```text
familyowned.json
```

It also no longer patches:

```csharp
SteamGameData.IsOwned
SteamApps.IsSubscribedToApp(...)
```

Family titles are represented as ordinary BOXROOM custom games instead.

## Example Log

Successful startup:

```text
[FamilyOwned] Initialized custom Steam URI launcher.
```

## Linux and Proton

FamilyOwned is compatible with BOXROOM running through Proton. The Steam client and Proton environment must be able to open the generated `steam://run//<APP-ID>` URL.

If a game does not launch on Linux, first verify that its Steam URL opens outside BOXROOM and that the game is available to the current Steam Family member.

## Limitations

FamilyOwned does not:

- Grant ownership of Steam games
- Bypass Steam DRM
- Enable Steam Families
- Download or install games
- Override Steam account permissions
- Make unavailable family games playable

Steam must already allow the signed-in account to launch the selected game.

## Building

Reference the BOXROOM and MelonLoader assemblies configured in `FamilyOwned.csproj`, then build the project as an x64 class library:

```powershell
dotnet build FamilyOwned.csproj -c Release -p:SkipGameDeploy=true
```

The resulting DLL is written to:

```text
bin/Release/netstandard2.1/FamilyOwned.dll
```

Omit `SkipGameDeploy` when the configured BOXROOM `Mods` directory is available and you want the post-build step to install the DLL automatically.

## Updating from Version 1.x

1. Replace the old `FamilyOwned.dll` with version 2.0 or newer.
2. Re-import the family library using the current BR-QImport release.
3. Restart BOXROOM.
4. Delete the obsolete `familyowned.json` if it still exists. Current BR-QImport versions also remove it automatically.

## Uninstall

Delete:

```text
BOXROOM/Mods/FamilyOwned.dll
```

Imported family entries will remain visible as custom games, but their `steam://` launch paths will not work through BOXROOM without this compatibility patch.

## License

MIT License.

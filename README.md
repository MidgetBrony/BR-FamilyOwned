# FamilyOwned

FamilyOwned is a small MelonLoader compatibility mod for **BOXROOM** that allows Steam-backed custom games to launch through `steam://` URIs.

It was originally designed for family-shared games imported by **BR-QImport**, but also supports Steam URI launching for custom games running through BOXROOM on Linux/Proton.

## Why It Is Needed

BR-QImport imports Steam Family titles as BOXROOM custom games. These games use stable negative BOXROOM App IDs beginning at `-500000`, while retaining their original Steam App IDs in the launch path:

```text
steam://run/<STEAM-APP-ID>
```

BOXROOM normally treats a custom game's `LaunchExePath` as a local executable and may validate or launch it as a filesystem path.

A Steam URI is not a local executable, so BOXROOM's normal custom-game launch path cannot handle it correctly.

FamilyOwned intercepts Steam URI launch requests for custom games, passes the URI to Unity's URL launcher, and skips BOXROOM's normal executable-launch logic.

This also provides a useful compatibility layer when BOXROOM is running through Proton: rather than attempting to launch a native Linux executable from inside BOXROOM's Wine/Proton environment, BOXROOM can hand the launch request back to the host Steam client.

## Features

* Launches BR-QImport family games through Steam
* Supports `steam://run/<APP-ID>` for normal Steam App IDs
* Supports `steam://rungameid/<SHORTCUT-ID>` for Steam non-Steam shortcuts
* Supports Steam URI launch paths without requiring BOXROOM to understand the underlying executable
* Useful for BOXROOM running through Proton on Steam Deck/Linux
* Only intercepts Steam URIs assigned to BOXROOM custom games
* Leaves ordinary Steam games and local executable-based custom games unchanged
* Requires no JSON configuration
* Does not override Steam ownership or subscription checks

## Requirements

* BOXROOM
* MelonLoader
* Steam client capable of handling `steam://` URIs
* BR-QImport when importing Steam Family games
* Access to any Steam Family game being launched

## Installation

1. Install MelonLoader for BOXROOM.
2. Copy `FamilyOwned.dll` into BOXROOM's `Mods` directory.
3. If using BR-QImport, import your library with **Include Family Shared** enabled.
4. Restart BOXROOM.

Typical installation path:

```text
<BOXROOM installation>/Mods/FamilyOwned.dll
```

When running BOXROOM through Proton on Linux, install the DLL into the same `Mods` directory inside the BOXROOM installation.

### Linux / Proton MelonLoader Setup

MelonLoader may require a Wine DLL override when BOXROOM is launched through Proton.

Add the following to BOXROOM's Steam launch options:

```text
WINEDLLOVERRIDES="version=n,b" %command%
```

A quick way to verify that MelonLoader is working is to launch BOXROOM and confirm that the MelonLoader console/log appears and that the following FamilyOwned startup message is present:

```text
[FamilyOwned] Initialized custom Steam URI launcher.
```

If FamilyOwned does not appear in the MelonLoader log, Steam URI handling provided by this mod will not be active.

## BR-QImport Metadata

BR-QImport generates family-game metadata resembling:

```json
{
  "AppType": "custom",
  "Name": "Example Family Game",
  "LaunchExePath": "steam://run/123456",
  "LaunchArguments": "",
  "AppId": -500000
}
```

The two IDs serve different purposes:

* `AppId` is BOXROOM's custom negative ID.
* The ID inside `LaunchExePath` is the game's real Steam App ID.

BR-QImport keeps these negative IDs stable between imports whenever possible.

## How It Works

FamilyOwned patches:

```csharp
SteamLibrarySystem.LaunchGame(SteamGameData data)
```

For BOXROOM custom games, FamilyOwned checks whether `LaunchExePath` contains a Steam URI:

```text
steam://
```

When it does, FamilyOwned passes the URI to:

```csharp
Application.OpenURL(...)
```

and skips BOXROOM's original `LaunchGame` implementation.

This prevents BOXROOM from treating the Steam URI as a local executable path.

Conceptually:

```text
BOXROOM custom game
        |
        v
FamilyOwned
        |
        v
steam:// URI
        |
        v
Steam
        |
        v
Game
```

Every launch that does not match these conditions continues through BOXROOM's original method unchanged.

## Steam URI Types

### Normal Steam Games

For a normal Steam App ID, use:

```text
steam://run/<APP-ID>
```

Example:

```text
steam://run/123456
```

### Non-Steam Games Added to Steam

Steam non-Steam shortcuts use a different URI:

```text
steam://rungameid/<SHORTCUT-ID>
```

Example:

```text
steam://rungameid/1534302213798240256
```

This is particularly useful on Steam Deck/Linux.

The URI must be placed in the custom game's **executable / `LaunchExePath` field**, not in `LaunchArguments`.

Correct:

```text
Executable:
steam://rungameid/<SHORTCUT-ID>

Launch Arguments:
(empty)
```

Do not configure it as:

```text
Executable:
/usr/bin/steam

Launch Arguments:
steam://rungameid/<SHORTCUT-ID>
```

FamilyOwned detects Steam URIs from `LaunchExePath`.

## Linux / Proton and Native Linux Games

BOXROOM currently runs through Proton on Linux. This creates an important limitation when launching native Linux games.

BOXROOM itself is a Windows application running inside Wine/Proton. Native Linux executable paths such as:

```text
/usr/bin/steam
```

or:

```text
/home/user/Games/example/game
```

should not be used as BOXROOM custom-game executable paths and cannot be treated like normal Windows executable paths from inside the Proton environment.

Instead, add the native Linux game to the host Steam client as a **non-Steam game**.

Steam will create a shortcut ID for that entry. Configure the corresponding BOXROOM custom game to use:

```text
steam://rungameid/<SHORTCUT-ID>
```

as its executable path.

The resulting launch path is effectively:

```text
BOXROOM
   |
   v
Proton / Wine
   |
   v
MelonLoader
   |
   v
FamilyOwned
   |
   v
steam://rungameid/<SHORTCUT-ID>
   |
   v
Host Steam client
   |
   v
Native Linux game
```

This allows BOXROOM to hand the launch request back to Steam instead of attempting to directly execute a Linux filesystem path from inside Proton.

Steam remains responsible for the actual shortcut configuration, working directory, launch options, compatibility settings, and native game executable.

## Troubleshooting Linux / Proton

If a game does not launch, check the following before changing the BOXROOM configuration:

1. Confirm MelonLoader is actually loading under Proton.

2. Confirm the FamilyOwned startup message appears in `MelonLoader/Latest.log`.

3. Confirm the Steam URI is in the custom game's **executable field**, not `LaunchArguments`.

4. For normal Steam games, use:

   ```text
   steam://run/<APP-ID>
   ```

5. For non-Steam shortcuts, use:

   ```text
   steam://rungameid/<SHORTCUT-ID>
   ```

6. Confirm the corresponding game or non-Steam shortcut launches successfully from Steam itself.

If the Steam shortcut does not work directly from Steam, FamilyOwned cannot make it launch.

## No Configuration File

Version 2.0 and newer no longer use:

```text
familyowned.json
```

FamilyOwned also no longer patches:

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

If this line is missing, verify that MelonLoader itself is loading correctly before troubleshooting Steam URI handling.

## Limitations

FamilyOwned does not:

* Grant ownership of Steam games
* Bypass Steam DRM
* Enable Steam Families
* Download or install games
* Override Steam account permissions
* Make unavailable family games playable
* Configure Steam non-Steam shortcuts
* Make Proton directly execute arbitrary native Linux paths

Steam must already be capable of launching the selected game or shortcut.

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

Users running BOXROOM through Proton should also verify that the required MelonLoader Wine DLL override is configured.

## Uninstall

Delete:

```text
BOXROOM/Mods/FamilyOwned.dll
```

Imported family entries will remain visible as custom games, but Steam URI launch paths will no longer be intercepted by FamilyOwned.

## License

MIT License.

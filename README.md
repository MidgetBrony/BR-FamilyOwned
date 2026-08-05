# FamilyOwned

FamilyOwned is a small MelonLoader mod for **BOXROOM** that allows selected Steam Family Sharing games to pass BOXROOM's ownership and subscription checks.

BOXROOM normally filters games using Steam ownership checks such as:

```csharp
SteamGameData.IsOwned
```

and:

```csharp
SteamApps.IsSubscribedToApp(...)
```

Steam Family Sharing titles may be available to play while still failing these checks because the current Steam account does not directly own the license.

FamilyOwned lets you manually specify those games in a JSON file.

## Features

- Allows selected Family Sharing games to pass `SteamGameData.IsOwned`
- Allows selected Family Sharing games to pass `SteamApps.IsSubscribedToApp`
- Uses normal positive Steam App IDs
- Does not modify BOXROOM save files
- Does not change custom or negative App IDs
- Simple JSON configuration
- Invalid App IDs are ignored and logged

## Requirements

- BOXROOM
- MelonLoader
- Newtonsoft.Json
- Steam must still provide access to the shared game

## Installation

1. Install MelonLoader for BOXROOM.
2. Place `FamilyOwned.dll` inside the `Mods` folder.
3. Launch BOXROOM once.
4. Create `familyowned.json` inside the BOXROOM persistent data folder.

On Windows this is typically:

```text
%USERPROFILE%\AppData\LocalLow\NestedLoop\BOXROOM\familyowned.json
```

The mod uses:

```csharp
Application.persistentDataPath/familyowned.json
```

## Configuration

Create a file named:

```text
familyowned.json
```

Example:

```json
{
  "appIds": [
    620,
    400,
    105600
  ]
}
```

Each value must be a valid positive Steam App ID.

For example:

```
https://store.steampowered.com/app/620/
```

The App ID is:

```
620
```

## How It Works

When BOXROOM checks whether a game is owned, FamilyOwned checks whether the App ID exists inside `familyowned.json`.

If it does, the mod overrides:

```csharp
SteamGameData.IsOwned
```

and returns:

```csharp
true
```

It also overrides:

```csharp
SteamApps.IsSubscribedToApp(appId)
```

and returns:

```csharp
true
```

Every other game continues using BOXROOM's normal ownership checks.

## Example Log

Successful startup:

```text
[FamilyOwned] Initialized with 3 family-owned app ID(s).
```

Missing configuration:

```text
[FamilyOwned] Family ownership file not found: <path>
```

Invalid App ID:

```text
[FamilyOwned] Ignoring invalid Steam app ID: -1
```

## Updating

The configuration is loaded when the mod starts.

If you edit `familyowned.json`, simply restart BOXROOM.

## Notes

FamilyOwned **does not grant ownership** of Steam games.

It only changes how BOXROOM interprets selected App IDs internally. Steam Family Sharing must already allow your account to launch the game.

This mod does **not**:

- Bypass Steam DRM
- Grant game licenses
- Download games
- Enable Family Sharing
- Modify Steam accounts
- Circumvent Steam restrictions

If Steam cannot launch the game, this mod cannot make it launch.

## Why?

BOXROOM currently treats Steam games as either:

- Owned
- Not owned

Steam Family Sharing introduces a third case:

- Available through a shared library

Until BOXROOM officially supports Family Sharing licenses, this mod provides a lightweight compatibility layer by allowing selected App IDs to pass the ownership checks.

## Building

Reference the following assemblies:

```text
0Harmony.dll
MelonLoader.dll
Newtonsoft.Json.dll
Assembly-CSharp.dll
Facepunch.Steamworks.Win64.dll
UnityEngine.CoreModule.dll
```

Build as a Class Library and copy the resulting DLL into:

```text
BOXROOM/Mods/
```

## Uninstall

Delete:

```text
BOXROOM/Mods/FamilyOwned.dll
```

Optionally remove:

```text
familyowned.json
```

No save data is modified.

## License

MIT License.
# FamilyOwned 2.2.0

- Recognize GOG importer launch addresses.
- Locate GOG Galaxy and invoke its direct `runGame` command with the imported product ID.
- Fall back to opening the normal Galaxy game page when Galaxy cannot be located directly.
- Preserve generic absolute URI launching for Steam, CurseForge, and other registered schemes.

Build validation passed. Live GOG Galaxy and Proton launching remains to be tested.

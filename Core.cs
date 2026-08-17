using HarmonyLib;
using MelonLoader;
using SteamShelf;
using UnityEngine;

[assembly: MelonInfo(typeof(FamilyOwned.Core), "FamilyOwned", "2.0.3", "MidgetBrony", null)]
[assembly: MelonGame("NestedLoop", "BOXROOM")]

namespace FamilyOwned
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized custom Steam URI launcher.");
        }
    }

    [HarmonyPatch(typeof(SteamLibrarySystem), nameof(SteamLibrarySystem.LaunchGame))]
    internal static class SteamCustomUriLaunchPatch
    {
        private const string SteamUriPrefix = "steam://";

        private static bool Prefix(SteamGameData data)
        {
            if (data == null ||
                !SteamLibrarySystem.IsCustomAppId(data.AppId) ||
                string.IsNullOrWhiteSpace(data.LaunchExePath) ||
                !data.LaunchExePath.StartsWith(
                    SteamUriPrefix,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Application.OpenURL(data.LaunchExePath);
            return false;
        }
    }
}
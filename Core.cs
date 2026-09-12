using HarmonyLib;
using MelonLoader;
using SteamShelf;
using System;
using System.Diagnostics;

[assembly: MelonInfo(typeof(FamilyOwned.Core), "FamilyOwned", "2.1.0", "MidgetBrony", null)]
[assembly: MelonGame("NestedLoop", "BOXROOM")]

namespace FamilyOwned
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized custom URI launcher.");
        }
    }

    [HarmonyPatch(typeof(SteamLibrarySystem), nameof(SteamLibrarySystem.LaunchGame))]
    internal static class CustomUriLaunchPatch
    {
        private static bool Prefix(SteamGameData data)
        {
            if (data == null ||
                !SteamLibrarySystem.IsCustomAppId(data.AppId) ||
                string.IsNullOrWhiteSpace(data.LaunchExePath) ||
                !Uri.TryCreate(data.LaunchExePath, UriKind.Absolute, out Uri launchUri) ||
                launchUri.IsFile)
            {
                return true;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = data.LaunchExePath,
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                MelonLogger.Error(
                    $"Could not launch URI '{data.LaunchExePath}': {exception.Message}");
            }

            return false;
        }
    }
}

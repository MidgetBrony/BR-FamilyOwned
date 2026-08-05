using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Newtonsoft.Json;
using SteamShelf;
using Steamworks;

[assembly: MelonInfo(typeof(FamilyOwned.Core), "FamilyOwned", "1.0.0", "MidgetBrony", null)]
[assembly: MelonGame("NestedLoop", "BOXROOM")]

namespace FamilyOwned
{
    public sealed class FamilyOwnedConfig
    {
        [JsonProperty("appIds")]
        public List<int> AppIds { get; set; } = new List<int>();
    }

    public class Core : MelonMod
    {
        private static readonly HashSet<int> FamilyOwnedAppIds = new HashSet<int>();
        private static string ConfigPath;

        public override void OnInitializeMelon()
        {
            ConfigPath = Path.Combine(UnityEngine.Application.persistentDataPath, "familyowned.json");
            LoadConfig();
            LoggerInstance.Msg($"Initialized with {FamilyOwnedAppIds.Count} family-owned app ID(s).");
        }

        private static void LoadConfig()
        {
            FamilyOwnedAppIds.Clear();

            try
            {
                if (!File.Exists(ConfigPath))
                {
                    MelonLogger.Warning($"Family ownership file not found: {ConfigPath}");
                    return;
                }

                var config = JsonConvert.DeserializeObject<FamilyOwnedConfig>(File.ReadAllText(ConfigPath));
                if (config?.AppIds == null)
                {
                    MelonLogger.Warning("familyowned.json does not contain a valid 'appIds' array.");
                    return;
                }

                foreach (int appId in config.AppIds)
                {
                    if (appId > 0)
                    {
                        FamilyOwnedAppIds.Add(appId);
                    }
                    else
                    {
                        MelonLogger.Warning($"Ignoring invalid Steam app ID: {appId}");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Could not load {ConfigPath}: {ex}");
            }
        }

        internal static bool IsFamilyOwned(int appId)
        {
            return FamilyOwnedAppIds.Contains(appId);
        }
    }

    [HarmonyPatch]
    internal static class SteamGameDataIsOwnedPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(SteamGameData), "IsOwned");
        }

        private static bool Prefix(SteamGameData __instance, ref bool __result)
        {
            if (__instance != null && Core.IsFamilyOwned(__instance.AppId))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(SteamApps), nameof(SteamApps.IsSubscribedToApp))]
    internal static class SteamAppsIsSubscribedToAppPatch
    {
        private static bool Prefix(AppId appid, ref bool __result)
        {
            if (appid.Value <= int.MaxValue && Core.IsFamilyOwned((int)appid.Value))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}

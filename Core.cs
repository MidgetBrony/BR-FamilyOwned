using HarmonyLib;
using MelonLoader;
using SteamShelf;
using System;
using System.Diagnostics;
using System.IO;

[assembly: MelonInfo(typeof(FamilyOwned.Core), "FamilyOwned", "2.2.0", "MidgetBrony", null)]
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
                if (TryGetGogProductId(launchUri, out string gogProductId) &&
                    TryLaunchGogGame(gogProductId))
                {
                    return false;
                }

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

        private static bool TryGetGogProductId(Uri launchUri, out string productId)
        {
            productId = string.Empty;
            if (!launchUri.Scheme.Equals("goggalaxy", StringComparison.OrdinalIgnoreCase) ||
                !launchUri.Host.Equals("openGameView", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string candidate = launchUri.AbsolutePath.Trim('/');
            if (!long.TryParse(candidate, out long parsed) || parsed <= 0)
                return false;

            productId = candidate;
            return true;
        }

        private static bool TryLaunchGogGame(string productId)
        {
            string galaxyClient = FindGalaxyClient();
            if (string.IsNullOrWhiteSpace(galaxyClient))
            {
                MelonLogger.Warning(
                    "GOG Galaxy was not found. Opening the game's Galaxy page instead.");
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = galaxyClient,
                    Arguments = $"/command=runGame /gameId={productId}",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(galaxyClient) ?? string.Empty
                });
                return true;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning(
                    $"Could not launch GOG game {productId} directly: {exception.Message}. " +
                    "Opening its Galaxy page instead.");
                return false;
            }
        }

        private static string FindGalaxyClient()
        {
            string[] registryKeys =
            {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\GOG.com\GalaxyClient\paths",
                @"HKEY_CURRENT_USER\SOFTWARE\GOG.com\GalaxyClient\paths"
            };

            foreach (string key in registryKeys)
            {
                string candidate = ReadRegistryValue(key, "client");
                string executable = NormalizeGalaxyPath(candidate);
                if (!string.IsNullOrWhiteSpace(executable))
                    return executable;
            }

            string protocolCommand = ReadRegistryValue(
                @"HKEY_CLASSES_ROOT\goggalaxy\shell\open\command",
                null);
            string protocolExecutable = ExtractExecutable(protocolCommand);
            if (!string.IsNullOrWhiteSpace(protocolExecutable) && File.Exists(protocolExecutable))
                return protocolExecutable;

            string[] programFolders =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            foreach (string programFolder in programFolders)
            {
                string executable = Path.Combine(programFolder, "GOG Galaxy", "GalaxyClient.exe");
                if (File.Exists(executable))
                    return executable;
            }

            return string.Empty;
        }

        private static string ReadRegistryValue(string key, string valueName)
        {
            try
            {
                string valueArgument = string.IsNullOrWhiteSpace(valueName)
                    ? "/ve"
                    : $"/v \"{valueName}\"";
                using Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"query \"{key}\" {valueArgument}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (process == null)
                    return string.Empty;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(2000);
                if (process.ExitCode != 0)
                    return string.Empty;

                foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int typeStart = line.IndexOf("REG_", StringComparison.OrdinalIgnoreCase);
                    if (typeStart < 0)
                        continue;

                    int valueStart = typeStart;
                    while (valueStart < line.Length && !char.IsWhiteSpace(line[valueStart]))
                        valueStart++;
                    while (valueStart < line.Length && char.IsWhiteSpace(line[valueStart]))
                        valueStart++;

                    return valueStart < line.Length ? line.Substring(valueStart).Trim() : string.Empty;
                }
            }
            catch
            {
                // Fall back to the registered protocol or standard install folders.
            }

            return string.Empty;
        }

        private static string NormalizeGalaxyPath(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return string.Empty;

            candidate = Environment.ExpandEnvironmentVariables(candidate.Trim().Trim('"'));
            if (File.Exists(candidate))
                return candidate;

            string executable = Path.Combine(candidate, "GalaxyClient.exe");
            return File.Exists(executable) ? executable : string.Empty;
        }

        private static string ExtractExecutable(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return string.Empty;

            command = Environment.ExpandEnvironmentVariables(command.Trim());
            if (command.StartsWith("\"", StringComparison.Ordinal))
            {
                int closingQuote = command.IndexOf('"', 1);
                return closingQuote > 1 ? command.Substring(1, closingQuote - 1) : string.Empty;
            }

            int argumentStart = command.IndexOf(' ');
            return argumentStart > 0 ? command.Substring(0, argumentStart) : command;
        }
    }
}

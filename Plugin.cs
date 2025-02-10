using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using LethalLevelLoader;
using System.Linq;
using System.IO;
using System.Reflection;
using Atlas;
using System;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Assertions.Must;
using static UnityEngine.Rendering.HighDefinition.CameraSettings;
using BepInEx.Bootstrap;

namespace TemplatePluginName
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        const string PLUGIN_GUID = "dopadream.lethalcompany.atlas", PLUGIN_NAME = "Atlas", PLUGIN_VERSION = "1.0.0";
        internal static new ManualLogSource Logger;
        internal static VolumeProfile canyonProfile, valleyProfile, tundraProfile, amethystProfile;
        internal static AssetBundle hdriSkies;

        void Awake()
        {
            Logger = base.Logger;
            ModConfig.Init(Config);
            new Harmony(PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        }

        internal static void ApplySky()
        {
            Plugin.Logger.LogDebug("Starting ApplySky...");

            // Parse the blacklist only once
            string[] blackList = ModConfig.configMoonBlacklist.Value.Split(',');
            Plugin.Logger.LogDebug($"Blacklist contains: {string.Join(", ", blackList)}");

            // Find all volumes
            var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            Plugin.Logger.LogDebug($"Found {volumes.Length} volumes.");

            if (Chainloader.PluginInfos.ContainsKey("imabatby.lethallevelloader"))
            {
                initLLL(blackList, volumes);
            }
            else
            {
                initVanilla(blackList, volumes);
            }

            Plugin.Logger.LogDebug("Finished ApplySky.");
        }

        private static void initLLL(string[] blackList, Volume[] volumes)
        {
            foreach (var tag in LevelManager.CurrentExtendedLevel.ContentTags)
            {
                string tagName = tag.name;
                Plugin.Logger.LogDebug($"Processing tag: {tagName}");

                // Skip tags in the blacklist
                if (blackList.Contains(LevelManager.CurrentExtendedLevel.NumberlessPlanetName))
                {
                    Plugin.Logger.LogDebug($"Skipping volume change because current planet is in blacklist");
                    continue;
                }

                foreach (var volume in volumes)
                {
                    if (volume?.sharedProfile == null)
                    {
                        Plugin.Logger.LogDebug("Skipping volume because it's null or sharedProfile is null.");
                        continue;
                    }

                    if (volume.sharedProfile.TryGet(out HDRISky sky))
                    {
                        if (sky.hdriSky.value.name != "cedar_bridge_4k")
                        {
                            Logger.LogDebug("Skipping volume because it does not match the vanilla skybox.");
                            continue;
                        }
                    }


                    // Load the asset bundle once
                    if (hdriSkies == null)
                    {
                        try
                        {
                            Plugin.Logger.LogDebug("Loading asset bundle 'hdri_skies'...");
                            var assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "hdri_skies");
                            hdriSkies = AssetBundle.LoadFromFile(assetPath);
                            Plugin.Logger.LogDebug("Successfully loaded asset bundle.");
                        }
                        catch (Exception ex)
                        {
                            Plugin.Logger.LogError($"Failed to load asset bundle 'hdri_skies': {ex.Message}");
                            return;
                        }
                    }

                    // Map content tags to asset names and load profiles if not already loaded
                    // Amethyst is here twice to account for LLL currently having a typo
                    VolumeProfile profile = tagName switch
                    {
                        "CanyonContentTag" => ModConfig.configCanyonSky.Value ? LoadProfile(ref canyonProfile, "CanyonSky") : null,
                        "ValleyContentTag" => ModConfig.configValleySky.Value ? LoadProfile(ref valleyProfile, "ValleySky") : null,
                        "TundraContentTag" => ModConfig.configTundraSky.Value ? LoadProfile(ref tundraProfile, "TundraSky") : null,
                        "AmethystContentTag" => ModConfig.configAmethystSky.Value ? LoadProfile(ref amethystProfile, "AmethystSky") : null,
                        "AmythestContentTag" => ModConfig.configAmethystSky.Value ? LoadProfile(ref amethystProfile, "AmethystSky") : null,
                        _ => null
                    };

                    if (volume.name.StartsWith("Sky and Fog"))
                    {

                        if (profile != null)
                        {
                            Plugin.Logger.LogDebug($"Applying profile to volume: {volume}");
                            volume.sharedProfile = profile;
                        }

                        if (LevelManager.CurrentExtendedLevel.NumberlessPlanetName == "Embrion")
                        {
                            Plugin.Logger.LogDebug($"Applying profile to volume: {volume}");
                            volume.sharedProfile = LoadProfile(ref amethystProfile, "AmethystSky");
                        }
                    }
                }
            }
        }

        private static void initVanilla(string[] blackList, Volume[] volumes)
        {
            Plugin.Logger.LogDebug($"LLL missing! Falling back to vanilla moon support...");

            string planetName = StartOfRound.Instance.currentLevel.sceneName;

            // Skip tags in the blacklist
            if (blackList.Contains(GetNumberlessPlanetName(StartOfRound.Instance.currentLevel)))
            {
                Plugin.Logger.LogDebug($"Skipping volume change because current planet is in blacklist");
                return;
            }

            foreach (var volume in volumes)
            {
                if (volume?.sharedProfile == null)
                {
                    Plugin.Logger.LogDebug("Skipping volume because it's null or sharedProfile is null.");
                    continue;
                }


                if (volume.sharedProfile.TryGet(out HDRISky sky))
                {
                    if (sky.hdriSky.value.name != "cedar_bridge_4k")
                    {
                        Logger.LogDebug("Skipping volume because it does not match the vanilla skybox.");
                        continue;
                    }
                }

                // Load the asset bundle once
                if (hdriSkies == null)
                {
                    try
                    {
                        Plugin.Logger.LogDebug("Loading asset bundle 'hdri_skies'...");
                        var assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "hdri_skies");
                        hdriSkies = AssetBundle.LoadFromFile(assetPath);
                        Plugin.Logger.LogDebug("Successfully loaded asset bundle.");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"Failed to load asset bundle 'hdri_skies': {ex.Message}");
                        return;
                    }
                }

                // Map content tags to asset names and load profiles if not already loaded
                VolumeProfile profile = planetName switch
                {
                    "Level7Offense" => ModConfig.configCanyonSky.Value ? LoadProfile(ref canyonProfile, "CanyonSky") : null,
                    "Level2Assurance" => ModConfig.configCanyonSky.Value ? LoadProfile(ref canyonProfile, "CanyonSky") : null,
                    "Level3Vow" => ModConfig.configValleySky.Value ? LoadProfile(ref valleyProfile, "ValleySky") : null,
                    "Level4March" => ModConfig.configValleySky.Value ? LoadProfile(ref valleyProfile, "ValleySky") : null,
                    "Level10Adamance" => ModConfig.configValleySky.Value ? LoadProfile(ref valleyProfile, "ValleySky") : null,
                    "Level5Rend" => ModConfig.configTundraSky.Value ? LoadProfile(ref tundraProfile, "TundraSky") : null,
                    "Level6Dine" => ModConfig.configTundraSky.Value ? LoadProfile(ref tundraProfile, "TundraSky") : null,
                    "Level8Titan" => ModConfig.configTundraSky.Value ? LoadProfile(ref tundraProfile, "TundraSky") : null,
                    "Level11Embrion" => ModConfig.configAmethystSky.Value ? LoadProfile(ref amethystProfile, "AmethystSky") : null,
                    _ => null
                };

                if (volume.name.StartsWith("Sky and Fog"))
                {
                    if (profile != null)
                    {
                        Plugin.Logger.LogDebug($"Applying profile to volume: {volume}");
                        volume.sharedProfile = profile;
                    }
                }
            }
        }

        internal static string GetNumberlessPlanetName(SelectableLevel selectableLevel)
        {
            if (selectableLevel != null)
            {
                return new string(selectableLevel.PlanetName.SkipWhile((char c) => !char.IsLetter(c)).ToArray());
            }

            return string.Empty;
        }

        private static VolumeProfile LoadProfile(ref VolumeProfile profile, string assetName)
        {
            if (profile == null)
            {
                try
                {
                    Plugin.Logger.LogDebug($"Loading profile '{assetName}' from asset bundle...");
                    profile = hdriSkies.LoadAsset<VolumeProfile>(assetName);
                    Plugin.Logger.LogDebug($"Successfully loaded profile '{assetName}'.");
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Failed to load asset '{assetName}' from bundle 'hdri_skies': {ex.Message}");
                }
            }
            /*else
            {
                Plugin.Logger.LogDebug($"Profile '{assetName}' already loaded.");
            }*/

            if (profile.name.StartsWith("Amethyst") && !ModConfig.configAmethystFog.Value)
            {
                if (profile.TryGet(out Fog fog))
                {
                    fog.active = false;
                }
            }

            return profile;
        }

        [HarmonyPatch]
        internal class AtlasPatches
        {
            [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
            [HarmonyPostfix]
            static void PostFinishGeneratingNewLevelClientRpc(RoundManager __instance)
            {
                ApplySky();
            }
        }
    }
}
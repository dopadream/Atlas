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

namespace TemplatePluginName
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(LethalLevelLoader.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
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
            Plugin.Logger.LogDebug("Finished ApplySky.");
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
            else
            {
                Plugin.Logger.LogDebug($"Profile '{assetName}' already loaded.");
            }

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
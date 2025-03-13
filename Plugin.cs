using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using LethalLevelLoader;
using System.Linq;
using System.IO;
using System.Reflection;
using System;
using UnityEngine.Rendering.HighDefinition;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Atlas
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LOBBY_COMPATIBILITY, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string PLUGIN_GUID = "dopadream.lethalcompany.atlas", PLUGIN_NAME = "Atlas", PLUGIN_VERSION = "1.0.7", LOBBY_COMPATIBILITY = "BMX.LobbyCompatibility";
        internal static new ManualLogSource Logger;
        internal static VolumeProfile canyonProfile, valleyProfile, tundraProfile, amethystProfile, companyProfile;
        internal static AssetBundle hdriSkies;

        void Awake()
        {
            Logger = base.Logger;

            if (Chainloader.PluginInfos.ContainsKey(LOBBY_COMPATIBILITY))
            {
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Lobby Compatibility detected");
                LobbyCompatibility.Init();
            }

            ModConfig.Init(Config);
            new Harmony(PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        }

        internal static void ApplySky()
        {
            Plugin.Logger.LogDebug("Starting ApplySky...");

            // Parse the blacklist only once
            string[] blackList = ModConfig.configMoonBlacklist.Value.Split(',');

            for (int i = 0; i < blackList.Length; i++)
            {
                blackList[i] = blackList[i].Trim();
            }

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
            if (blackList == null || volumes == null)
            {
                Plugin.Logger.LogError("Blacklist or volumes array is null.");
                return;
            }

            if (blackList.Contains(LevelManager.CurrentExtendedLevel.NumberlessPlanetName))
            {
                Plugin.Logger.LogDebug("Skipping volume change because current planet is in blacklist");
                return;
            }

            if (volumes.Length == 0)
            {
                Plugin.Logger.LogDebug("Skipping volume change because no volumes could be found");
                return;
            }

            foreach (var volume in volumes)
            {
                if (volume == null)
                {
                    Plugin.Logger.LogDebug("Skipping volume because it's null.");
                    continue;
                }

                if (volume.sharedProfile == null)
                {
                    Plugin.Logger.LogDebug("Skipping volume because sharedProfile is null.");
                    continue;
                }

                if (!volume.sharedProfile.TryGet(out HDRISky sky))
                {
                    Plugin.Logger.LogDebug("Skipping volume change because no HDRISky component could be found");
                    continue;
                }



                if (hdriSkies == null)
                {
                    try
                    {
                        Plugin.Logger.LogDebug("Loading asset bundle 'hdri_skies'...");
                        var assemblyLocation = Assembly.GetExecutingAssembly()?.Location;
                        if (assemblyLocation == null)
                        {
                            Plugin.Logger.LogError("Failed to get assembly location.");
                            return;
                        }

                        var assetPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "hdri_skies");
                        hdriSkies = AssetBundle.LoadFromFile(assetPath);
                        if (hdriSkies == null)
                        {
                            Plugin.Logger.LogError("Failed to load asset bundle: AssetBundle is null.");
                            return;
                        }
                        Plugin.Logger.LogDebug("Successfully loaded asset bundle.");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"Failed to load asset bundle 'hdri_skies': {ex.Message}");
                        return;
                    }
                }

                // probably do override stuff here then return

                ConfigEntry<string>[] overridesList = {
                    ModConfig.configCanyonOverrides,
                    ModConfig.configValleyOverrides,
                    ModConfig.configTundraOverrides,
                    ModConfig.configCompanyOverrides,
                    ModConfig.configAmethystOverrides
                };

                foreach (ConfigEntry<string> entry in overridesList)
                {
                    if (entry.Value == "")
                    {
                        continue;
                    }

                    string[] list = entry.Value.Split(',');

                    for (int i = 0; i < list.Length; i++)
                    {
                        list[i] = list[i].Trim();
                    }

                    if (list.Contains(LevelManager.CurrentExtendedLevel.NumberlessPlanetName))
                    {
                        switch (entry.Definition.Key)
                        {
                            case ("Canyon"):
                                ChangeProfileIfAvailable("Canyon", volume);
                                goto IterateLoop;
                            case ("Valley"):
                                ChangeProfileIfAvailable("Valley", volume);
                                goto IterateLoop;
                            case ("Tundra"):
                                ChangeProfileIfAvailable("Tundra", volume);
                                goto IterateLoop;
                            case ("Company"):
                                ChangeProfileIfAvailable("Company", volume);
                                goto IterateLoop;
                            case ("Amethyst"):
                                ChangeProfileIfAvailable("Amethyst", volume);
                                goto IterateLoop;
                        }
                    }
                }

                if (sky?.hdriSky?.value?.name != "cedar_bridge_4k")
                {
                    continue;
                }

                if (LevelManager.CurrentExtendedLevel?.ContentTags == null)
                {
                    Plugin.Logger.LogError("ContentTags is null.");
                    return;
                }

                foreach (var tag in LevelManager.CurrentExtendedLevel.ContentTags)
                {
                    if (tag?.name == null)
                    {
                        Plugin.Logger.LogDebug("Skipping tag because it's null.");
                        continue;
                    }

                    string tagName = tag.name;
                    Plugin.Logger.LogDebug($"Processing tag: {tagName}");


                    // -- automatic tag mapping -- 

                    switch (tagName)
                    {
                        case ("CanyonContentTag"):
                            if (ModConfig.configCanyonSky?.Value == true)
                                ChangeProfileIfAvailable("Canyon", volume);
                            break;
                        case ("ValleyContentTag"):
                            if (ModConfig.configValleySky?.Value == true)
                                ChangeProfileIfAvailable("Valley", volume);
                            break;
                        case ("TundraContentTag"):
                            if (ModConfig.configTundraSky?.Value == true)
                                ChangeProfileIfAvailable("Tundra", volume);
                            break;
                    }

                    // -- embrion mapping --

                    if (LevelManager.CurrentExtendedLevel.NumberlessPlanetName == "Embrion" || tagName == "AmethystContentTag" || tagName == "AmythestContentTag")
                    {
                        if (ModConfig.configAmethystSky?.Value == false)
                        {
                            return;
                        }

                        ChangeProfileIfAvailable("Amethyst", volume);
                    }

                    // -- company mapping -- 

                    if (LevelManager.CurrentExtendedLevel.NumberlessPlanetName == "Gordion" || tagName == "CompanyContentTag")
                    {
                        if (ModConfig.configCompanySky?.Value == false)
                        {
                            return;
                        }

                        ChangeProfileIfAvailable("Company", volume);
                    }
                }
            IterateLoop:
                continue;
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

            if (volumes == null || volumes.Length == 0)
            {
                Plugin.Logger.LogDebug("Skipping volume change because no volumes could be found");
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
                        //Logger.LogDebug("Skipping volume because it does not match the vanilla skybox.");
                        continue;
                    }
                }
                else
                {
                    Logger.LogDebug("Skipping volume change because no HDRISky component could be found");
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
                    "CompanyBuilding" => ModConfig.configCompanySky.Value ? LoadProfile(ref companyProfile, "CompanySky") : null,
                    "Level11Embrion" => ModConfig.configAmethystSky.Value ? LoadProfile(ref amethystProfile, "AmethystSky") : null,
                    _ => null
                };

                if (volume.name.StartsWith("Sky and Fog"))
                {
                    if (profile != null)
                    {
                        if (volume.sharedProfile.TryGet<HDRISky>(out HDRISky vanillaSky))
                        {
                            if (profile.TryGet<HDRISky>(out HDRISky newSky))
                            {
                                Plugin.Logger.LogDebug($"Applying {newSky.hdriSky.value.name} to sky: {vanillaSky.name}");
                                vanillaSky.hdriSky.value = newSky.hdriSky.value;
                            }
                        }
                    }
                    if (GetNumberlessPlanetName(StartOfRound.Instance.currentLevel) == "Embrion")
                    {
                        if (profile != null)
                        {

                            if (ModConfig.configAmethystSky?.Value == false)
                            {
                                return;
                            }

                            if (volume.name.Equals("Sky and Fog Global Volume"))
                            {
                                Plugin.Logger.LogDebug($"Applying profile to volume: {volume.name}");
                                volume.sharedProfile = LoadProfile(ref amethystProfile, "AmethystSky");
                            }
                            else if (volume.name.Equals("Sky and Fog Global Volume (1)"))
                            {
                                if (volume.sharedProfile.TryGet(out HDRISky vanillaSky) && LoadProfile(ref amethystProfile, "AmethystSky").TryGet(out HDRISky newSky))
                                {
                                    vanillaSky.hdriSky.value = newSky.hdriSky.value;
                                    vanillaSky.distortionMode.value = newSky.distortionMode.value;
                                }
                            }
                        }
                    }

                    if (GetNumberlessPlanetName(StartOfRound.Instance.currentLevel) == "Gordion")
                    {
                        if (ModConfig.configCompanySky?.Value == false)
                        {
                            return;
                        }

                        if (volume.name.Equals("Sky and Fog Global Volume"))
                        {
                            Plugin.Logger.LogDebug($"Applying profile to volume: {volume.name}");
                            volume.sharedProfile = LoadProfile(ref companyProfile, "CompanySky");
                        }
                    }
                }
            }
        }

        internal static void ChangeProfileIfAvailable(string TagName, Volume volume)
        {
            if (volume?.sharedProfile == null || !volume.sharedProfile.TryGet<HDRISky>(out HDRISky vanillaSky))
            {
                return;
            }

            Plugin.Logger.LogDebug($"Applying profile to volume: {volume.name}");

            VolumeProfile profile = TagName switch
            {
                "Canyon" => LoadProfile(ref canyonProfile, "CanyonSky"),
                "Valley" => LoadProfile(ref valleyProfile, "ValleySky"),
                "Tundra" => LoadProfile(ref tundraProfile, "TundraSky"),
                "Amethyst" => LoadProfile(ref amethystProfile, "AmethystSky"),
                "Company" => LoadProfile(ref companyProfile, "CompanySky"),
                _ => null
            };

            if (profile == null)
            {
                Plugin.Logger.LogDebug($"No profile found for tag: {TagName}");
                return;
            }

            // Handle special cases where the entire shared profile needs replacement
            if (TagName == "Amethyst" || TagName == "Company")
            {
                if (volume.name == "Sky and Fog Global Volume")
                {
                    volume.sharedProfile = profile;
                    return;
                }
            }

            if (!profile.TryGet<HDRISky>(out HDRISky newSky) || newSky?.hdriSky?.value == null)
            {
                Plugin.Logger.LogDebug($"Skipping profile application for tag: {TagName} due to missing HDRISky.");
                return;
            }

            if (volume.name == "Sky and Fog Global Volume" || volume.name == "Sky and Fog Global Volume (1)")
            {
                Plugin.Logger.LogDebug($"{vanillaSky.hdriSky.value.name} replaced with {newSky.hdriSky.value.name}");
                vanillaSky.hdriSky.value = newSky.hdriSky.value;
                vanillaSky.distortionMode.value = newSky.distortionMode.value;
                vanillaSky.rotation.value = newSky.rotation.value;
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
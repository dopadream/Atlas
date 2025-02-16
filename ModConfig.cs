using BepInEx.Configuration;
using System.Collections.Generic;

namespace Atlas
{
    public class ModConfig
    {

        internal static ConfigEntry<bool> configCanyonSky, configValleySky, configTundraSky, configCompanySky, configAmethystSky, configAmethystFog;

        internal static ConfigEntry<string> configMoonBlacklist, configCanyonOverrides, configValleyOverrides, configTundraOverrides, configCompanyOverrides, configAmethystOverrides;

        internal static void Init(ConfigFile cfg)
        {
            configCanyonSky = cfg.Bind("General", "Canyon Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Canyon\""));

            configValleySky = cfg.Bind("General", "Valley Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Valley\""));

            configTundraSky = cfg.Bind("General", "Tundra Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Tundra\""));

            configCompanySky = cfg.Bind("General", "Company Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Company\""));

            configAmethystSky = cfg.Bind("General", "Amethyst Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Amethyst\""));

            configAmethystFog = cfg.Bind("General", "Amethyst Fog", true,
                new ConfigDescription("Adds extra fog to the new Amethyst sky, applies to both interior and exterior."));

            // -- blacklist --

            configMoonBlacklist = cfg.Bind("Blacklist", "Moon Blacklist", "Experimentation, Artifice",
                new ConfigDescription("Planet names in this list will not have their skies overwritten. Must be separated by comma."));

            // -- overrides --

            configCanyonOverrides = cfg.Bind("Overrides", "Canyon", "",
                new ConfigDescription("Moon names in this list will forcefully load the Canyon profile. Must be separated by comma."));

            configValleyOverrides = cfg.Bind("Overrides", "Valley", "",
                new ConfigDescription("Moon names in this list will forcefully load the Valley profile. Must be separated by comma."));

            configTundraOverrides = cfg.Bind("Overrides", "Tundra", "",
                new ConfigDescription("Moon names in this list will forcefully load the Tundra profile. Must be separated by comma."));

            configCompanyOverrides = cfg.Bind("Overrides", "Company", "",
                new ConfigDescription("Moon names in this list will forcefully load the Company profile. Must be separated by comma."));

            configAmethystOverrides = cfg.Bind("Overrides", "Amethyst", "",
                new ConfigDescription("Moon names in this list will forcefully load the Amethyst profile. Must be separated by comma."));
        }
    }
}

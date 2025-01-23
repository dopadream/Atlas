using BepInEx.Configuration;

namespace Atlas
{
    public class ModConfig
    {

        internal static ConfigEntry<bool> configCanyonSky, configValleySky, configTundraSky, configAmethystSky, configAmethystFog;

        internal static ConfigEntry<string> configMoonBlacklist;

        internal static void Init(ConfigFile cfg)
        {
            configCanyonSky = cfg.Bind("General", "Canyon Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Canyon\""));

            configValleySky = cfg.Bind("General", "Valley Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Valley\""));

            configTundraSky = cfg.Bind("General", "Tundra Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Tundra\""));

            configAmethystSky = cfg.Bind("General", "Amethyst Skies", true,
                new ConfigDescription("Adds a new HDRI volume to moons tagged with \"Amethyst\""));

            configAmethystFog = cfg.Bind("General", "Amethyst Fog", true,
                new ConfigDescription("Adds extra fog to the new Amethyst sky, applies to both interior and exterior."));

            configMoonBlacklist = cfg.Bind("Blacklist", "Moon Blacklist", "Experimentation, Artifice",
                new ConfigDescription("Planet names in this list will not have their skies overwritten. Must be separated by comma."));
        }
    }
}

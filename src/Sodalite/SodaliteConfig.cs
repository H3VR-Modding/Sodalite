using BepInEx.Configuration;
using BepInEx.Logging;

namespace Sodalite;

internal class SodaliteConfig
{
	public ConfigEntry<bool> SpoofSteamUserID;
	public ConfigEntry<LogLevel> LogPanelFilter;

	public SodaliteConfig(ConfigFile config)
	{
		SpoofSteamUserID = config.Bind("Privacy", nameof(SpoofSteamUserID), false, "Randomizes your Steam User ID on every startup (requires restart)");
		LogPanelFilter = config.Bind("Universal Panel", nameof(LogPanelFilter), LogLevel.All & ~LogLevel.Debug, "Configures which types of log messages show up in the log panel");
	}
}

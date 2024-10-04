using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using YamlDotNet.Serialization;

namespace Sodalite.Api;

/// <summary>
/// Helper methods for discovering and obtaining information about other installed mods
/// </summary>
public static class ModsAPI
{
	private static readonly ReadOnlyDict<string, ThunderstorePackage> ThunderstorePackagesMutable = new();
	public static IReadOnlyDictionary<string, ThunderstorePackage> ThunderstorePackages => ThunderstorePackagesMutable;

	private static readonly ReadOnlyList<PluginInfo> BepInExPluginsMutable = new();
	public static IReadOnlyList<PluginInfo> BepInExPlugins => BepInExPluginsMutable;

	private static readonly ReadOnlyDict<PluginInfo, ThunderstorePackage> PluginToPackageLookupMutable = new();
	public static IReadOnlyDictionary<PluginInfo, ThunderstorePackage> PluginToPackageLookup => PluginToPackageLookupMutable;

	internal static void Discover()
	{
		ThunderstorePackagesMutable.Clear();
		BepInExPluginsMutable.Clear();
		PluginToPackageLookupMutable.Clear();

		// Check for the r2mm/TMM mods.yml file
		string modsYamlFile = Path.Combine(Path.GetDirectoryName(Paths.BepInExRootPath)!, "mods.yml");
		if (File.Exists(modsYamlFile))
		{
			// Deserialize the mods.yaml
			var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
			var installedPackages = deserializer.Deserialize<ThunderstorePackage[]>(File.OpenText(modsYamlFile));

			// Stick them all into the dict
			foreach (var enabledPackage in installedPackages)
			{
				ThunderstorePackagesMutable[enabledPackage.Name] = enabledPackage;
			}
		}
		else
		{
			Sodalite.Logger.LogWarning("[ModsAPI] mods.yml file not found, some functionality may be missing (Are you not using r2mm/TMM?).");
		}

		// Check BepInEx and try to match up the plugins to the thunderstore package they came from
		var bepinexPlugins = Chainloader.ManagerObject.GetComponents<BaseUnityPlugin>();
		string basePluginsPath = Path.GetFullPath(Paths.PluginPath);
		foreach (var pluginScript in bepinexPlugins)
		{
			PluginInfo pluginInfo = pluginScript.Info;
			BepInExPluginsMutable.Add(pluginInfo);

			// Grab the plugin's location and trim the bepinex plugin path from the start
			string assemblyPath = Path.GetFullPath(pluginInfo.Location);
			string relativePath = assemblyPath.Substring(basePluginsPath.Length).TrimStart(Path.DirectorySeparatorChar);

			// This should leave us with the name of the package it's from
			string packageName = Path.GetDirectoryName(relativePath)!;
			if (!string.IsNullOrEmpty(packageName) && ThunderstorePackages.TryGetValue(packageName, out var package))
			{
				// Everything matches up, we're good to go.
				package.PluginsMutable.Add(pluginInfo);
				PluginToPackageLookupMutable[pluginInfo] = package;
			}
			else
			{
				// Doesn't belong to a thunderstore package, probably an in-development plugin installed manually for testing.
				// We'll just make a stub package for it
				ThunderstorePackage orphanPackage = new("Unknown", pluginInfo.Metadata.Name, "", pluginInfo.Metadata.Version);
				orphanPackage.PluginsMutable.Add(pluginInfo);
				ThunderstorePackagesMutable[orphanPackage.Name] = orphanPackage;
				PluginToPackageLookupMutable[pluginInfo] = orphanPackage;
			}
		}

		// Output some diagnostics
		int packageCount = ThunderstorePackages.Count;
		int enabledPackageCount = ThunderstorePackages.Count(p => p.Value.Enabled);
		int bepinexPluginsCount = bepinexPlugins.Length;
		Sodalite.Logger.LogInfo($"[ModsAPI] Installed mods report: {packageCount} Thunderstore packages ({enabledPackageCount} enabled), {bepinexPluginsCount} BepInEx plugins");

		// Print out a nice report into the debug log
		foreach (var package in ThunderstorePackages.Values)
		{
			Sodalite.Logger.LogDebug("[ModsAPI] - Package: " + package);
			foreach (var plugin in package.Plugins)
			{
				Sodalite.Logger.LogDebug("[ModsApi]   - Plugin: " + plugin.Metadata.GUID + " " + plugin.Metadata.Version + " (" + Path.GetFileName(plugin.Location) + ")");
			}
		}
	}

	public class ThunderstorePackage
	{
		public struct Version(int major, int minor, int patch)
		{
			[YamlMember(Alias = "major")]
			public int Major { get; private set; } = major;

			[YamlMember(Alias = "minor")]
			public int Minor { get; private set; } = minor;

			[YamlMember(Alias = "patch")]
			public int Patch { get; private set; } = patch;

			public override string ToString() => $"{Major}.{Minor}.{Patch}";

			public static implicit operator Version(System.Version v)
			{
				return new Version(v.Major, v.Minor, v.Build);
			}
		}

		[YamlIgnore]
		public string Name => $"{AuthorName}-{DisplayName}";

		[YamlMember(Alias = "authorName")]
		public string AuthorName { get; private set; } = null!;

		[YamlMember(Alias = "displayName")]
		public string DisplayName { get; private set; } = null!;

		[YamlMember(Alias = "enabled")]
		public bool Enabled { get; private set; }

		[YamlMember(Alias = "icon")]
		public string IconPath { get; private set; } = null!;

		[YamlMember(Alias = "versionNumber")]
		public Version VersionNumber { get; private set; }

		[YamlIgnore]
		public IReadOnlyList<PluginInfo> Plugins => PluginsMutable;

		[YamlIgnore]
		internal readonly ReadOnlyList<PluginInfo> PluginsMutable = [];

		public override string ToString() => $"{AuthorName}-{DisplayName}-{VersionNumber}";

		public ThunderstorePackage()
		{

		}

		internal ThunderstorePackage(string authorName, string displayName, string iconPath, Version versionNumber)
		{
			AuthorName = authorName;
			DisplayName = displayName;
			IconPath = iconPath;
			VersionNumber = versionNumber;
		}
	}

	private class ReadOnlyDict<TKey, TValue> : Dictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
	{
		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
	}

	internal class ReadOnlyList<T> : List<T>, IReadOnlyList<T>
	{
	}
}



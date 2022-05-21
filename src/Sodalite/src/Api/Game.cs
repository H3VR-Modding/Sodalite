using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using FistVR;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sodalite.Api;

/// <summary>
///     Sodalite Game API provides methods for interfacing with the game's metadata
/// </summary>
public static class GameAPI
{
	private static readonly Func<string, Type, Object[]> OriginalResourcesLoadAll;
	private static readonly Dictionary<Type, List<Object>> InjectedAssets = new();

	/// <summary>
	///     The name of the beta the game is running, or an empty string if none.
	/// </summary>
	public static string BetaName { get; internal set; } = null!;

	/// <summary>
	///     The Steam Build ID the game is running, or -10 if unknown.
	/// </summary>
	public static int BuildId { get; internal set; }

	/// <summary>
	///     Intercepts Resources.LoadAll calls to append arbitrary resources to the result
	/// </summary>
	/// <param name="resource">Resource to inject</param>
	public static void InjectResource(Object resource)
	{
		// Make sure we have a key for this type and add it to the list
		var type = resource.GetType();
		if (!InjectedAssets.ContainsKey(type)) InjectedAssets[type] = new List<Object>();
		InjectedAssets[type].Add(resource);
	}

	/// <summary>
	///     Loads the asset bundle at the given path and injects all the resources
	/// </summary>
	/// <param name="assetBundle">Path to the asset bundle</param>
	public static void PreloadAllAssets(string assetBundle)
	{
		// Get the path of the plugin the bundle is from, and the plugin info of the plugin
		var pluginPath = Path.GetDirectoryName(Path.GetFullPath(assetBundle))!;
		var pluginInfo = Chainloader.PluginInfos.Values.FirstOrDefault(p => p.Location == pluginPath);

		// Load all the resources from the bundle
		var bundle = AssetBundle.LoadFromFile(assetBundle);
		foreach (var asset in bundle.LoadAllAssets())
		{
			switch (asset)
			{
				// Correct the prefab path if this is an FVRObject
				case FVRObject objectId:
					objectId.m_anvilPrefab.Bundle = Path.Combine(pluginPath, objectId.m_anvilPrefab.Bundle);
					objectId.IsModContent = true;
					break;

				// If this is an ItemSpawnerID, we need to set the FromMod field
				case ItemSpawnerID itemSpawnerID when pluginInfo != null:
					itemSpawnerID.FromMod = pluginInfo.Metadata.GUID;
					break;
			}

			// Inject the resource into the game
			InjectResource(asset);
		}
	}


#if RUNTIME
	static GameAPI()
	{
		// Hook Resources.LoadAll and create a trampoline for the original
		var detour = new NativeDetour(
			typeof(Resources).GetMethod("LoadAll", new[] {typeof(string), typeof(Type)}),
			typeof(GameAPI).GetMethod(nameof(Detour_Resources_LoadAll), BindingFlags.Static | BindingFlags.NonPublic)
		);
		OriginalResourcesLoadAll = detour.GenerateTrampoline<Func<string, Type, Object[]>>();
	}

	private static Object[] Detour_Resources_LoadAll(string path, Type type)
	{
		// Get the original result
		var original = OriginalResourcesLoadAll(path, type);

		// Check if we have anything to add to it
		if (InjectedAssets.TryGetValue(type, out var objects))
			original = original.Concat(objects).ToArray();

		// Then return the modified result
		return original;
	}
#endif
}

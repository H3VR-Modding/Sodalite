using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FistVR;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sodalite.Api
{
	/// <summary>
	/// Sodalite Game API provides methods for interfacing with the game's metadata
	/// </summary>
	public static class GameAPI
	{
		/// <summary>
		/// The name of the beta the game is running, or an empty string if none.
		/// </summary>
		public static string BetaName { get; internal set; } = null!;

		/// <summary>
		/// The Steam Build ID the game is running, or -10 if unknown.
		/// </summary>
		public static int BuildId { get; internal set; }

		private static readonly Func<string, Type, Object[]> OriginalResourcesLoadAll;
		private static readonly Dictionary<Type, List<Object>> InjectedAssets = new();


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
			Object[] original = OriginalResourcesLoadAll(path, type);

			// Check if we have anything to add to it
			if (InjectedAssets.TryGetValue(type, out var objects))
				original = original.Concat(objects).ToArray();

			// Then return the modified result
			return original;
		}
#endif

		/// <summary>
		/// Intercepts Resources.LoadAll calls to append arbitrary resources to the result
		/// </summary>
		/// <param name="resource">Resource to inject</param>
		public static void InjectResource(Object resource)
		{
			// Make sure we have a key for this type and add it to the list
			Type type = resource.GetType();
			if (!InjectedAssets.ContainsKey(type)) InjectedAssets[type] = new List<Object>();
			InjectedAssets[type].Add(resource);
		}

		/// <summary>
		/// Loads the asset bundle at the given path and injects all the resources
		/// </summary>
		/// <param name="assetBundle">Path to the asset bundle</param>
		public static void PreloadAllAssets(string assetBundle)
		{
			string pluginPath = Path.GetDirectoryName(Path.GetFullPath(assetBundle))!;

			// Load all the resources from the bundle
			AssetBundle bundle = AssetBundle.LoadFromFile(assetBundle);
			foreach (var asset in bundle.LoadAllAssets())
			{
				// Correct the prefab path if this is an FVRObject
				if (asset is FVRObject objectId)
				{
					objectId.m_anvilPrefab.Bundle = Path.Combine(pluginPath, objectId.m_anvilPrefab.Bundle);
					objectId.IsModContent = true;
				}

				// Inject the resource into the game
				InjectResource(asset);
			}
		}
	}
}

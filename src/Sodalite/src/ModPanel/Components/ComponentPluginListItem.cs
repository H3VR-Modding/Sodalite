#pragma warning disable CS1591

using System.Collections.Generic;
using System.IO;
using BepInEx;
using Sodalite.ModPanel.Pages;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

namespace Sodalite.ModPanel.Components;

public class SodalitePluginListItem : MonoBehaviour
{
	private static readonly Dictionary<string, Sprite> CachedIcons = new();
	private static readonly Dictionary<string, JObject> CachedManifests = new();

	public Text PluginName = null!;
	public Text PluginDescription = null!;
	public Image PluginIcon = null!;
	public Button PluginSettings = null!;
	public Button DocumentationButton = null!;

	public void ApplyFrom(PluginInfo plugin)
	{
		// Get the path of the plugin's assembly
		var pluginPath = Path.GetDirectoryName(plugin.Location)!;
		var iconPath = Path.Combine(pluginPath, "icon.png");
		var manifestPath = Path.Combine(pluginPath, "manifest.json");

		// Try to get the icon from the cache or from a file
		if (!CachedIcons.TryGetValue(iconPath, out var icon))
		{
			if (File.Exists(iconPath))
			{
				var tex = SodaliteUtils.LoadTextureFromFile(iconPath);
				icon = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
				CachedIcons[iconPath] = icon;
			}
			else
			{
				icon = null;
			}
		}

		// Try to get the manifest from the cache or from a file
		if (!CachedManifests.TryGetValue(manifestPath, out var manifest))
		{
			if (File.Exists(manifestPath))
			{
				manifest = JObject.Parse(File.ReadAllText(manifestPath));
				CachedManifests[manifestPath] = manifest;
			}
			else
			{
				manifest = null;
			}
		}

		// Apply the properties if they exist
		if (manifest != null)
		{
			PluginName.text = plugin.Metadata.Name;
			PluginDescription.text = manifest["description"].ToObject<string>();
		}
		else
		{
			PluginName.text = plugin.Metadata.Name;
			PluginDescription.text = "Bare plugin assembly from " + Path.GetFileName(plugin.Location);
		}

		PluginIcon.sprite = icon;

		// Setup the buttons
		PluginSettings.onClick.AddListener(() => UniversalModPanel.Instance.GetPageOfType<ModPanelConfigPage>()!.NavigateHere(plugin));
		if (!UniversalModPanel.RegisteredConfigs.ContainsKey(plugin))
		{
			PluginSettings.interactable = false;
			PluginSettings.transform.GetChild(0).GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
		}

		// TODO: Re-add this. The button got removed from the prefab.
		if (DocumentationButton != null)
		{
			DocumentationButton.onClick.AddListener(() => Sodalite.Logger.LogInfo("Stuff!"));
			if ( /*!UniversalModPanel.PluginsWithDocumentation.Contains(plugin)*/ true)
			{
				DocumentationButton.interactable = false;
				DocumentationButton.transform.GetChild(0).GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
			}
		}
	}
}

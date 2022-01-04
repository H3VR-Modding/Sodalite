#pragma warning disable CS1591
using System.IO;
using System.Linq;
using BepInEx.Bootstrap;
using Sodalite.ModPanel.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Pages;

public class ModPanelPluginListPage : UniversalModPanelPage
{
	public RectTransform ContentGameObject = null!;
	public SodalitePluginListItem ListItemPrefab = null!;
	public Text BannerText = null!;

	private void Awake()
	{
		// Don't do this in the editor.
		if (Application.isEditor) return;

		// Clear out all the children of the content
		for (int i = ContentGameObject.childCount - 1; i >= 0; i--)
			Destroy(ContentGameObject.GetChild(i).gameObject);

		// Re-initialize with all the plugins
		foreach (var plugin in Chainloader.PluginInfos.Values
			         .OrderByDescending(x => UniversalModPanel.RegisteredConfigs.ContainsKey(x))
			         .ThenByDescending(x => x.Metadata.Name))
		{
			// Skip any Mason-compiled assemblies and any monomod assemblies.
			if (plugin.Location.EndsWith("mm.dll") || Path.GetFileName(plugin.Location) == "bootstrap.dll") continue;

			var listItem = Instantiate(ListItemPrefab, ContentGameObject);
			listItem.ApplyFrom(plugin);
		}
	}
}

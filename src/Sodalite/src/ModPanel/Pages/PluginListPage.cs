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

		RedrawList();
	}

	internal void RedrawList()
	{
		// Clear out all the children of the content
		for (var i = ContentGameObject.childCount - 1; i >= 0; i--)
			Destroy(ContentGameObject.GetChild(i).gameObject);

		// Re-initialize with all the plugins
		foreach (var plugin in Chainloader.PluginInfos.Values
			         .Where(x => UniversalModPanel.RegisteredConfigs.ContainsKey(x) || UniversalModPanel.PluginsWithDocumentation.Contains(x))
			         .OrderByDescending(x => x.Metadata.Name))
		{
			var listItem = Instantiate(ListItemPrefab, ContentGameObject);
			listItem.ApplyFrom(plugin);
		}
	}
}

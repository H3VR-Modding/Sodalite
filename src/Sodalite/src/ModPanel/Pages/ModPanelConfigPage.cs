#pragma warning disable CS1591
using System;
using System.Linq;
using BepInEx;
using Sodalite.ModPanel.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Pages;

public class ModPanelConfigPage : UniversalModPanelPage
{
	public RectTransform ContentGameObject = null!;
	public Text SectionTextPrefab = null!;

	public SodaliteConfigInputFieldBase BoolField = null!;
	public SodaliteConfigInputFieldBase EnumField = null!;

	private void Awake()
	{
		UniversalModPanel.RegisteredInputFields[typeof(bool)] = BoolField;
		UniversalModPanel.RegisteredInputFields[typeof(Enum)] = EnumField;
	}

	public void NavigateHere(PluginInfo plugin)
	{
		// Update this page's title so it shows correctly in the breadcrumb
		DisplayName = plugin.Metadata.Name;
		UniversalModPanel.Instance.Navigate(this);

		// Clear out all the children of the content
		for (int i = ContentGameObject.childCount - 1; i >= 0; i--)
			Destroy(ContentGameObject.GetChild(i).gameObject);

		// Make sure we have registered config values
		if (!UniversalModPanel.RegisteredConfigs.TryGetValue(plugin, out var values))
			throw new InvalidOperationException("This plugin has no registered config options!");

		// Group them by the section
		foreach (var section in values.GroupBy(x => x.Definition.Section))
		{
			// Then create the section header and instantiate all the keys
			Instantiate(SectionTextPrefab, ContentGameObject).text = section.Key;
			foreach (var entry in section)
			{
				// Either pick the exact same type for the field, or the first which is a subclass of the actual type.
				if (!UniversalModPanel.RegisteredInputFields.TryGetValue(entry.SettingType, out var prefab))
				{
					prefab = UniversalModPanel.RegisteredInputFields
						.FirstOrDefault(x => entry.SettingType.IsSubclassOf(x.Key)).Value;
				}

				if (prefab is null)
					throw new InvalidOperationException($"The setting type {entry.SettingType} of {plugin.Metadata.Name} /  {section}.{entry.Definition.Key} is not supported.");
				Instantiate(prefab, ContentGameObject).Apply(entry);
			}
		}
	}
}

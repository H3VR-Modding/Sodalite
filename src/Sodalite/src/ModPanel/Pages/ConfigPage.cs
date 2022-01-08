#pragma warning disable CS1591
using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Pages;

public class ModPanelConfigPage : UniversalModPanelPage
{
	public RectTransform ContentGameObject = null!;
	public Text SectionTextPrefab = null!;

	public ConfigFieldBase BoolField = null!;
	public ConfigFieldBase ListField = null!;
	public ConfigFieldBase ColorField = null!;
	public ConfigFieldBase RangeField = null!;
	public ConfigFieldBase RawField = null!;

	private void Awake()
	{
		UniversalModPanel.RegisteredInputFields[BoolField] = x => x.SettingType == typeof(bool);
		UniversalModPanel.RegisteredInputFields[ColorField] = x => x.SettingType == typeof(Color);
		UniversalModPanel.RegisteredInputFields[ListField] = x => x.SettingType.IsEnum || SodaliteUtils.IsInstanceOfGenericType(typeof(AcceptableValueList<>), x.Description.AcceptableValues);
		UniversalModPanel.RegisteredInputFields[RangeField] = x => SodaliteUtils.IsInstanceOfGenericType(typeof(AcceptableValueRange<>), x.Description.AcceptableValues);
	}

	public void NavigateHere(PluginInfo plugin)
	{
		// Update this page's title so it shows correctly in the breadcrumb
		DisplayName = plugin.Metadata.Name;
		UniversalModPanel.Instance.Navigate(this);

		// Clear out all the children of the content
		for (var i = ContentGameObject.childCount - 1; i >= 0; i--)
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
				// Pick the first config field that passes the predicate or use the raw input (text) field if none match.
				var prefab = UniversalModPanel.RegisteredInputFields.FirstOrDefault(x => x.Value(entry)).Key;
				if (!prefab) prefab = RawField;

				// Instantiate it
				var field = Instantiate(prefab, ContentGameObject);
				field.Apply(entry);
				field.Redraw();
			}
		}
	}
}

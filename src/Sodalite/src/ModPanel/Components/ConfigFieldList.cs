#pragma warning disable CS1591
using System;
using System.Collections;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

public class ConfigFieldList : ConfigFieldBase
{
	public Text Text = null!;
	private int _index;
	private IList _values = new object[0];

	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);

		// Check if the type is an enum
		if (entry.SettingType.IsEnum) _values = Enum.GetValues(entry.SettingType);

		// Else, check if it has an acceptable values list
		else if (entry.Description.AcceptableValues is not null)
		{
			var prop = AccessTools.Property(entry.Description.AcceptableValues.GetType(), nameof(AcceptableValueList<int>.AcceptableValues));
			if (prop is not null) _values = (IList) prop.GetValue(entry.Description.AcceptableValues, null);
		}

		// Check if _values was set
		if (_values.Count == 0) throw new ArgumentException("Provided config entry does not have an acceptable values list, nor is it an enum.", nameof(entry));
		_index = _values.IndexOf(entry.BoxedValue);

	}

	public override void Redraw()
	{
		Text.text = ConfigEntry.BoxedValue.ToString();
	}

	public void Next()
	{
		_index = (_index + 1) % _values.Count;
		UpdateFromIndex();
	}

	public void Previous()
	{
		_index--;
		if (_index < 0) _index = _values.Count - 1;
		UpdateFromIndex();
	}

	private void UpdateFromIndex()
	{
		SetValue(_values[_index]);
		Redraw();
	}
}

#pragma warning disable CS1591
using System;
using BepInEx.Configuration;
using UnityEngine.UI;

namespace Sodalite.ModPanel.ConfigFields;

public class EnumConfigField : ConfigField<Enum>
{
	public Text Text = null!;
	private Type _enumType = null!;
	private int _index;

	private string[] _items = new string[0];

	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);
		_enumType = entry.SettingType;
		_items = Enum.GetNames(_enumType);
	}

	public override void Redraw()
	{
		var value = Enum.GetName(ConfigEntry.SettingType, ConfigEntry.BoxedValue)!;
		_index = Array.IndexOf(_items, value);
		Text.text = value;
	}

	public void Next()
	{
		_index = (_index + 1) % _items.Length;
		UpdateFromIndex();
	}

	public void Previous()
	{
		_index--;
		if (_index < 0) _index = _items.Length - 1;
		UpdateFromIndex();
	}

	private void UpdateFromIndex()
	{
		var value = _items[_index];
		SetValue(Enum.Parse(_enumType, value));
		Redraw();
	}
}

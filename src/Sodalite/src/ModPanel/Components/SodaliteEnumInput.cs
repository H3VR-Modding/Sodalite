#pragma warning disable CS1591
using System;
using BepInEx.Configuration;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

public class SodaliteEnumInput : SodaliteConfigInputField<Enum>
{
	public Text Text = null!;

	private string[] _items = new string[0];
	private int _index;
	private Type _enumType = null!;

	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);
		_enumType = entry.SettingType;
		_items = Enum.GetNames(_enumType);
		string value = Enum.GetName(entry.SettingType, entry.BoxedValue)!;
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
		string value = _items[_index];
		Text.text = value;
		SetValue(Enum.Parse(_enumType, value));
	}
}

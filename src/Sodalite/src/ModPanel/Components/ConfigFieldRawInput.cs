using System;
using System.ComponentModel;
using BepInEx.Configuration;
using UnityEngine;
using TypeConverter = System.ComponentModel.TypeConverter;

#pragma warning disable CS1591

namespace Sodalite.ModPanel.Components;

public class ConfigFieldRawInput : ConfigFieldBase
{
	public SodaliteTextInput Input = null!;
	private TypeConverter _converter = null!;

	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);
		Input.OnValueChanged += OnInputValueChanged;
		Input.Description = entry.Definition.Key;
		_converter = TypeDescriptor.GetConverter(ConfigEntry.SettingType);
	}

	private void OnInputValueChanged(string obj)
	{
		// Make sure we don't error out if the entered value is not valid for this type
		try
		{
			SetValue(_converter.ConvertFrom(obj)!);
		}
		catch
		{
			// ignored
		}

		Redraw();
	}

	public override void Redraw()
	{
		Input.InputField.text = _converter.ConvertToString(ConfigEntry.BoxedValue);
	}
}

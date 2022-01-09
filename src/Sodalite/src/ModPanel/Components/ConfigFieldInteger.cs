using System;
using BepInEx.Configuration;
using UnityEngine;

#pragma warning disable CS1591
namespace Sodalite.ModPanel.Components;

public class ConfigFieldInteger : ConfigFieldBase
{
	public SodaliteNumberInput Input = null!;

	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);
		Input.OnValueChanged += InputOnOnValueChanged;

		switch (entry.Description.AcceptableValues)
		{
			case AcceptableValueIntRangeStep intRangeStepped:
				Input.MinValue = intRangeStepped.MinValue;
				Input.MaxValue = intRangeStepped.MaxValue;
				Input.Step = intRangeStepped.Step;
				break;
			case AcceptableValueRange<int> range:
				Input.MinValue = range.MinValue;
				Input.MaxValue = range.MaxValue;
				Input.Step = 1;
				break;
			case AcceptableValueFloatRangeStep floatRangeStepped:
				Input.MinValue = floatRangeStepped.MinValue;
				Input.MaxValue = floatRangeStepped.MaxValue;
				Input.Step = floatRangeStepped.Step;
				break;
			default:
				throw new ArgumentException("Config entry did not specify any valid range for input", nameof(entry));
		}
	}

	private void InputOnOnValueChanged(float val)
	{
		if (typeof(int) == ConfigEntry.SettingType)
		{
			int value = (int) val;
			if (value == (int) ConfigEntry.BoxedValue) return;
			ConfigEntry.BoxedValue = value;
		}
		else
		{
			if (Mathf.Approximately(val, (float) ConfigEntry.BoxedValue)) return;
			ConfigEntry.BoxedValue = val;
		}

		Redraw();
	}

	public override void Redraw()
	{
		if (ConfigEntry.SettingType == typeof(int)) Input.Set((int) ConfigEntry.BoxedValue);
		else Input.Set((float) ConfigEntry.BoxedValue);
	}
}

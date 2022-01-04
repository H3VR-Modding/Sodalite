#pragma warning disable CS1591
using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

public abstract class SodaliteConfigInputFieldBase : MonoBehaviour
{
	public Text Name = null!;
	public Text Description = null!;

	private ConfigEntryBase _configEntry = null!;

	public virtual void Apply(ConfigEntryBase entry)
	{
		_configEntry = entry;
		Name.text = entry.Definition.Key;
		Description.text = entry.Description.Description;
	}

	protected void SetValue(object val)
	{
		_configEntry.BoxedValue = val;
	}
}

public abstract class SodaliteConfigInputField<TVal> : SodaliteConfigInputFieldBase
{
	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);
		if (!typeof(TVal).IsAssignableFrom(entry.SettingType))
			throw new InvalidOperationException($"The setting type {entry.SettingType} of {entry.Definition.Section}.{entry.Definition.Key} is not compatible. Expected {typeof(TVal)}");
	}
}

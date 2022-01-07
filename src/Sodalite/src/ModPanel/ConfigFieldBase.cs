#pragma warning disable CS1591
using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel;

public abstract class ConfigFieldBase : MonoBehaviour
{
	public Text Name = null!;
	public Text Description = null!;

	protected ConfigEntryBase ConfigEntry = null!;

	public virtual void Apply(ConfigEntryBase entry)
	{
		ConfigEntry = entry;
		Name.text = entry.Definition.Key;
		Description.text = entry.Description.Description;
	}

	protected void SetValue(object val)
	{
		ConfigEntry.BoxedValue = val;
	}

	public abstract void Redraw();
}

public abstract class ConfigField<TVal> : ConfigFieldBase
{
	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);

		// Double check we can actually use this type
		if (!typeof(TVal).IsAssignableFrom(entry.SettingType))
			throw new InvalidOperationException(
				$"The setting type {entry.SettingType} of {entry.Definition.Section}.{entry.Definition.Key} is not compatible. Expected {typeof(TVal)}");
	}
}

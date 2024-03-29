﻿#pragma warning disable CS1591
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
		Description.text = entry.Description.Description + $"\nDefault Value: {entry.DefaultValue}";
	}

	protected void SetValue(object val)
	{
		ConfigEntry.BoxedValue = val;
	}

	public abstract void Redraw();
}

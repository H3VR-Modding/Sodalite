#pragma warning disable CS1591
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

public class SodaliteNumberInput : MonoBehaviour
{
	public float MinValue;
	public float MaxValue;
	public float Value;
	public float Step;
	public Text Text = null!;

	public event Action<float>? OnValueChanged;

	public void Increase()
	{
		Set(Value + Step);
	}

	public void Decrease()
	{
		Set(Value - Step);
	}

	public void Set(float newValue)
	{
		Value = Mathf.Clamp(newValue, MinValue, MaxValue);
		Text.text = Value.ToString(CultureInfo.InvariantCulture);
		OnValueChanged?.Invoke(Value);
	}
}

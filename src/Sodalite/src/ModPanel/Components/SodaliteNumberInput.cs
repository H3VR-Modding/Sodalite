#pragma warning disable CS1591
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

public class SodaliteNumberInput : MonoBehaviour
{
	public float MinValue;
	public float MaxValue;
	public float Step;
	public float Value;
	public Text Text = null!;

	public Action<float>? OnValueChanged;

	public void Increase() => Set(Value + Step);

	public void Decrease() => Set(Value - Step);

	public void Set(float newValue)
	{
		Value = Mathf.Round(newValue / Step) * Step;
		Value = Mathf.Clamp(Value, MinValue, MaxValue);
		Text.text = Value + "";
		OnValueChanged?.Invoke(Value);
	}
}

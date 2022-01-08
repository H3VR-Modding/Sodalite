#pragma warning disable CS1591
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

public class SodaliteNumberInput : MonoBehaviour
{
	public int MinValue;
	public int MaxValue;
	public int Value;
	public Text Text = null!;

	public event Action<int>? OnValueChanged;

	public void Increase()
	{
		Set(Value + 1);
	}

	public void Decrease()
	{
		Set(Value - 1);
	}

	public void Set(int newValue)
	{
		Value = Mathf.Clamp(Value, MinValue, MaxValue);
		Text.text = Value.ToString();
		OnValueChanged?.Invoke(Value);
	}
}

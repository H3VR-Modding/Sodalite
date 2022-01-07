#pragma warning disable CS1591
using System;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Pages;

/// <summary>
///     Unity component for displaying and using the Universal Mod Panel color picker page
/// </summary>
public sealed class ModPanelColorPickerPage : UniversalModPanelPage
{
	public Slider RedSlider = null!;
	public Slider GreenSlider = null!;
	public Slider BlueSlider = null!;
	public Slider AlphaSlider = null!;
	public Text FieldName = null!;
	public Image OldColor = null!;
	public Image NewColor = null!;
	public Text OldColorText = null!;
	public Text NewColorText = null!;
	private Color _newColor;

	private Action<Color>? _onSuccess;
	private Action<Color>? _onUpdate;

	/// <summary>
	///     Opens the color picker page and prompts the user to edit a color
	/// </summary>
	/// <param name="fieldName">Name of the color field user is editing</param>
	/// <param name="currentColor">The existing color to edit</param>
	/// <param name="onSuccess">Callback when the user confirms the new color</param>
	/// <param name="onUpdate">Optional callback for when a color is being picked, to show a preview outside the panel in real-time</param>
	public void PickColor(string fieldName, Color currentColor, Action<Color> onSuccess, Action<Color>? onUpdate = null)
	{
		// Open this page
		Panel.Navigate(this);
		_onSuccess = onSuccess;
		_onUpdate = onUpdate;
		_newColor = currentColor;

		// Setup the sliders (we want them to be in the range 0-15)
		RedSlider.value = currentColor.r * RedSlider.maxValue;
		GreenSlider.value = currentColor.g * GreenSlider.maxValue;
		BlueSlider.value = currentColor.b * BlueSlider.maxValue;
		AlphaSlider.value = currentColor.a * AlphaSlider.maxValue;

		// Setup the other things
		FieldName.text = fieldName;
		OldColor.color = currentColor;
		OldColorText.text = "Old color: " + currentColor.AsRGBA();
		NewColor.color = _newColor;
		NewColorText.text = "New color: " + _newColor.AsRGBA();
	}

	public void SliderValueChanged()
	{
		// Construct the color object and update the text and image
		_newColor = new Color(RedSlider.normalizedValue, GreenSlider.normalizedValue, BlueSlider.normalizedValue, AlphaSlider.normalizedValue);
		NewColor.color = _newColor;
		NewColorText.text = "New color: " + _newColor.AsRGBA();
		_onUpdate?.Invoke(_newColor);
	}

	public void Cancel()
	{
		// Exit
		Panel.NavigateBack();
	}

	public void Confirm()
	{
		// Callback and exit
		_onSuccess?.Invoke(_newColor);
		Panel.NavigateBack();
	}
}

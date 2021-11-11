using System;
using System.Collections;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel
{
	/// <summary>
	/// Unity component for displaying and using the Universal Mod Panel color picker page
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

		private Action<Color> _onSuccess;
		private Color _newColor;

		public void PickColor(string fieldName, Color currentColor, Action<Color> onSuccess)
		{
			// Open this page
			Panel.Navigate(this);
			_onSuccess = onSuccess;
			_newColor = currentColor;

			// Setup the sliders (we want them to be in the range 0-15)
			RedSlider.value = currentColor.r * 15;
			GreenSlider.value = currentColor.g * 15;
			BlueSlider.value = currentColor.b * 15;
			AlphaSlider.value = currentColor.a * 15;

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
			_newColor = new Color(RedSlider.value / 15f, GreenSlider.value / 15f, BlueSlider.value / 15f, AlphaSlider.value / 15f);
			NewColor.color = _newColor;
			NewColorText.text = "New color: " + _newColor.AsRGBA();
		}

		public void Cancel()
		{
			// Exit
			Panel.NavigateBack();
		}

		public void Confirm()
		{
			// Callback and exit
			_onSuccess(_newColor);
			Panel.NavigateBack();
		}
	}
}

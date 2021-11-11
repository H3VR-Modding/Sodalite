using Sodalite.UiWidgets.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel
{
	public class UITestsPage : UniversalModPanelPage
	{
		public Text ButtonText = null!;
		public Text SliderText = null!;

		public SodaliteNumberInput NumberInput = null!;
		public SodaliteSlider SliderInput = null!;
		public SodaliteColorPicker ColorInput = null!;

		private void Awake()
		{
			NumberInput.OnValueChanged = NumberInputChanged;
			SliderInput.OnValueChanged = SliderValueChanged;
			ColorInput.ColorChanged = PickColor;
		}

		public void ToggleButtonClick()
		{
			ButtonText.text = ButtonText.text == "Button <color=red>Off</color>" ? "Button <color=green>On</color>" : "Button <color=red>Off</color>";
		}

		private void SliderValueChanged(float val)
		{
			SliderText.text = "Value: " + val;
		}

		private void NumberInputChanged(float val)
		{

		}

		private void PickColor(Color c)
		{

		}
	}
}

#pragma warning disable CS1591
using Sodalite.ModPanel.Components;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Pages;

public class UITestsPage : UniversalModPanelPage
{
	public Text ButtonText = null!;
	public Text SliderText = null!;

	public SodaliteSlider Slider = null!;

	private void Awake()
	{
		Slider.OnValueChanged = SliderValueChanged;
	}

	public void ToggleButtonClick()
	{
		ButtonText.text = ButtonText.text == "Button <color=red>Off</color>" ? "Button <color=green>On</color>" : "Button <color=red>Off</color>";
	}

	private void SliderValueChanged(float val)
	{
		SliderText.text = "Value: " + val;
	}
}

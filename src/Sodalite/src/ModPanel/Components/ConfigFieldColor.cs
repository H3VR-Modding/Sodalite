#pragma warning disable CS1591
using Sodalite.ModPanel.Pages;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.ConfigFields;

public class ColorConfigField : ConfigField<Color>
{
	public Image Image = null!;
	public Text Text = null!;

	private Color _currentColor;

	private void SetColor(Color c)
	{
		SetValue(c);
		Redraw();
	}

	public void OpenPicker()
	{
		UniversalModPanel.Instance.GetPageOfType<ModPanelColorPickerPage>()!.PickColor(ConfigEntry.Definition.Key, _currentColor, SetColor);
	}

	public override void Redraw()
	{
		_currentColor = (Color) ConfigEntry.BoxedValue;
		Text.text = _currentColor.AsRGBA();
		Image.color = _currentColor;
		Text.color = _currentColor.maxColorComponent < 0.8f ? Color.white : Color.black;
	}
}

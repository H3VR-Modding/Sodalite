#pragma warning disable CS1591
using BepInEx.Configuration;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

public class SodaliteBoolInput : SodaliteConfigInputField<bool>
{
	public Button ToggleButton;
	public Text ButtonText;
	private bool _state;

	private const string TextTrue = "<color=grey>False</color> / <color=green>True</color>";
	private const string TextFalse = "<color=red>False</color> / <color=grey>True</color>";

	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);
		_state = (bool) entry.BoxedValue;
		ButtonText.text = _state ? TextTrue : TextFalse;
	}

	public void Toggle()
	{
		_state = !_state;
		ButtonText.text = _state ? TextTrue : TextFalse;
		SetValue(_state);
	}
}

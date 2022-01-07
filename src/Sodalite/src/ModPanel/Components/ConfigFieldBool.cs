#pragma warning disable CS1591
using UnityEngine.UI;

namespace Sodalite.ModPanel.ConfigFields;

public class BoolConfigField : ConfigField<bool>
{
	private const string TextTrue = "<color=grey>False</color> / <color=green>True</color>";
	private const string TextFalse = "<color=red>False</color> / <color=grey>True</color>";
	public Text ButtonText = null!;
	private bool _state;


	public void Toggle()
	{
		SetValue(!_state);
		Redraw();
	}

	public override void Redraw()
	{
		_state = (bool) ConfigEntry.BoxedValue;
		ButtonText.text = _state ? TextTrue : TextFalse;
	}
}

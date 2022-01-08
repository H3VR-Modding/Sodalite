using BepInEx.Configuration;

#pragma warning disable CS1591
namespace Sodalite.ModPanel.Components;

public class ConfigFieldInteger : ConfigFieldBase
{
	public SodaliteNumberInput Input = null!;

	public override void Apply(ConfigEntryBase entry)
	{
		base.Apply(entry);
		Input.OnValueChanged += InputOnOnValueChanged;

		if (entry.Description.AcceptableValues is AcceptableValueRange<int> range)
		{
			Input.MinValue = range.MinValue;
			Input.MaxValue = range.MaxValue;
		}
	}

	private void InputOnOnValueChanged(int val)
	{
		int current = (int) ConfigEntry.BoxedValue;
		if (current != val)
		{
			ConfigEntry.BoxedValue = val;
			Redraw();
		}
	}

	public override void Redraw()
	{
		Input.Set((int) ConfigEntry.BoxedValue);
	}
}

using System;
using BepInEx.Configuration;

namespace Sodalite.ModPanel;

/// <summary>
///		Acceptable values class which accepts integer min, max, and step values.
/// </summary>
public class AcceptableValueIntRangeStep : AcceptableValueRange<int>
{
	/// <summary>
	/// The step increment value
	/// </summary>
	public int Step { get; }

	/// <inheritdoc />
	public AcceptableValueIntRangeStep(int minValue, int maxValue, int step) : base(minValue, maxValue)
	{
		Step = step;
	}

	/// <inheritdoc />
	public override object Clamp(object value)
	{
		int val = (int) base.Clamp(value);
		var a = (int) Math.Round((val - MinValue) / (float) Step);
		return MinValue + a * Step;
	}

	/// <inheritdoc />
	public override bool IsValid(object value)
	{
		int val = (int) value;
		return base.IsValid(value) && (val - MinValue) % Step == 0;
	}

	/// <inheritdoc />
	public override string ToDescriptionString() => $"# Acceptable value range: From {MinValue} to {MaxValue} in increments of {Step}";
}

/// <summary>
///		Acceptable values class which accepts integer min, max, and step values.
/// </summary>
public class AcceptableValueFloatRangeStep : AcceptableValueRange<float>
{
	/// <summary>
	/// The step increment value
	/// </summary>
	public float Step { get; }

	/// <inheritdoc />
	public AcceptableValueFloatRangeStep(float minValue, float maxValue, float step) : base(minValue, maxValue)
	{
		Step = step;
	}

	/// <inheritdoc />
	public override object Clamp(object value)
	{
		float val = (float) base.Clamp(value);
		var a = (float) Math.Round((val - MinValue) / Step);
		return MinValue + a * Step;
	}

	/// <inheritdoc />
	public override bool IsValid(object value)
	{
		float val = (float) value;
		return base.IsValid(value) && (val - MinValue) % Step == 0;
	}

	/// <inheritdoc />
	public override string ToDescriptionString() => $"# Acceptable value range: From {MinValue} to {MaxValue} in increments of {Step}";
}

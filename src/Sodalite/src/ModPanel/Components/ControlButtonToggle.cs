using System;
using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

/// <summary>
/// Similar to <see cref="SodaliteBetterPointableButton"/> except that it toggles state on click and passes that state to a handler
/// </summary>
public class SodaliteToggleButton : FVRPointable
{
	[SerializeField] private bool BeginEnabled = false;
	[SerializeField] private GameObject EnableOnToggle = null!;
	[SerializeField] private Image Graphic = null!;

	private Button _button = null!;
	private bool _state;
	private Color _originalGraphicTint;

	/// <summary>
	/// Event for when this button's value changes
	/// </summary>
	public event Action<bool>? OnValueChanged;

	private void Awake()
	{
		_button = GetComponent<Button>();
		if (Graphic != null) _originalGraphicTint = Graphic.color;
		_state = BeginEnabled;
		UpdateState();
	}

	/// <summary>
	/// Sets the toggles state of this button
	/// </summary>
	/// <param name="value">If the button is enabled</param>
	public void SetState(bool value)
	{
		if (_state == value) return;
		_state = value;
		UpdateState();
		OnValueChanged?.Invoke(_state);
	}

	/// <summary>
	/// Toggles the state of this button
	/// </summary>
	public void ToggleState()
	{
		_state = !_state;
		UpdateState();
		OnValueChanged?.Invoke(_state);
	}

	private void UpdateState()
	{
		if (EnableOnToggle != null) EnableOnToggle.SetActive(BeginEnabled != _state);
		if (Graphic != null)
		{
			Graphic.color = BeginEnabled == _state ? _originalGraphicTint : Color.gray;
		}
	}

	/// <summary>
	///     Called when a hand starts hovering over this pointable
	/// </summary>
	public override void BeginHoverDisplay()
	{
		// Proxy a pointer enter to the button
		_button.OnPointerEnter(null);
	}

	/// <summary>
	///     Called when a hand stops hovering over this pointable
	/// </summary>
	public override void EndHoverDisplay()
	{
		// Proxy a pointer exit to the button
		_button.OnPointerExit(null);
	}

	/// <summary>
	///     Called per-frame per-hand that is pointing at this pointable
	/// </summary>
	/// <param name="hand">The hand pointing at this</param>
	public override void OnPoint(FVRViveHand hand)
	{
		base.OnPoint(hand);
		if (hand.Input.TriggerUp)
			SetState(!_state);
	}
}

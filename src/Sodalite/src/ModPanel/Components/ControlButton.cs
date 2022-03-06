#pragma warning disable CS1591
using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

/// <summary>
///     A better pointable button component that doesn't require assigning redundant options
///     and allows for making use of the 'interactable' field of the Button.
/// </summary>
[RequireComponent(typeof(Button), typeof(BoxCollider))]
public class SodaliteBetterPointableButton : FVRPointable
{
	private Button _button = null!;

	private void Awake()
	{
		_button = GetComponent<Button>();
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

		// If the trigger is clicked invoke the onClick event of the button
		// NOTE: I use trigger up here instead of trigger down because the Steam overlay keyboard
		//       immediately closes itself otherwise.
		if (hand.Input.TriggerUp)
			_button.onClick.Invoke();
	}
}

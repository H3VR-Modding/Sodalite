using System;
using FistVR;

namespace Sodalite.UiWidgets.Components
{
	/// <summary>
	/// Extension of FVRPointableButton which gives additional information when the button is clicked.
	/// </summary>
	public class SodalitePointableButton : FVRPointableButton
	{
		/// <summary>
		/// Event called when the button is clicked. This event is called in addition to the Unity onClick, so don't register your callback there too.
		/// </summary>
		public event EventHandler<ButtonClickEventArgs>? ButtonClicked;

		/// <summary>
		/// Called every frame the button is being pointed at.
		/// </summary>
		/// <param name="hand">The hand that is pointing at this.</param>
		public override void OnPoint(FVRViveHand hand)
		{
			base.OnPoint(hand);
			if (hand.Input.TriggerDown) ButtonClicked?.Invoke(this, new ButtonClickEventArgs(hand));
		}
	}
}

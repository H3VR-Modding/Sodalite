using System;
using FistVR;

namespace Sodalite
{
	/// <summary>
	/// Delegate for when a button is clicked
	/// </summary>
	public delegate void ButtonClickEvent(object sender, ButtonClickEventArgs args);

	/// <summary>
	/// Event arguments for when a button is clicked. This includes UiWidget and WristMenu buttons.
	/// </summary>
	public class ButtonClickEventArgs : EventArgs
	{
		/// <summary>
		/// The hand that clicked this button
		/// </summary>
		public FVRViveHand Hand { get; }

		/// <summary>
		/// Constructor for this event
		/// </summary>
		public ButtonClickEventArgs(FVRViveHand hand)
		{
			Hand = hand;
		}
	}
}

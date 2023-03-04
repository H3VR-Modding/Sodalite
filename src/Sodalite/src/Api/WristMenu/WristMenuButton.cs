using FistVR;

namespace Sodalite.Api;

/// <summary>
///     This class represents a custom button on the wrist menu. This abstraction is required
///     because the wrist menu buttons are just game objects and would be lost on a scene load
///     so this class is used to store the button's information and re-create for you when required.
/// </summary>
public class WristMenuButton
{
	/// <summary>
	///     Constructor for the wrist menu button taking the text and a click action
	/// </summary>
	/// <param name="text">The text to display on this button</param>
	/// <param name="clickAction">The callback for when the button is clicked</param>
	public WristMenuButton(string text, ButtonClickEvent? clickAction = null) : this(text, 0, clickAction)
	{
	}

	/// <summary>
	///     Constructor for the wrist menu button taking the text, priority and a click action
	/// </summary>
	/// <param name="text">The text to display on this button</param>
	/// <param name="priority">The priority of the button. This decides the order of buttons on the wrist menu</param>
	/// <param name="clickAction">The callback for when the button is clicked</param>
	public WristMenuButton(string text, int priority, ButtonClickEvent? clickAction = null)
	{
		Text = text;
		Priority = priority;
		if (clickAction is not null) OnClick += clickAction;
	}

	/// <summary>
	///     The text that this button will display when shown on the wrist menu.
	/// </summary>
	public string Text { get; }

	/// <summary>
	///     The priority of this button. Priority determines the order in which buttons appear.
	/// </summary>
	public int Priority { get; }

	/// <summary>
	///     Event callback for when this button is clicked on by the player
	/// </summary>
	public event ButtonClickEvent? OnClick;

	internal void CallOnClick(FVRViveHand hand)
	{
		OnClick?.Invoke(this, new ButtonClickEventArgs(hand));
	}
}

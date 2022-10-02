using System;
using System.Collections.Generic;
using FistVR;
using Sodalite.Utilities;
using Object = UnityEngine.Object;

namespace Sodalite.Api;

/// <summary>
///     Sodalite Wrist Menu API for adding and removing custom wrist menu buttons.
/// </summary>
public static class WristMenuAPI
{
	private static readonly ObservableHashSet<WristMenuButton> WristMenuButtons = new();
	private static LegacyButtonsWristMenuSection? _wristMenuSection;

	/// <summary>
	///     Reference to the current instance of the game's wrist menu.
	/// </summary>
	[Obsolete("FVRWristMenu is a deprecated class. Use WristMenuAPI.Instance2 instead.", true)]
	public static FVRWristMenu? Instance
	{
		get
		{
			Sodalite.StaticLogger.LogWarning("Mod tried to access WristMenuAPI.Instance, this is an obsolete property. Please update your mod to use FVRWristMenu2.");
			return null;
		}
	}

	/// <summary>
	///     Reference to the current instance of the game's wrist menu.
	/// </summary>
	public static FVRWristMenu2? Instance2 { get; private set; }

	/// <summary>
	///     Collection of wrist menu buttons. Add to this collection to register a button and remove from the collection to unregister.
	/// </summary>
	public static ICollection<WristMenuButton> Buttons => WristMenuButtons;

	private static void WristMenuButtonsItemAdded(WristMenuButton button)
	{
		if (Instance2 == null) return;
		AddWristMenuButton(Instance2, button);
	}

	private static void WristMenuButtonsItemRemoved(WristMenuButton button)
	{
		if (Instance2 == null) return;
		RemoveWristMenuButton(Instance2, button);
	}

	private static void AddWristMenuButton(FVRWristMenu2 wristMenu, WristMenuButton button)
	{
		// Check if we need to create the section first
		if (_wristMenuSection == null)
		{
			wristMenu.RegenerateButtons();
		}
	}

	private static void RemoveWristMenuButton(FVRWristMenu2 wristMenu, WristMenuButton button)
	{
		// If there's no buttons left, remove the button from the menu and destroy it.
		if (WristMenuButtons.Count == 0 && _wristMenuSection != null)
		{
			wristMenu.Sections.Remove(_wristMenuSection);
			Object.Destroy(_wristMenuSection.gameObject);
			wristMenu.RegenerateButtons();
		}
	}

#if RUNTIME
	static WristMenuAPI()
	{
		// Wrist Menu stuff

		On.FistVR.FVRWristMenu2.Awake += FVRWristMenuOnAwake;
		WristMenuButtons.ItemAdded += WristMenuButtonsItemAdded;
		WristMenuButtons.ItemRemoved += WristMenuButtonsItemRemoved;
	}

	private static void FVRWristMenuOnAwake(On.FistVR.FVRWristMenu2.orig_Awake orig, FVRWristMenu2 self)
	{
		// Note to self; this is required and very important.
		orig(self);

		// Keep our reference to the wrist menu up to date
		Instance2 = self;


		// For all the registered buttons, add them
		foreach (var button in WristMenuButtons)
			AddWristMenuButton(self, button);
	}
#endif
}

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

internal class LegacyButtonsWristMenuSection : FVRWristMenuSection
{
	private void Awake()
	{
		// Set the text to something descriptive
		ButtonText = "Sodalite (+Legacy)";
	}
}

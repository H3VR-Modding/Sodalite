using System;
using System.Collections.Generic;
using System.Linq;
using FistVR;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sodalite.Api;

/// <summary>
///     Sodalite Wrist Menu API for adding and removing custom wrist menu buttons.
/// </summary>
public static class WristMenuAPI
{
	private static readonly ObservableHashSet<WristMenuButton> WristMenuButtons = new();
	private static readonly Dictionary<GameObject, WristMenuButton> ExistingButtons = new();
	private static FVRWristMenuSection? _wristMenuSection;

	/// <summary>
	///     Reference to the current instance of the game's wrist menu.
	/// </summary>
	[Obsolete("FVRWristMenu is a deprecated class. Use WristMenuAPI.Instance2 instead.", true)]
	public static FVRWristMenu? Instance
	{
		get
		{
			Sodalite.Logger.LogWarning("Mod tried to access WristMenuAPI.Instance but this is now an obsolete property. If an error occurs immediately after this message it's probably related to this. Please update your mod to use FVRWristMenu2.");
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

	private static void AddWristMenuButton(WristMenuButton button)
	{
		// If the wrist menu doesn't exist yet we don't care
		if (Instance2 == null) return;

		// Local function used to set the text and on-click actions of a created button
		static void SetupButton(WristMenuButton button, Transform buttonTransform)
		{
			// Add it to our list of buttons
			ExistingButtons.Add(buttonTransform.gameObject, button);

			// Change the text on the button
			buttonTransform.GetComponentInChildren<Text>().text = button.Text;

			// Remove any existing listeners on the button by assigning a new onClick event and add our own listener
			Button buttonButton = buttonTransform.GetComponent<Button>();
			buttonButton.onClick = new Button.ButtonClickedEvent();
			buttonButton.onClick.AddListener(() =>
			{
				// Play a beep and call the onclick handler
				SM.PlayGlobalUISound(SM.GlobalUISound.Beep, buttonTransform.position);
				button.CallOnClick(Instance2!.m_currentHand.OtherHand);
			});
		}

		// Check if we need to create the section first
		if (_wristMenuSection == null)
		{
			// Make a copy of the 'Spawn' section as that's basically what we want
			FVRWristMenuSection originalComponent = Instance2.Sections.First(x => x.GetType() == typeof(FVRWristMenuSection_Spawn));
			Transform originalTransform = originalComponent.transform;
			FVRWristMenuSection oldSectionComponent = Object.Instantiate(originalComponent, originalTransform.position, originalTransform.rotation, originalTransform.parent);

			// Destroy the existing section component and replace it with our own
			GameObject sectionGo = oldSectionComponent.gameObject;
			Object.Destroy(oldSectionComponent);
			_wristMenuSection = sectionGo.AddComponent<FVRWristMenuSection>();
			_wristMenuSection.ButtonText = "Custom Buttons";
			_wristMenuSection.Menu = Instance2;

			// Add to the list of sections and let the game redraw those buttons
			Instance2.Sections.Add(_wristMenuSection);
			Instance2.RegenerateButtons();

			// Grab one of the existing buttons to modify
			RectTransform wristMenuSectionTransform = (RectTransform) _wristMenuSection.transform;
			RectTransform bottomButton = (RectTransform) wristMenuSectionTransform.GetChild(0);

			// Remove all the buttons except for that one
			int originalChildCount = wristMenuSectionTransform.childCount;
			for (int i = originalChildCount - 1; i >= 0; i--)
			{
				RectTransform child = (RectTransform) wristMenuSectionTransform.GetChild(i);
				if (child == bottomButton) continue;

				// I know what I'm doing.
				Object.DestroyImmediate(child.gameObject);
			}

			// Assign the button's properties and resize + reorder the section
			SetupButton(button, bottomButton);
			ResizeAndReorderSection(wristMenuSectionTransform, originalChildCount);
		}
		else
		{
			// Not the first button to get added so we can skip some steps from above
			// Doesn't matter which button we duplicate here because we'll need to move and re-order all of them anyway
			var wristMenuSectionTransform = (RectTransform) _wristMenuSection.transform;
			Transform templateButton = wristMenuSectionTransform.GetChild(0);
			var newButton = (RectTransform) Object.Instantiate(templateButton.gameObject, templateButton.position, templateButton.rotation, templateButton.parent).transform;
			SetupButton(button, newButton);
			ResizeAndReorderSection(wristMenuSectionTransform, wristMenuSectionTransform.childCount - 1);
		}
	}

	private static void RemoveWristMenuButton(WristMenuButton button)
	{
		// If the wrist menu doesn't exist yet we don't care
		if (Instance2 == null || _wristMenuSection == null) return;

		// Destroy this button and then redo the size and ordering
		GameObject buttonGo = ExistingButtons.First(kv => kv.Value == button).Key;
		Object.DestroyImmediate(buttonGo); // I know what I'm doing
		var wristMenuSectionTransform = (RectTransform) _wristMenuSection.transform;
		ResizeAndReorderSection(wristMenuSectionTransform, wristMenuSectionTransform.childCount + 1);

		// If there's no buttons left, remove the button from the menu and destroy it.
		if (WristMenuButtons.Count == 0 && _wristMenuSection != null)
		{
			Instance2.Sections.Remove(_wristMenuSection);
			Object.Destroy(_wristMenuSection.gameObject);
			Instance2.RegenerateButtons();
			_wristMenuSection = null;
		}
	}

	// Local function used to resize the section background and reorder the buttons inside it
	private static void ResizeAndReorderSection(RectTransform background, int oldButtonCount)
	{
		// Calculate some variables
		Vector2 backgroundSize = background.sizeDelta;
		int newButtonCount = background.childCount;
		RectTransform button = (RectTransform) background.GetChild(0);
		float buttonHeight = button.sizeDelta.y * button.localScale.y; // Should be 28
		float verticalPadding = (backgroundSize.y - buttonHeight * oldButtonCount) / (oldButtonCount + 2); // Should be 2
		float buttonSpacing = buttonHeight + verticalPadding; // Should be 30

		// Resize the background of the section
		background.sizeDelta = new Vector2(backgroundSize.x, newButtonCount * buttonSpacing + verticalPadding * 2);

		// Order the buttons by their priority (placing higher priority buttons at the top)
		float currentHeight = buttonSpacing / 2 + verticalPadding;
		foreach (GameObject buttonObj in ExistingButtons.Keys.OrderBy(x => ExistingButtons[x].Priority))
		{
			buttonObj.transform.localPosition = new Vector3(0, currentHeight, 0);
			currentHeight += buttonSpacing;
		}
	}

	internal static void WristMenuAwake(FVRWristMenu2 instance)
	{
		// Keep our reference to the wrist menu up to date
		Instance2 = instance;
		ExistingButtons.Clear();

		// For all the registered buttons, add them
		foreach (var button in WristMenuButtons)
			AddWristMenuButton(button);
	}

	static WristMenuAPI()
	{
		// Wrist Menu stuff
		WristMenuButtons.ItemAdded += AddWristMenuButton;
		WristMenuButtons.ItemRemoved += RemoveWristMenuButton;
	}
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

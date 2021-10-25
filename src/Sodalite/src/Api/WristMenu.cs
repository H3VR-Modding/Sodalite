using System.Collections.Generic;
using System.Linq;
using FistVR;
using HarmonyLib;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sodalite.Api
{
	/// <summary>
	///	Sodalite Wrist Menu API for adding and removing custom wrist menu buttons.
	/// </summary>
	public static class WristMenuAPI
	{
		/// <summary>
		/// Reference to the current instance of the game's wrist menu.
		/// </summary>
		public static FVRWristMenu? Instance { get; private set; }

		/// <summary>
		///	Collection of wrist menu buttons. Add to this collection to register a button and remove from the collection to unregister.
		/// </summary>
		public static ICollection<WristMenuButton> Buttons => WristMenuButtons;

		private static readonly ObservableHashSet<WristMenuButton> WristMenuButtons = new();
		private static readonly Dictionary<WristMenuButton, Button> CurrentButtons = new();

#if RUNTIME
		static WristMenuAPI()
		{
			// Wrist Menu stuff

			On.FistVR.FVRWristMenu.Awake += FVRWristMenuOnAwake;
			WristMenuButtons.ItemAdded += WristMenuButtonsItemAdded;
			WristMenuButtons.ItemRemoved += WristMenuButtonsItemRemoved;
		}

		private static void FVRWristMenuOnAwake(On.FistVR.FVRWristMenu.orig_Awake orig, FVRWristMenu self)
		{
			// Note to self; this is required and very important.
			orig(self);

			// Keep our reference to the wrist menu up to date
			Instance = self;

			// Clear the list of existing buttons
			CurrentButtons.Clear();

			// For all the registered buttons, add them
			foreach (WristMenuButton button in WristMenuButtons)
				AddWristMenuButton(self, button);
		}
#endif

		private static void WristMenuButtonsItemAdded(WristMenuButton button)
		{
			if (Instance is null || !Instance) return;
			AddWristMenuButton(Instance, button);
		}

		private static void WristMenuButtonsItemRemoved(WristMenuButton button)
		{
			if (Instance is null || !Instance) return;
			RemoveWristMenuButton(Instance, button);
		}

		private static void AddWristMenuButton(FVRWristMenu wristMenu, WristMenuButton button)
		{
			// The button we want to use as a reference is either the spectator button (wristMenu.Buttons[16])
			// or the button just above where this one should go according to the priority
			WristMenuButton? aboveButton = WristMenuButtons
				.OrderByDescending(x => x.Priority)
				.FirstOrDefault(x => x.Priority > button.Priority);
			Button referenceButton = aboveButton is null ? wristMenu.Buttons[16] : CurrentButtons[aboveButton];
			RectTransform referenceRt = referenceButton.GetComponent<RectTransform>();

			// Expand the canvas by the height of this button
			RectTransform canvas = wristMenu.transform.Find("MenuGo/Canvas").GetComponent<RectTransform>();
			OptionsPanel_ButtonSet buttonSet = canvas.GetComponent<OptionsPanel_ButtonSet>();
			Vector2 size = canvas.sizeDelta;
			size.y += referenceRt.sizeDelta.y;
			canvas.sizeDelta = size;

			// So for any UI elements that are LOWER than this button, move them down by the height of the button
			foreach (RectTransform child in canvas)
			{
				if (!(child.anchoredPosition.y < referenceRt.anchoredPosition.y)) continue;
				Vector2 pos1 = child.anchoredPosition;
				pos1.y -= referenceRt.sizeDelta.y;
				child.anchoredPosition = pos1;
			}

			// Copy the spectator button and place it where it should be
			Button newButton = Object.Instantiate(referenceButton, canvas);
			RectTransform newButtonRt = newButton.GetComponent<RectTransform>();
			Vector2 pos = newButtonRt.anchoredPosition;
			pos.y -= referenceRt.sizeDelta.y;
			newButtonRt.anchoredPosition = pos;

			// Apply the options
			newButton.GetComponentInChildren<Text>().text = button.Text;
			newButton.onClick = new Button.ButtonClickedEvent();
			newButton.onClick.AddListener(() =>
			{
				wristMenu.Aud.PlayOneShot(wristMenu.AudClip_Engage);

				// The hand that isn't showing the wrist menu is the one that clicked the button
				FVRViveHand hand = GM.CurrentMovementManager.Hands[GM.CurrentMovementManager.Hands[0] == wristMenu.m_currentHand ? 1 : 0];
				button.CallOnClick(hand);
			});

			// Now we need to modify some things to accomodate this new button
			FVRWristMenuPointableButton pointable = newButton.GetComponent<FVRWristMenuPointableButton>();
			pointable.ButtonIndex = wristMenu.Buttons.Count;
			buttonSet.ButtonImagesInSet = buttonSet.ButtonImagesInSet.AddToArray(newButton.GetComponent<Image>());
			wristMenu.Buttons.Add(newButton);

			// Finally add it to the dict and call the create event
			CurrentButtons.Add(button, newButton);
		}

		private static void RemoveWristMenuButton(FVRWristMenu wristMenu, WristMenuButton button)
		{
			// This time our reference is the current button
			Button referenceButton = CurrentButtons[button];
			RectTransform referenceRt = referenceButton.GetComponent<RectTransform>();

			// Shrink the canvas by the height of this button
			RectTransform canvas = wristMenu.transform.Find("MenuGo/Canvas").GetComponent<RectTransform>();
			OptionsPanel_ButtonSet buttonSet = canvas.GetComponent<OptionsPanel_ButtonSet>();
			Vector2 size = canvas.sizeDelta;
			size.y -= referenceRt.sizeDelta.y;
			canvas.sizeDelta = size;

			// So for any UI elements that are LOWER than this button, move them up by the height of the button
			foreach (RectTransform child in canvas)
			{
				if (!(child.anchoredPosition.y < referenceRt.anchoredPosition.y)) continue;
				Vector2 pos1 = child.anchoredPosition;
				pos1.y += referenceRt.sizeDelta.y;
				child.anchoredPosition = pos1;
			}

			// Then remove it from the internal stuff.
			// Unfortunately, removing a button requires us to re-assign the index values of all the buttons on the wrist menu :P
			wristMenu.Buttons.Remove(CurrentButtons[button]);
			buttonSet.ButtonImagesInSet = wristMenu.Buttons.Select(x => x.GetComponent<Image>()).ToArray();
			for (int i = 0; i < buttonSet.ButtonImagesInSet.Length; i++)
			{
				FVRWristMenuPointableButton pointable = buttonSet.ButtonImagesInSet[i].GetComponent<FVRWristMenuPointableButton>();
				pointable.ButtonIndex = i;
			}

			// Destroy the object and remove the button from the dict
			Object.Destroy(referenceButton.gameObject);
			CurrentButtons.Remove(button);
		}
	}

	/// <summary>
	///	This class represents a custom button on the wrist menu. This abstraction is required
	/// because the wrist menu buttons are just game objects and would be lost on a scene load
	/// so this class is used to store the button's information and re-create for you when required.
	/// </summary>
	public class WristMenuButton
	{
		/// <summary>
		///	The text that this button will display when shown on the wrist menu.
		/// </summary>
		public string Text { get; }

		/// <summary>
		///	The priority of this button. Priority determines the order in which buttons appear.
		/// </summary>
		public int Priority { get; }

		/// <summary>
		///	Event callback for when this button is clicked on by the player
		/// </summary>
		public event ButtonClickEvent? OnClick;

		/// <summary>
		///	Constructor for the wrist menu button taking the text and a click action
		/// </summary>
		/// <param name="text">The text to display on this button</param>
		/// <param name="clickAction">The callback for when the button is clicked</param>
		public WristMenuButton(string text, ButtonClickEvent? clickAction = null) : this(text, 0, clickAction)
		{
		}

		/// <summary>
		///	Constructor for the wrist menu button taking the text, priority and a click action
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

		internal void CallOnClick(FVRViveHand hand)
		{
			OnClick?.Invoke(this, new ButtonClickEventArgs(hand));
		}
	}
}

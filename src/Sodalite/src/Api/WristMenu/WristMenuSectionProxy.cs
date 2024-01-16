using System;
using System.Collections.Generic;
using System.Linq;
using FistVR;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sodalite.Api;

public abstract class WristMenuSectionProxy
{
	private FVRWristMenuSection? _section;
	private readonly ObservableHashSet<WristMenuButton> _buttonHashSet = new();
	private readonly Dictionary<GameObject, WristMenuButton> _existingButtons = new();
	protected bool AllowRemovingButtons = true;

	/// <summary>
	/// Collection of custom buttons added to this section
	/// </summary>
	public ICollection<WristMenuButton> Buttons => _buttonHashSet;

	/// <summary>
	/// Used to obtain a reference to the wrist menu section we'll be operating on
	/// </summary>
	/// <returns>The section's primary component</returns>
	protected abstract FVRWristMenuSection FindSection();

	public WristMenuSectionProxy()
	{
		_buttonHashSet.ItemAdded += AddButton;
		_buttonHashSet.ItemRemoved += RemoveButton;
	}

	internal void WristMenuAwake()
	{
		// Add all of our buttons
		_existingButtons.Clear();
		foreach (var button in _buttonHashSet) AddButton(button);
	}

	private void AddButton(WristMenuButton button)
	{
		// Disregard if the wrist menu isn't spawned in
		if (WristMenuAPI.Instance2 == null) return;

		// Make sure we have a reference to the section we're adding to
		if (_section == null) _section = FindSection();

		// Grab a button to use as a template
		// Doesn't matter which button we duplicate here because we'll need to move and re-order all of them anyway
		var wristMenuSectionTransform = (RectTransform) _section.transform;
		Transform templateButton = wristMenuSectionTransform.GetChild(0);

		// If this button is disabled we want to re-enable it and re-use it rather than making a new one
		RectTransform newButton;
		int oldButtonCount;
		if (templateButton.gameObject.activeSelf)
		{
			oldButtonCount = wristMenuSectionTransform.childCount;
			newButton = (RectTransform) Object.Instantiate(templateButton.gameObject, templateButton.position, templateButton.rotation, templateButton.parent).transform;
		}
		else
		{
			oldButtonCount = 1;
			newButton = (RectTransform) templateButton;
			newButton.gameObject.SetActive(true);
		}

		// Initialize the button and reorder them all
		SetupButton(button, newButton);
		ResizeAndReorderSection(wristMenuSectionTransform, oldButtonCount);
	}

	// Used to set the text and on-click actions of a created button
	private void SetupButton(WristMenuButton button, Transform buttonTransform)
	{
		// Add it to our list of buttons
		_existingButtons.Add(buttonTransform.gameObject, button);

		// Change the text on the button
		Text textComponent = buttonTransform.GetComponentInChildren<Text>();
		textComponent.text = button.Text;

		// Remove any existing listeners on the button by assigning a new onClick event and add our own listener
		Button buttonButton = buttonTransform.GetComponent<Button>();
		buttonButton.onClick = new Button.ButtonClickedEvent();
		buttonButton.onClick.AddListener(() =>
		{
			// Play a beep and call the onclick handler
			SM.PlayGlobalUISound(SM.GlobalUISound.Beep, buttonTransform.position);
			button.CallOnClick(WristMenuAPI.Instance2!.m_currentHand.OtherHand);
		});

		button.CallOnCreate(buttonTransform.gameObject, textComponent);
	}

	// Used to resize the section background and reorder the buttons inside it
	protected void ResizeAndReorderSection(RectTransform background, int oldButtonCount)
	{
		// Make sure we have at least one child.
		if (background.childCount == 0) return;

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
		foreach (GameObject buttonObj in background.gameObject.EnumerateChildren().OrderBy(x => _existingButtons.ContainsKey(x) ? _existingButtons[x].Priority : int.MaxValue))
		{
			buttonObj.transform.localPosition = new Vector3(0, currentHeight, 0);
			currentHeight += buttonSpacing;
		}
	}

	private void RemoveButton(WristMenuButton button)
	{
		// If this section doesn't allow removing buttons throw an exception
		if (!AllowRemovingButtons) throw new InvalidOperationException("Buttons cannot be removed from this section.");

		// If the wrist menu doesn't exist yet we don't care
		if (WristMenuAPI.Instance2 == null || _section == null) return;

		// Destroy this button and then redo the size and ordering
		GameObject buttonGo = _existingButtons.First(kv => kv.Value == button).Key;
		_existingButtons.Remove(buttonGo);
		Object.DestroyImmediate(buttonGo); // I know what I'm doing
		var wristMenuSectionTransform = (RectTransform) _section.transform;
		ResizeAndReorderSection(wristMenuSectionTransform, wristMenuSectionTransform.childCount + 1);

		// If there's no buttons left, remove the button from the menu and destroy it.
		if (_buttonHashSet.Count == 0 && _section != null)
		{
			WristMenuAPI.Instance2.Sections.Remove(_section);
			Object.Destroy(_section.gameObject);
			WristMenuAPI.Instance2.RegenerateButtons();
			_section = null;
		}
	}
}

// Used to make a NEW section on the wrist menu that a mod can add buttons to
internal class NewWristMenuSectionProxy : WristMenuSectionProxy
{
	private readonly string _buttonText;

	public NewWristMenuSectionProxy(string buttonText)
	{
		_buttonText = buttonText;
	}

	protected override FVRWristMenuSection FindSection()
	{
		// Make a copy of the 'Spawn' section as that's basically what we want
		FVRWristMenuSection originalComponent = WristMenuAPI.Instance2!.Sections.First(x => x.GetType() == typeof(FVRWristMenuSection_Spawn));
		Transform originalTransform = originalComponent.transform;
		FVRWristMenuSection oldSectionComponent = Object.Instantiate(originalComponent, originalTransform.position, originalTransform.rotation, originalTransform.parent);

		// Destroy the existing section component and replace it with our own
		GameObject sectionGo = oldSectionComponent.gameObject;
		Object.Destroy(oldSectionComponent);
		var section = sectionGo.AddComponent<FVRWristMenuSection>();
		section.ButtonText = _buttonText;
		section.Menu = WristMenuAPI.Instance2;

		// Add to the list of sections and let the game redraw those buttons
		WristMenuAPI.Instance2.Sections.Add(section);
		WristMenuAPI.Instance2.RegenerateButtons();

		// Remove all except for one button
		RectTransform wristMenuSectionTransform = (RectTransform) section.transform;
		RectTransform bottomButton = (RectTransform) wristMenuSectionTransform.GetChild(0);
		int originalChildCount = wristMenuSectionTransform.childCount;
		for (int i = originalChildCount - 1; i >= 0; i--)
		{
			RectTransform child = (RectTransform) wristMenuSectionTransform.GetChild(i);
			if (child == bottomButton) continue;

			// I know what I'm doing.
			Object.DestroyImmediate(child.gameObject);
		}

		// Hide it for later. This will become our first button when we add them
		bottomButton.gameObject.SetActive(false);
		ResizeAndReorderSection(wristMenuSectionTransform, originalChildCount);

		// Return created section
		return section;
	}
}

// Used to allow a mod to add buttons to an existing section
internal class TypedWristMenuSectionProxy<TSection> : WristMenuSectionProxy where TSection : FVRWristMenuSection
{
	internal TypedWristMenuSectionProxy()
	{
		AllowRemovingButtons = false;
	}

	protected override FVRWristMenuSection FindSection()
	{
		return WristMenuAPI.Instance2!.Sections.First(x => x.GetType() == typeof(TSection));
	}
}

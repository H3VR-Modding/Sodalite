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
	/// <summary>
	///     Reference to the current instance of the game's wrist menu.
	/// </summary>
	[Obsolete("FVRWristMenu is a deprecated class. Use WristMenuAPI.Instance2 instead.", true)]
	public static FVRWristMenu? Instance
	{
		get
		{
			Sodalite.Logger.LogWarning(
				"Mod tried to access WristMenuAPI.Instance but this is now an obsolete property. If an error occurs immediately after this message it's probably related to this. Please update your mod to use FVRWristMenu2.");
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
	[Obsolete("Use WristMenuAPI.CustomButtonsSection.Buttons instead.")]
	public static ICollection<WristMenuButton> Buttons => CustomButtonsSection.Buttons;

	/// <summary>
	/// Proxy for the Spawn Tools section of the wrist menu
	/// </summary>
	public static readonly WristMenuSectionProxy SpawnSection = new TypedWristMenuSectionProxy<FVRWristMenuSection_Spawn>();

	/// <summary>
	/// Proxy for the 'Custom Buttons' section of the wrist menu
	/// </summary>
	public static readonly WristMenuSectionProxy CustomButtonsSection = new NewWristMenuSectionProxy("Custom Buttons");

	private static readonly List<WristMenuSectionProxy> ExtraSections = new();

	internal static void WristMenuAwake(FVRWristMenu2 instance)
	{
		// Keep our reference to the wrist menu up to date
		Instance2 = instance;

		// Awake all wrist menus section proxies
		SpawnSection.WristMenuAwake();
		CustomButtonsSection.WristMenuAwake();
		foreach (var section in ExtraSections) section.WristMenuAwake();
	}

	/// <summary>
	/// Use this if you want to add your own section on the wrist menu that uses the default simple style.
	/// </summary>
	/// <param name="buttonText">The name of your section</param>
	/// <returns>A proxy object that you can use to add buttons to your section</returns>
	public static WristMenuSectionProxy CreateSection(string buttonText)
	{
		var newSection = new NewWristMenuSectionProxy(buttonText);
		ExtraSections.Add(newSection);
		return newSection;
	}
}

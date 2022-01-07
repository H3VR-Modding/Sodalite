#pragma warning disable CS1591
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace Sodalite.ModPanel.Components;

public class SodaliteTextInput : MonoBehaviour
{
	[Header("References")] public InputField InputField = null!;

	[Header("Keyboard options")] public EGamepadTextInputMode InputMode = EGamepadTextInputMode.k_EGamepadTextInputModeNormal;
	public EGamepadTextInputLineMode LineMode = EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine;
	public string Description = "Text Input";
	public uint MaxChars = 256;

	private bool _isKeyboardOpen;

	private void Awake()
	{
		SteamVR_Events.System(EVREventType.VREvent_KeyboardCharInput).Listen(OnKeyboard);
		SteamVR_Events.System(EVREventType.VREvent_KeyboardClosed).Listen(OnKeyboardClosed);
		InputField.onValueChanged.AddListener(value => OnValueChanged?.Invoke(value));
	}

	/// <summary>
	///     Callback for when the value of this field is changed
	/// </summary>
	public event Action<string>? OnValueChanged;

	private void OnKeyboard(VREvent_t args)
	{
		// Make sure we're the one who opened this keyboard so we don't get confused with any other keyboard events
		if (!_isKeyboardOpen) return;

		// Get the input and handle the special cases
		var input = GetKeyboardInput(args.data.keyboard);
		switch (input)
		{
			// This means the user hit backspace
			case "\b":
				var len = InputField.text.Length;
				if (len > 0)
					InputField.text = InputField.text.Substring(0, len - 1);
				break;

			// This means the user closed the keyboard
			case "\x1b":
				SteamVR.instance.overlay.HideKeyboard();
				_isKeyboardOpen = false;
				break;

			// Otherwise it's just some simple added input
			default:
				InputField.text += input;
				break;
		}
	}

	private void OnKeyboardClosed(VREvent_t args)
	{
		OnValueChanged?.Invoke(InputField.text);
	}

	// Since most of the SteamVR stuff is just a wrapper around native code, of course we have to do this...
	private string GetKeyboardInput(VREvent_Keyboard_t kb)
	{
		byte[] inputBytes =
		{
			kb.cNewInput0, kb.cNewInput1, kb.cNewInput2, kb.cNewInput3, kb.cNewInput4, kb.cNewInput5, kb.cNewInput6, kb.cNewInput7
		};
		var len = 0;
		for (; inputBytes[len] != 0 && len < 7; len++)
		{
		}

		return Encoding.UTF8.GetString(inputBytes, 0, len);
	}

	/// <summary>
	///     Opens the keyboard and starts listening for input for this field
	/// </summary>
	public void ShowKeyboard()
	{
		SteamVR.instance.overlay.ShowKeyboard((int) InputMode, (int) LineMode, Description, MaxChars, InputField.text, true, 0);
		_isKeyboardOpen = true;
	}
}

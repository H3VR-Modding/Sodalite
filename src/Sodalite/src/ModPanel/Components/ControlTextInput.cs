#pragma warning disable CS1591
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace Sodalite.ModPanel.Components;

public class SodaliteTextInput : MonoBehaviour
{
	[Header("References")]
	public InputField InputField = null!;

	[Header("Keyboard options")]
	public EGamepadTextInputMode InputMode = EGamepadTextInputMode.k_EGamepadTextInputModeNormal;
	public EGamepadTextInputLineMode LineMode = EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine;
	public string Description = "Text Input";
	public uint MaxChars = 256;

	private bool _isKeyboardOpen;

	private void Awake()
	{
		SteamVR_Events.System(EVREventType.VREvent_KeyboardDone).Listen(OnKeyboardDone);
		InputField.onValueChanged.AddListener(value => OnValueChanged?.Invoke(value));
	}

	/// <summary>
	///     Callback for when the value of this field is changed
	/// </summary>
	public event Action<string>? OnValueChanged;

	private void OnKeyboardDone(VREvent_t args)
	{
		if (!_isKeyboardOpen) return;
		_isKeyboardOpen = false;
		StringBuilder sb = new StringBuilder(1024);
		SteamVR.instance.overlay.GetKeyboardText(sb, 1024);
		string text = sb.ToString();
		InputField.text = text;
		OnValueChanged?.Invoke(text);
	}

	/// <summary>
	///     Opens the keyboard and starts listening for input for this field
	/// </summary>
	public void ShowKeyboard()
	{
		SteamVR.instance.overlay.ShowKeyboard((int) InputMode, (int) LineMode, Description, MaxChars, InputField.text, false, 0);
		_isKeyboardOpen = true;
	}
}

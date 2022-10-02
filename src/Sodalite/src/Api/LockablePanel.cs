using System;
using FistVR;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sodalite.Api;

/// <summary>
///     Sodalite Lockable Panel API for making custom lockable panels.
/// </summary>
public static class LockablePanelAPI
{
	/// <summary>
	///     Returns a new lockable panel that has been completely emptied of content
	/// </summary>
	/// <returns>The parent game object of the panel</returns>
	/// <exception cref="InvalidOperationException">Method was called before a reference to the options panel prefab was taken</exception>
	public static GameObject GetCleanLockablePanel()
	{
		var panel = Object.Instantiate(GM.CurrentOptionsPanel);
		CleanPanel(panel);
		return panel;
	}

	private static void CleanPanel(GameObject panel)
	{
		var panelTransform = panel.transform;

		// This proto object has a bunch of hidden stuff we don't want, but it does also contain the actual panel model
		// So just move it up and delete the proto
		var proto = panelTransform.Find("OptionsPanelProto");
		proto.Find("Tablet").SetParent(panelTransform);
		Object.Destroy(proto.gameObject);

		// Then, everything else we want to delete in the main object is disabled so use that as a filter
		foreach (Transform child in panelTransform)
			if (!child.gameObject.activeSelf)
				Object.Destroy(child.gameObject);

		// Lastly we just want to clear out the main canvas
		var canvas = panelTransform.Find("OptionsCanvas_0_Main/Canvas");
		foreach (Transform child in canvas) Object.Destroy(child.gameObject);

		// Then remove the old component
		Object.Destroy(panel.GetComponent<OptionsPanel_Screenmanager>());
	}
}

/// <summary>
///     The LockablePanel class represents a lockable panel similar to the options panel in game.
///     This abstraction is required because Unity Game Objects are scoped to the scene that they are
///     instantiated in, so this class will re-create and configure the panel for you when you try and
///     get it if necessary.
/// </summary>
public class LockablePanel
{
	// And also a reference to this object's current panel
	private GameObject? _currentPanel;

	/// <summary>
	///     The texture that will be applied to this panel when it is configured. Leaving this null
	///     will leave the default blue texture on the panel.
	/// </summary>
	public Texture2D? TextureOverride;

	/// <summary>
	///     Event callback for when this panel is being configured. This is called once every time a
	///     new panel game object needs to be created so if you need to run any setup code for your
	///     panel, do it here.
	/// </summary>
	public event Action<GameObject>? Configure;

	/// <summary>
	///     Returns this instance of the lockable panel, creating and configuring it if necessary.
	/// </summary>
	/// <returns>The parent game object of the panel</returns>
	public GameObject GetOrCreatePanel()
	{
		// If we've never made a panel or it's gotten destroyed make a new one
		if (_currentPanel is null || !_currentPanel)
		{
			// Make a new empty panel
			_currentPanel = LockablePanelAPI.GetCleanLockablePanel();

			// If we have a texture override, set it here
			if (TextureOverride is not null && TextureOverride)
			{
				var tabletRenderer = _currentPanel.transform.Find("Tablet").GetComponent<Renderer>();
				tabletRenderer.material.mainTexture = TextureOverride;
			}

			// Invoke the configure event
			Configure?.Invoke(_currentPanel);
		}

		return _currentPanel;
	}
}

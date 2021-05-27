using System;
using FistVR;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sodalite.Api
{
	/// <summary>
	///		Sodalite Lockable Panel API for making custom lockable panels.
	/// </summary>
	public class LockablePanelAPI
	{
		// Internal constructor so no one can make a second one of these
		internal LockablePanelAPI()
		{
		}

		/// <summary>
		/// Returns a new lockable panel that has been completely emptied of content
		/// </summary>
		/// <returns>The parent game object of the panel</returns>
		/// <exception cref="InvalidOperationException">Method was called before a reference to the options panel prefab was taken</exception>
		public GameObject GetCleanLockablePanel()
		{
			FVRWristMenu? wristMenu = H3Api.WristMenu.Instance;
			if (wristMenu is null || !wristMenu)
				throw new InvalidOperationException("You're trying to create a lockable panel too early! Please wait until the runtime phase.");

			GameObject panel = Object.Instantiate(wristMenu.OptionsPanelPrefab);
			CleanPanel(panel);
			return panel;
		}

		private static void CleanPanel(GameObject panel)
		{
			Transform panelTransform = panel.transform;

			// This proto object has a bunch of hidden stuff we don't want, but it does also contain the actual panel model
			// So just move it up and delete the proto
			Transform proto = panelTransform.Find("OptionsPanelProto");
			proto.Find("Tablet").SetParent(panelTransform);
			Object.Destroy(proto.gameObject);

			// Then, everything else we want to delete in the main object is disabled so use that as a filter
			foreach (Transform child in panelTransform)
				if (!child.gameObject.activeSelf)
					Object.Destroy(child.gameObject);

			// Lastly we just want to clear out the main canvas
			Transform canvas = panelTransform.Find("OptionsCanvas_0_Main/Canvas");
			foreach (Transform child in canvas) Object.Destroy(child.gameObject);

			// Then remove the old component
			Object.Destroy(panel.GetComponent<OptionsPanel_Screenmanager>());
		}
	}

	public class LockablePanel
	{
		// And also a reference to this object's current panel
		private GameObject? _currentPanel;
		public Texture2D? TextureOverride;
		public event Action<GameObject>? Configure;

		/// <summary>
		/// Returns this instance of the lockable panel, creating and configuring it if necessary.
		/// </summary>
		/// <returns>The parent game object of the panel</returns>
		public GameObject GetOrCreatePanel()
		{
			// If we've never made a panel or it's gotten destroyed make a new one
			if (_currentPanel is null || !_currentPanel)
			{
				// Make a new empty panel
				_currentPanel = H3Api.LockablePanel.GetCleanLockablePanel();

				// If we have a texture override, set it here
				if (TextureOverride is not null && TextureOverride)
				{
					Renderer tabletRenderer = _currentPanel.transform.Find("Tablet").GetComponent<Renderer>();
					tabletRenderer.material.mainTexture = TextureOverride;
				}

				// Invoke the configure event
				Configure?.Invoke(_currentPanel);
			}

			return _currentPanel;
		}
	}
}

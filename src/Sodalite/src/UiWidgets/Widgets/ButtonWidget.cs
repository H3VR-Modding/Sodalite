using System.Collections;
using FistVR;
using Sodalite.Api;
using Sodalite.UiWidgets.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.UiWidgets
{
	/// <summary>
	/// UiWidget that represents a button with text and a background image
	/// </summary>
	public class ButtonWidget : UiWidget
	{
		/// <summary>Reference to the Unity Button component of this widget</summary>
		public Button Button = null!;

		/// <summary>Reference to the Unity Text component of this widget</summary>
		public Text ButtonText = null!;

		/// <summary>Reference to the Unity Image component of this widget</summary>
		public Image ButtonImage = null!;

		/// <summary>Reference to the pointable component of this widget</summary>
		public SodalitePointableButton Pointable = null!;

		private BoxCollider _boxCollider = null!;

		/// <inheritdoc cref="UiWidget.Awake"/>
		protected override void Awake()
		{
			base.Awake();
			ButtonImage = gameObject.AddComponent<Image>();
			Button = gameObject.AddComponent<Button>();
			ButtonImage.sprite = Style.ButtonSprite;
			ButtonImage.color = Style.ButtonColorUnselected;

			// Get the text stuff setup
			GameObject child = new("Text");
			child.transform.SetParent(transform);
			ButtonText = child.AddComponent<Text>();
			((RectTransform) child.transform).FillParent();
			ButtonText.alignment = TextAnchor.MiddleCenter;
			ButtonText.color = Style.TextColor;
			ButtonText.font = Style.TextFont;

			// The PointableButton component is given to us in the base UiWidgets class
			SodalitePointableButton pointable = gameObject.AddComponent<SodalitePointableButton>();
			pointable.MaxPointingRange = 2;
			pointable.Button = Button;
			pointable.Image = ButtonImage;
			pointable.ColorUnselected = Style.ButtonColorUnselected;
			pointable.ColorSelected = Style.ButtonColorSelected;
		}

		private IEnumerator Start()
		{
			// Wait one frame for the layout groups to do their work
			yield return null;

			// Lastly we need a collider for the buttons
			_boxCollider = gameObject.AddComponent<BoxCollider>();
			Vector2 size = RectTransform.sizeDelta;
			_boxCollider.center = Vector3.zero;
			_boxCollider.size = new Vector3(size.x, size.y, 5f);
		}

		/// <summary>
		///	Adds a button listener. This method will automatically add the button click sound effect. If you don't want the sound effect, you can
		/// manually subscribe to the <see cref="SodalitePointableButton.ButtonClicked"/> event of <see cref="Pointable"/>
		/// </summary>
		/// <param name="callback">The action to preform when the button is clicked</param>
		public void AddButtonListener(ButtonClickEvent callback)
		{
			Pointable.ButtonClicked += (_, args) =>
			{
				// If we have references to everything we need to play a sound, play a sound
				FVRWristMenu? wristMenu = WristMenuAPI.Instance;
				if (AudioSource is not null && AudioSource && wristMenu is not null && wristMenu)
					AudioSource.PlayOneShot(wristMenu.AudClip_Engage);

				// Create the event and fire the callback
				callback(this, args);
			};
		}
	}
}

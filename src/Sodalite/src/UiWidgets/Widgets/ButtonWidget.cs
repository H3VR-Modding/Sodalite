using System;
using System.Collections;
using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.UiWidgets
{
	public class ButtonWidget : UiWidget
	{
		public Button Button = null!;
		public Text ButtonText = null!;
		public Image ButtonImage = null!;

		private BoxCollider _boxCollider = null!;

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
			FVRPointableButton pointable = gameObject.AddComponent<FVRPointableButton>();
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
		///		Adds a button listener. This method will automatically add the button click sound effect.
		/// </summary>
		/// <param name="callback">The action to preform when the button is clicked</param>
		public void AddButtonListener(Action callback)
		{
			Button.onClick.AddListener(() =>
			{
				// If we have references to everything we need to play a sound, play a sound
				FVRWristMenu? wristMenu = H3Api.WristMenu.Instance;
				if (AudioSource is not null && AudioSource && wristMenu is not null && wristMenu)
					AudioSource.PlayOneShot(wristMenu.AudClip_Engage);
				callback();
			});
		}
	}
}

using System;
using FistVR;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.UiWidgets.Components
{
	public class SodaliteSlider : FVRPointable
	{
		private Slider _slider;
		private FVRViveHand? _selectingHand;

		public Action<float>? OnValueChanged;

		private void Awake()
		{
			_slider = GetComponent<Slider>();
			_slider.onValueChanged.AddListener(val => OnValueChanged?.Invoke(val));
		}

		public override void Update()
		{
			base.Update();

			if (_selectingHand)
			{
				if (_selectingHand!.Input.TriggerUp)
					_selectingHand = null;
				else
				{
					// Convert the hand raycast hit point to a point along the slider
					RectTransform handleSlideArea = (RectTransform) _slider.handleRect.parent;
					float margin = handleSlideArea.sizeDelta.x / 2;
					Vector3 delta = new Vector3(((RectTransform) transform).sizeDelta.x / 2 + margin, 0f, 0f);
					Vector3 min = handleSlideArea.TransformPoint(-delta);
					Vector3 max = handleSlideArea.TransformPoint(delta);
					Vector3 pointOnLine = Math3D.ProjectPointOnLineSegment(min, max, _selectingHand!.m_pointingHit.point);

					// Get how far along the slider that point is and set our value accordingly
					_slider.value = Mathf.Lerp(_slider.minValue, _slider.maxValue, Math3D.InverseLerpVector3(min, max, pointOnLine));
					OnValueChanged?.Invoke(_slider.value);
				}
			}
		}

		public override void OnPoint(FVRViveHand hand)
		{
			base.OnPoint(hand);

			if (hand.Input.TriggerDown && !_selectingHand)
				_selectingHand = hand;
		}
	}
}

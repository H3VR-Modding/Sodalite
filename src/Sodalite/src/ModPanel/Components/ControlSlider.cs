#pragma warning disable CS1591
using System;
using FistVR;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

public class SodaliteSlider : FVRPointable
{
	private FVRViveHand? _selectingHand;
	private Slider _slider = null!;

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
			{
				_selectingHand = null;
			}
			else
			{
				// Convert the hand raycast hit point to a point along the slider
				var handleSlideArea = (RectTransform) _slider.handleRect.parent;
				var margin = handleSlideArea.sizeDelta.x / 2;
				var delta = new Vector3(((RectTransform) transform).sizeDelta.x / 2 + margin, 0f, 0f);
				var min = handleSlideArea.TransformPoint(-delta);
				var max = handleSlideArea.TransformPoint(delta);
				var pointOnLine = Math3D.ProjectPointOnLineSegment(min, max, _selectingHand!.m_pointingHit.point);

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

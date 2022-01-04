#pragma warning disable CS1591
using System;
using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

/// <summary>
/// Pointable monobehaviour for the log panel. This is responsible for getting the player's touchpad input
/// </summary>
public class SodaliteScrollPointable : FVRPointable
{
	private ISodaliteScrollable? _scrollable;
	private ScrollRect? _scrollRect;
	private float _lastScrollTime;

	private const float ScrollPause = 0.05f;

	private void Awake()
	{
		_scrollable = GetComponent<ISodaliteScrollable>();
		_scrollRect = GetComponent<ScrollRect>();
	}

	/// <summary>
	/// Called every frame when the player is pointing at this
	/// </summary>
	public override void OnHoverDisplay()
	{
		if (Time.time < _lastScrollTime + ScrollPause) return;
		int scroll = 0;
		foreach (var hand in PointingHands)
		{
			switch (hand.Input.TouchpadAxes.y)
			{
				case > .5f:
					scroll = 1;
					break;
				case < -.5f:
					scroll = -1;
					break;
				default:
					continue;
			}

			break;
		}

		_lastScrollTime = Time.time;

		if (_scrollRect && scroll != 0)
		{
			float contentHeight = _scrollRect!.content.sizeDelta.y;
			float contentShift = 1 * scroll * Time.deltaTime;
			_scrollRect!.verticalNormalizedPosition += contentShift / contentHeight;
		}
		_scrollable?.Scroll(scroll);
	}
}

/// <summary>
/// Interface for things which want to be able to scrolled
/// </summary>
public interface ISodaliteScrollable
{
	/// <summary>
	/// Callback for when a thing is scrolled
	/// </summary>
	/// <param name="x">The amount and direction of scroll</param>
	void Scroll(float x);
}

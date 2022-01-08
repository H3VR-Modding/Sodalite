#pragma warning disable CS1591
using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Components;

/// <summary>
///     Pointable monobehaviour for the log panel. This is responsible for getting the player's touchpad input
/// </summary>
public class SodaliteScrollPointable : FVRPointable
{
	private ISodaliteScrollable? _scrollable;
	private ScrollRect? _scrollRect;

	private void Awake()
	{
		_scrollable = GetComponent<ISodaliteScrollable>();
		_scrollRect = GetComponent<ScrollRect>();
	}

	/// <summary>
	///     Called every frame when the player is pointing at this
	/// </summary>
	public override void OnHoverDisplay()
	{
		var scroll = Vector2.zero;
		foreach (var hand in PointingHands)
		{
			if (Mathf.Approximately(hand.Input.TouchpadAxes.y, 0f)) continue;
			scroll = hand.Input.TouchpadAxes;
			break;
		}
		if (_scrollRect && scroll != Vector2.zero)
		{
			var contentHeight = _scrollRect!.content.sizeDelta.y;
			var contentShiftVertical = 500 * scroll.y * Time.deltaTime;
			float newVal = _scrollRect!.verticalNormalizedPosition + contentShiftVertical / contentHeight;
			_scrollRect!.verticalNormalizedPosition = Mathf.Clamp01(newVal);
		}

		_scrollable?.Scroll(scroll);
	}
}

/// <summary>
///     Interface for things which want to be able to scrolled
/// </summary>
public interface ISodaliteScrollable
{
	/// <summary>
	///     Callback for when a thing is scrolled
	/// </summary>
	/// <param name="x">The amount and direction of scroll</param>
	void Scroll(Vector2 x);
}

using UnityEngine;

namespace Sodalite.UiWidgets;

/// <summary>
///     Some UI extensions
/// </summary>
public static class Extensions
{
	/// <summary>
	///     Anchors this element so that it completely fills it's parent
	/// </summary>
	/// <param name="rt">RectTransform to operate on</param>
	public static void FillParent(this RectTransform rt)
	{
		//rt.localPosition = Vector3.zero;
		//rt.anchoredPosition = Vector2.zero;
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.sizeDelta = Vector2.zero;
	}
}

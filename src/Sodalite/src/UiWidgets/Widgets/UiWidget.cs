using System;
using UnityEngine;

namespace Sodalite.UiWidgets;

/// <summary>
///     The base class for all UI Widgets.
/// </summary>
public abstract class UiWidget : MonoBehaviour
{
	/// <summary>Reference to the RectTransform of this widget</summary>
	public RectTransform RectTransform = null!;

	/// <summary>
	///     The Awake method is used to configure widgets
	/// </summary>
	protected virtual void Awake()
	{
		// Make sure we're a 2D UI element
		RectTransform = gameObject.AddComponent<RectTransform>();

		// Set it upright so it looks in the proper direction
		RectTransform.localRotation = Quaternion.identity;
	}

#if DEBUG
		private void Update()
		{
			Rect rect = RectTransform.rect;
			Vector2 size = rect.size;
			Gizmos.Cube(rect.center, transform.rotation, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 5f));
		}
#endif

	/// <summary>
	///     Creates a widget on the provided game object
	/// </summary>
	/// <param name="go">The game object to create the widget on</param>
	/// <param name="configureWidget">The configuration to apply to the widget</param>
	/// <typeparam name="TWidget">The type of widget to make</typeparam>
	/// <returns>The created widget</returns>
	[Obsolete("UIWidgets have been superseded by the universal mod panel.")]
	public static TWidget CreateAndConfigureWidget<TWidget>(GameObject go, Action<TWidget> configureWidget)
		where TWidget : UiWidget
	{
		// Create the game object and parent it
		GameObject widgetGo = new(typeof(TWidget).Name);
		widgetGo.transform.SetParent(go.transform);

		// Configure the widget
		var widget = widgetGo.AddComponent<TWidget>();
		configureWidget(widget);

		// Return it
		return widget;
	}
}

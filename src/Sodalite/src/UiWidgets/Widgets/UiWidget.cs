using System;
using UnityEngine;

namespace Sodalite.UiWidgets
{
	/// <summary>
	///		The base class for all UI Widgets.
	/// </summary>
	public abstract class UiWidget : MonoBehaviour
	{
		/// <summary>Reference to the RectTransform of this widget</summary>
		public RectTransform RectTransform = null!;

		/// <summary>Reference to this widget's parent style</summary>
		protected WidgetStyle Style = null!;

		/// <summary>Reference to this widget's AudioSource, if it has one.</summary>
		protected AudioSource? AudioSource;

		/// <summary>
		///		All configuration of widgets are done in their Awake() methods.
		/// </summary>
		protected virtual void Awake()
		{
			// Make sure we're a 2D UI element
			RectTransform = gameObject.AddComponent<RectTransform>();

			// Try and find our style settings
			Style = GetComponentInParent<WidgetStyle>();
			if (!Style) throw new InvalidOperationException("Widget style not found! Are you creating your widget with the UiWidget.CreateAndConfigureWidget method?");

			// If we have an audio source somewhere in the parent we want that too
			AudioSource = GetComponentInParent<AudioSource>();
			
			// Set it upright so it looks in the proper direction
			RectTransform.localRotation = Quaternion.identity;
		}

		/// <summary>
		///		Creates a widget on the provided game object and configures it with the default style
		/// </summary>
		/// <param name="go">The game object to create the widget on</param>
		/// <param name="configureWidget">The configuration to apply to the widget</param>
		/// <typeparam name="TWidget">The type of widget to make</typeparam>
		/// <returns>The created widget</returns>
		public static TWidget CreateAndConfigureWidget<TWidget>(GameObject go, Action<TWidget> configureWidget) where TWidget : UiWidget
		{
			// Use the default style
			return CreateAndConfigureWidget(go, configureWidget, (WidgetStyle _) => { });
		}

		/// <summary>
		///		Creates a widget on the provided game object and configures it with the provided style type
		/// </summary>
		/// <param name="go">The game object to create the widget on</param>
		/// <param name="configureWidget">The configuration to apply to the widget</param>
		/// <param name="configureStyle">The configuration to apply to the widget style</param>
		/// <typeparam name="TWidget">The type of widget to make</typeparam>
		/// <typeparam name="TStyle">The type of style to apply</typeparam>
		/// <returns>The created widget</returns>
		public static TWidget CreateAndConfigureWidget<TWidget, TStyle>(GameObject go, Action<TWidget> configureWidget, Action<TStyle> configureStyle)
			where TWidget : UiWidget where TStyle : WidgetStyle
		{
			// Create the game object and parent it
			GameObject widgetGo = new(typeof(TWidget).Name);
			widgetGo.transform.SetParent(go.transform);

			// Configure the style
			TStyle style = widgetGo.AddComponent<TStyle>();
			configureStyle(style);

			// Configure the widget
			TWidget widget = widgetGo.AddComponent<TWidget>();
			configureWidget(widget);

			// Return it
			return widget;
		}
	}
}

using System;
using UnityEngine;

namespace Sodalite.UiWidgets
{
	/// <summary>
	///	Base class for widget styles. Any kind of commonly configurable element
	/// properties can be set in this style class.
	/// </summary>
	public static class WidgetStyle
	{
		// Internal references to the defaults
		internal static Font? DefaultTextFont;
		internal static Sprite? DefaultButtonSprite;

		/// <summary>
		/// The text font
		/// </summary>
		public static Font TextFont => DefaultTextFont is not null && DefaultTextFont
			? DefaultTextFont
			: throw new InvalidOperationException("Default text font was null!");

		/// <summary>
		/// The text color
		/// </summary>
		public static Color TextColor { get; } = Color.white;

		/// <summary>
		/// Sprite used for buttons
		/// </summary>
		public static Sprite ButtonSprite => DefaultButtonSprite is not null && DefaultButtonSprite
			? DefaultButtonSprite
			: throw new InvalidOperationException("Default text font was null!");

		/// <summary>
		/// The button sprite tint while it is unselected
		/// </summary>
		public static Color ButtonColorUnselected { get; } = new(27 / 255f, 73 / 255f, 155 / 255f, 160 / 255f);

		/// <summary>
		/// The button sprite tint when selected (hovered)
		/// </summary>
		public static Color ButtonColorSelected { get; } = new(192 / 255f, 202 / 255f, 222 / 255f, 216 / 255f);
	}
}

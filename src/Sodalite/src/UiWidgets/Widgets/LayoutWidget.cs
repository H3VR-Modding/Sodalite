using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.UiWidgets
{
	/// <summary>
	///		Widget that represents a layout group (e.g. GridLayoutGroup or HorizontalLayoutGroup) that can have children widgets
	/// </summary>
	/// <typeparam name="TLayout">The type of the layout group</typeparam>
	public class LayoutWidget<TLayout> : UiWidget where TLayout : LayoutGroup
	{
		// ReSharper disable once NotAccessedField.Global
		public TLayout LayoutGroup = null!;

		protected override void Awake()
		{
			base.Awake();
			LayoutGroup = gameObject.AddComponent<TLayout>();
			RectTransform.FillParent();
		}

		public void AddChild<T>(Action<T> configure) where T : UiWidget
		{
			T widget = CreateAndConfigureWidget(gameObject, configure);
			widget.RectTransform.localPosition = Vector3.zero;
			widget.RectTransform.localScale = Vector3.one;
		}
	}

	// Unity doesn't like generic mono behaviours.
	public class GridLayoutWidget : LayoutWidget<GridLayoutGroup>
	{
	}

	public class VerticalLayoutWidget : LayoutWidget<VerticalLayoutGroup>
	{
	}

	public class HorizontalLayoutWidget : LayoutWidget<HorizontalLayoutGroup>
	{
	}
}

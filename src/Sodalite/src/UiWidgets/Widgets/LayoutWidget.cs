using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.UiWidgets
{
	/// <summary>
	///	Widget that represents a layout group (e.g. GridLayoutGroup or HorizontalLayoutGroup) that can have children widgets
	/// </summary>
	/// <typeparam name="TLayout">The type of the layout group</typeparam>
	public class LayoutWidget<TLayout> : UiWidget where TLayout : LayoutGroup
	{
		/// <summary>
		/// This widget's layout group
		/// </summary>
		public TLayout LayoutGroup = null!;

		protected override void Awake()
		{
			base.Awake();
			LayoutGroup = gameObject.AddComponent<TLayout>();
			RectTransform.FillParent();
		}

		/// <summary>
		/// Creates a child widget on this layout widget
		/// </summary>
		/// <param name="configure">The configuration action for the child widget</param>
		/// <typeparam name="T">The type of the child widget</typeparam>
		public void AddChild<T>(Action<T> configure) where T : UiWidget
		{
			T widget = CreateAndConfigureWidget(gameObject, configure);
			widget.RectTransform.localPosition = Vector3.zero;
			widget.RectTransform.localScale = Vector3.one;
		}
	}

	/// <summary>
	/// Layout widget using Unity's Grid Layout Group.
	/// </summary>
	public class GridLayoutWidget : LayoutWidget<GridLayoutGroup>
	{
	}

	/// <summary>
	/// Layout widget using Unity's Vertical Layout Group.
	/// </summary>
	public class VerticalLayoutWidget : LayoutWidget<VerticalLayoutGroup>
	{
	}

	/// <summary>
	/// Layout widget using Unity's Horizontal Layout Group.
	/// </summary>
	public class HorizontalLayoutWidget : LayoutWidget<HorizontalLayoutGroup>
	{
	}
}

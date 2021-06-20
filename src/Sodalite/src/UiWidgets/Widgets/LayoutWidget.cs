using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.UiWidgets
{
	/// <summary>
	///	Widget that represents a layout group (e.g. GridLayoutGroup or HorizontalLayoutGroup) that can have children widgets
	/// </summary>
	/// <typeparam name="TLayout">The type of the layout group</typeparam>
	public abstract class LayoutWidget<TLayout> : UiWidget where TLayout : LayoutGroup
	{
		/// <summary>
		/// This widget's layout group
		/// </summary>
		public TLayout LayoutGroup = null!;

		/// <inheritdoc cref="UiWidget.Awake"/>
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
		/// <inheritdoc cref="UiWidget.Awake"/>
		protected override void Awake()
		{
			base.Awake();

			// Set the default layout rules
			LayoutGroup.spacing = Vector2.one * 4;
			LayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
			LayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
			LayoutGroup.childAlignment = TextAnchor.UpperLeft;
			LayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		}

		/// <summary>
		///	Automatically sets the grid size for a standard looking grid menu. More complex setups can be done manually.
		/// </summary>
		/// <param name="rows">The number of rows in the grid</param>
		/// <param name="columns">The number of columns in the grid</param>
		public void SetGrid(int rows, int columns)
		{
			// Calculate the cell size
			Vector2 gridSize = RectTransform.sizeDelta;
			float cellWidth = (gridSize.x - columns * LayoutGroup.spacing.x) / columns;
			float cellHeight = (gridSize.y - rows * LayoutGroup.spacing.y) / rows;

			// Apply the calculations
			LayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);
			LayoutGroup.constraintCount = columns;
		}
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

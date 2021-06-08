using UnityEngine.UI;

namespace Sodalite.UiWidgets
{
	/// <summary>
	/// UiWidget that represents just some text
	/// </summary>
	public class TextWidget : UiWidget
	{
		/// <summary>Reference to the Unity Text component of this widget</summary>
		public Text Text = null!;

		/// <summary>Initializes the widget</summary>
		protected override void Awake()
		{
			base.Awake();
			Text = gameObject.AddComponent<Text>();
			Text.font = Style.TextFont;
			Text.color = Style.TextColor;
		}
	}
}

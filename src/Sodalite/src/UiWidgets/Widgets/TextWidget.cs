using UnityEngine.UI;

namespace Sodalite.UiWidgets
{
	public class TextWidget : UiWidget
	{
		public Text Text = null!;

		protected override void Awake()
		{
			base.Awake();
			Text = gameObject.AddComponent<Text>();
			Text.font = Style.TextFont;
			Text.color = Style.TextColor;
		}
	}
}

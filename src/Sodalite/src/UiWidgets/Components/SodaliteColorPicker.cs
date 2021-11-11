using System;
using Sodalite.ModPanel;
using Sodalite.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.UiWidgets.Components
{
	public class SodaliteColorPicker : MonoBehaviour
	{
		public Image Image = null!;
		public Text Text = null!;
		public string FieldName;
		public Color CurrentColor;

		public Action<Color>? ColorChanged;

		public void Awake()
		{
			SetColor(CurrentColor);
		}

		public void OpenPicker()
		{
			UniversalModPanel.Instance.GetPageOfType<ModPanelColorPickerPage>()!.PickColor(FieldName, CurrentColor, SetColor);
		}

		private void SetColor(Color c)
		{
			Text.text = c.AsRGBA();
			Image.color = c;
			Text.color = c.maxColorComponent < 0.8f ? Color.white : Color.black;
			CurrentColor = c;
			ColorChanged?.Invoke(c);
		}
	}
}

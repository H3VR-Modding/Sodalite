using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using Sodalite.UiWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite
{
	/// <summary>
	/// Unity MonoBehaviour for controlling the BepInEx log panel
	/// </summary>
	public class BepInExLogPanel : MonoBehaviour
	{
		private List<LogEventArgs>? _currentEvents;
		private TextWidget? _logText;

		private ConfigEntry<int>? _fontSize;
		private ConfigEntry<int>? _maxLines;
		private ConfigEntry<string>? _fontName;

		private int _offset;

		private static readonly Dictionary<LogLevel, string> LogColors = new()
		{
			[LogLevel.Fatal] = "#962c2c", // Dark red
			[LogLevel.Error] = "red",
			[LogLevel.Warning] = "#e0dd10", // Slightly muted yellow
			[LogLevel.Message] = "white",
			[LogLevel.Info] = "white",
			[LogLevel.Debug] = "grey"
		};

		public void CreateWithExisting(BaseUnityPlugin source, GameObject canvas, List<LogEventArgs> currentEvents)
		{
			// Set variables and bind configs
			_currentEvents = currentEvents;
			_fontSize = source.Config.Bind("LogPanel", "FontSize", 10, "The size of the font in the log panel.");
			_maxLines = source.Config.Bind("LogPanel", "MaxLines", 30,
				"The maximum number of lines to render in the log panel at any given time. If you change some of the other settings and find that the log isn't filling the panel completely, try increasing this value.");
			_fontName = source.Config.Bind("LogPanel", "FontName", "Consolas",
				"The name of the font used on the log panel. This can be any font you have installed, but Consolas is pretty good and comes with Windows so.");

			// Place a mask around the canvas to prevent the log from overflowing
			canvas.AddComponent<RectMask2D>();

			// Create a text widget
			_logText = UiWidget.CreateAndConfigureWidget(canvas, (TextWidget widget) =>
			{
				widget.RectTransform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
				widget.RectTransform.localPosition = Vector3.zero;
				widget.RectTransform.anchoredPosition = Vector2.zero;
				widget.RectTransform.localRotation = Quaternion.identity;
				widget.RectTransform.sizeDelta = new Vector2(37f / 0.07f, 24f / 0.07f);
				widget.Text.fontSize = _fontSize.Value;
				widget.Text.alignment = TextAnchor.LowerLeft;
				widget.Text.verticalOverflow = VerticalWrapMode.Overflow;
				widget.Text.font = Font.CreateDynamicFontFromOSFont(Font.GetOSInstalledFontNames().Contains(_fontName.Value) ? _fontName.Value : "Consolas", _fontSize.Value);
			});

			// Get the pointable in behind and make it our custom one that does scrolling
			FVRPointable pointable = transform.Find("Backing").GetComponent<FVRPointable>();
			ScrollPointable scrollPointable = pointable.gameObject.AddComponent<ScrollPointable>();
			scrollPointable.MaxPointingRange = pointable.MaxPointingRange;
			scrollPointable.Panel = this;
			Destroy(pointable);

			// Lastly do an update so we're good to go!
			UpdateText();
		}

		public void Scroll(int direction)
		{
			if (_currentEvents is null || direction == 0) return;
			_offset = Mathf.Clamp(_offset + direction, 0, _currentEvents.Count - 1);
			UpdateText();
		}

		public void LogEvent()
		{
			// If the user is currently offset the scrolling, keep their position in the log
			if (_offset != 0) _offset += 1;
			UpdateText();
		}

		private void UpdateText()
		{
			if (_currentEvents is null || _maxLines is null || _logText is null) return;

			StringBuilder sb = new();
			int startIndex = Math.Max(0, _currentEvents.Count - _maxLines.Value - _offset);
			int endIndex = Math.Min(_maxLines.Value, _currentEvents.Count);
			for (int i = 0; i < endIndex - 1; i++)
			{
				LogEventArgs evt = _currentEvents[startIndex + i];
				sb.AppendLine($"<color={LogColors[evt.Level]}>{evt}</color>");
			}

			sb.Append($" -- Showing lines {startIndex + 1} to {startIndex + _maxLines.Value} (of {_currentEvents.Count})");
			_logText.Text.text = sb.ToString();
		}
	}

	public class ScrollPointable : FVRPointable
	{
		public BepInExLogPanel? Panel;
		private float _lastScrollTime;

		private const float ScrollPause = 0.05f;

		public override void OnHoverDisplay()
		{
			if (Panel is null || Time.time < _lastScrollTime + ScrollPause) return;
			int scroll = 0;
			foreach (var hand in PointingHands)
			{
				switch (hand.Input.TouchpadAxes.y)
				{
					case > .5f:
						scroll = 1;
						break;
					case < -.5f:
						scroll = -1;
						break;
					default:
						continue;
				}

				break;
			}

			_lastScrollTime = Time.time;
			Panel.Scroll(scroll);
		}
	}
}

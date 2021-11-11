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

namespace Sodalite.ModPanel
{
	/// <summary>
	/// Unity MonoBehaviour for controlling the BepInEx log panel
	/// </summary>
	public sealed class UniversalModPanelLogPage : UniversalModPanelPage
	{
		internal List<LogEventArgs>? CurrentEvents;
		[SerializeField] private Text Log = null!;
		[SerializeField] private int MaxLines;
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

		internal void Scroll(int direction)
		{
			if (CurrentEvents is null || direction == 0) return;
			_offset = Mathf.Clamp(_offset + direction, 0, CurrentEvents.Count - 1);
			UpdateText();
		}

		internal void LogEvent()
		{
			// If the user is currently offset the scrolling, keep their position in the log
			if (_offset != 0) _offset += 1;
			UpdateText();
		}

		internal void UpdateText()
		{
			if (CurrentEvents is null) return;

			StringBuilder sb = new();
			int startIndex = Math.Max(0, CurrentEvents.Count - MaxLines - _offset);
			int endIndex = Math.Min(MaxLines, CurrentEvents.Count);
			for (int i = 0; i < endIndex - 1; i++)
			{
				LogEventArgs evt = CurrentEvents[startIndex + i];
				sb.AppendLine($"<color={LogColors[evt.Level]}>{evt}</color>");
			}

			sb.Append($" -- Showing lines {startIndex + 1} to {startIndex + MaxLines} (of {CurrentEvents.Count})");
			Log.text = sb.ToString();
		}
	}

	/// <summary>
	/// Pointable monobehaviour for the log panel. This is responsible for getting the player's touchpad input
	/// </summary>
	public class ScrollPointable : FVRPointable
	{
		[SerializeField]
		private UniversalModPanelLogPage Panel = null!;
		private float _lastScrollTime;

		private const float ScrollPause = 0.05f;

		/// <summary>
		/// Called every frame when the player is pointing at this
		/// </summary>
		public override void OnHoverDisplay()
		{
			if (Time.time < _lastScrollTime + ScrollPause) return;
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

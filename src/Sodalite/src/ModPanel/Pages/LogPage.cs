#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using Sodalite.ModPanel.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel.Pages;

public sealed class UniversalModPanelLogPage : UniversalModPanelPage, ISodaliteScrollable
{
	private static readonly Dictionary<LogLevel, string> LogColors = new()
	{
		[LogLevel.Fatal] = "#962c2c", // Dark red
		[LogLevel.Error] = "red",
		[LogLevel.Warning] = "#e0dd10", // Slightly muted yellow
		[LogLevel.Message] = "white",
		[LogLevel.Info] = "white",
		[LogLevel.Debug] = "grey"
	};
	[SerializeField] private Text Log = null!;
	[SerializeField] private int MaxLines;
	private int _offset;

	private const float ScrollPause = 0.05f;
	private float _lastScrollTime;

	public void Scroll(Vector2 x)
	{
		if (Time.time < _lastScrollTime + ScrollPause) return;
		_lastScrollTime = Time.time;
		var direction = x.y == 0f ? 0 : x.y > 0 ? 1 : -1;
		if (direction == 0) return;
		_offset = Mathf.Clamp(_offset + direction, 0, Sodalite.LogEvents.Count - 1);
		UpdateText(false);
	}

	internal void LogEvent(LogEventArgs evt)
	{
		// If the user is currently offset the scrolling, keep their position in the log
		if (_offset != 0) _offset += Sodalite.LogEventLineCount![evt];
		UpdateText(false);
	}

	internal void UpdateText(bool evenIfDisabled)
	{
		if (!gameObject.activeSelf && !evenIfDisabled) return;

		StringBuilder sb = new();

		int count = 0;
		int toSkip = _offset;
		for (int i = 0; count < MaxLines; i++)
		{
			// Make sure we're not hitting the start of the list
			int idx = Sodalite.LogEvents.Count - (1 + i);
			if (idx < 0) break;

			// Get the ith line from the bottom, append it to the string, and count its lines
			var evt = Sodalite.LogEvents[idx];

			// Check if we're skipping this one
			int lineCount = Sodalite.LogEventLineCount![evt];
			if (lineCount <= toSkip)
			{
				toSkip -= lineCount;
				continue;
			}

			string[] lines = evt.ToString().Split('\n');
			for (var j = lines.Length - 1; j >= 0; j--)
			{
				// If there's still lines left to skip, do that.
				if (toSkip > 0)
				{
					toSkip--;
					continue;
				}

				// Append our line
				var line = lines[j];
				sb.Insert(0, $"<color={LogColors[evt.Level]}>{line}</color>\n");
				count++;
			}
		}

		// Remove the trailing newline and set the text
		sb.Remove(sb.Length - 1, 1);
		Log.text = sb.ToString();
	}
}

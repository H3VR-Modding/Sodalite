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
	internal List<LogEventArgs>? CurrentEvents;

	public void Scroll(float x)
	{
		var direction = x == 0f ? 0 : x > 0 ? 1 : -1;
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
		var startIndex = Math.Max(0, CurrentEvents.Count - MaxLines - _offset);
		var endIndex = Math.Min(MaxLines, CurrentEvents.Count);
		for (var i = 0; i < endIndex - 1; i++)
		{
			var evt = CurrentEvents[startIndex + i];
			sb.AppendLine($"<color={LogColors[evt.Level]}>{evt}</color>");
		}

		sb.Append($" -- Showing lines {startIndex + 1} to {startIndex + MaxLines} (of {CurrentEvents.Count})");
		Log.text = sb.ToString();
	}
}

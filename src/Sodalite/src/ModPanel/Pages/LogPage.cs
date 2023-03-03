#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
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
	[SerializeField] private int MaxLines = 0;

	[SerializeField] private SodaliteToggleButton DebugToggle = null!;
	[SerializeField] private SodaliteToggleButton InfoToggle = null!;
	[SerializeField] private SodaliteToggleButton WarningToggle = null!;
	[SerializeField] private SodaliteToggleButton ErrorToggle = null!;

	private int _offset;
	private float _scrollValue;
	private readonly List<LogEventArgs> _cache = new();

	private void Start()
	{
		// Setup the initial state of the toggle buttons
		var curFilter = Sodalite.Config.LogPanelFilter.Value;
		DebugToggle.SetState((curFilter & LogLevel.Debug) != 0);
		WarningToggle.SetState((curFilter & LogLevel.Warning) != 0);
		ErrorToggle.SetState((curFilter & LogLevel.Error) != 0);
		InfoToggle.SetState((curFilter & (LogLevel.Info | LogLevel.Message)) != 0);

		// Register a whole bunch of listeners for the buttons so we can toggle the log panel filter setting
		DebugToggle.OnValueChanged += b => SetOrClearFlag(b, LogLevel.Debug);
		WarningToggle.OnValueChanged += b => SetOrClearFlag(b, LogLevel.Warning);
		ErrorToggle.OnValueChanged += b => SetOrClearFlag(b, LogLevel.Error);
		InfoToggle.OnValueChanged += b => SetOrClearFlag(b, LogLevel.Info | LogLevel.Message);

		// Then register a listener for that config so we can update the panel when needed
		Sodalite.Config.LogPanelFilter.SettingChanged += (_, _) => RebuildFilterCache();

		// Finally do a rebuild and redraw all text
		RebuildFilterCache();
		UpdateText(true);

		// Function used above to set or clear the log level flags
		static void SetOrClearFlag(bool b, LogLevel flags)
		{
			if (b) Sodalite.Config.LogPanelFilter.Value |= flags;
			else Sodalite.Config.LogPanelFilter.Value &= ~flags;
		}
	}

	public void Scroll(Vector2 x)
	{
		// Scroll up to 25 lines per second
		_scrollValue += x.y * Time.deltaTime * 25;

		// Wait until we're scrolling at least one line
		if (Mathf.Abs(_scrollValue) < 1) return;

		// Clamp the offset so we can't scroll past the first or last lines
		_offset = Mathf.Clamp(_offset + (int) _scrollValue, 0, Mathf.Max(0, _cache.Count - (MaxLines - 1)));

		// Update text then move our scroll value counter one towards zero
		UpdateText(false);
		_scrollValue += _scrollValue > 0 ? -1 : 1;
	}

	internal void LogEvent(LogEventArgs evt)
	{
		// If the user is currently offset the scrolling, keep their position in the log
		if (_offset != 0) _offset += Sodalite.LogEventLineCount[evt];

		// If this message matches our current filter, add it to our cache immediately
		if ((evt.Level & Sodalite.Config.LogPanelFilter.Value) != 0)
		{
			_cache.Add(evt);
		}

		UpdateText(false);
	}

	public void RebuildFilterCache()
	{
		// Clear our current offset and cache, we can't keep it when changing the messages
		_offset = 0;
		_cache.Clear();

		// Loop over all the events, adding them to the cache if they match
		foreach (var evt in Sodalite.LogEvents)
		{
			// If this message matches our current filter, add it to our cache immediately
			if ((evt.Level & Sodalite.Config.LogPanelFilter.Value) != 0)
			{
				_cache.Add(evt);
			}
		}

		// Force a redraw
		UpdateText(true);
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
			int idx = _cache.Count - (1 + i);
			if (idx < 0) break;

			// Get the ith line from the bottom, append it to the string, and count its lines
			var evt = _cache[idx];

			// Check if we're skipping this one
			int lineCount = Sodalite.LogEventLineCount[evt];
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
		if (sb.Length > 0) sb.Remove(sb.Length - 1, 1);
		Log.text = sb.ToString();
	}
}

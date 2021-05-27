using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil;

namespace Sodalite.Patcher
{
	/// <summary>
	///		Patcher class for Sodalite. We don't actually want to patch anything, we just want to get an early handle on log entries
	/// </summary>
	internal static class SodalitePatcher
	{
		public static IEnumerable<string> TargetDLLs { get; } = new string[0];
		internal static LogBuffer LogBuffer { get; } = new();

		// ReSharper disable once UnusedParameter.Global
		public static void Patch(AssemblyDefinition assembly)
		{
		}
	}

	/// <summary>
	///		Small class to buffer logs output in the patching stage so we can read them later in the runtime stage
	/// </summary>
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class LogBuffer : ILogListener
	{
		public LogBuffer()
		{
			Logger.Listeners.Add(this);
		}

		public void Dispose()
		{
			Logger.Listeners.Remove(this);
		}

		// Capture log events
		internal readonly List<LogEventArgs> LogEvents = new();

		public void LogEvent(object sender, LogEventArgs eventArgs)
		{
			LogEvents.Add(eventArgs);
		}
	}
}

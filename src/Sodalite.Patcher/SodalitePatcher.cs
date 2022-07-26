using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Sodalite.Patcher;

/// <summary>
///		Patcher class for Sodalite. We don't actually want to patch anything, we just want to get an early handle on log entries
/// </summary>
internal static class SodalitePatcher
{
	// ReSharper disable once UnusedMember.Global
	public static IEnumerable<string> TargetDLLs => new string[0];
	internal static LogBuffer LogBuffer { get; set; } = new();

	private delegate ulong getSteamIDDelegate();

	private static ulong SessionId { get; set; }

	static SodalitePatcher()
	{
		// Generate a theoretically valid Steam ID following the format from Valve's documentation
		// https://developer.valvesoftware.com/wiki/SteamID

		// The lower 32 bits of the ID represents the account number
		byte[] buf = new byte[4];
		new Random().NextBytes(buf);
		uint rand = BitConverter.ToUInt32(buf, 0);

		// The upper 32 bits are always the same for individual accounts so
		// combine the random low 32 bits with this magic number and bam, valid Steam ID.
		SessionId = rand | 76_561_197_960_265_728ul;

		// Apply hooks for plugin types serialization
		FixPluginTypesSerialization.ApplyHooks();
	}

	internal static void CheckSpoofSteamUserID()
	{
		try
		{
			// Get the firstpass assembly. That's where the steamworks stuff resides.
			Assembly? asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "Assembly-CSharp-firstpass");
			if (asm is null) return;

			// Call the init function. This loads the native DLL since it is lazy loaded
			Type c = asm.GetType("Steamworks.NativeMethods");
			c.GetMethod("SteamAPI_Init")!.Invoke(null, new object[0]);

			// Get the pointers to the native code function
			IntPtr steamworks = DynDll.OpenLibrary("CSteamworks.dll");
			IntPtr getUserIdFunction = steamworks.GetFunction("ISteamUser_GetSteamID");

			// Check if we want to spoof or not by trying to find the config value in the file
			bool applyHook = false;
			string configPath = Path.Combine(Paths.ConfigPath, "nrgill28.Sodalite.cfg");
			if (File.Exists(configPath))
				applyHook = File.ReadAllLines(configPath)
					.Any(line => line.Contains("SpoofSteamUserID = true"));

			if (applyHook)
			{
				// Apply the hook
				IntPtr hook = Marshal.GetFunctionPointerForDelegate(GetSteamIDRandomized);
				var detour = new NativeDetour(getUserIdFunction, hook, new NativeDetourConfig {ManualApply = true});
				detour.Apply();
			}
			else
			{
				// If we don't want to apply the hook, set our sessionId to match
				var original = (getSteamIDDelegate) Marshal.GetDelegateForFunctionPointer(getUserIdFunction, typeof(getSteamIDDelegate));
				SessionId = original();
			}
		}
		catch (Exception)
		{
			// Ignored.
		}
	}

	private static ulong GetSteamIDRandomized() => SessionId;

	// ReSharper disable once UnusedMember.Global
	// ReSharper disable once UnusedParameter.Global
	public static void Patch(AssemblyDefinition assembly)
	{
	}
}

/// <summary>
///	Small class to buffer logs output in the patching stage so we can read them later in the runtime stage
/// </summary>
internal class LogBuffer : ILogListener
{
	// Capture log events
	internal readonly List<LogEventArgs> LogEvents = new();

	public LogBuffer()
	{
		Logger.Listeners.Add(this);
	}

	public void Dispose()
	{
		Logger.Listeners.Remove(this);
	}

	public void LogEvent(object sender, LogEventArgs eventArgs)
	{
		LogEvents.Add(eventArgs);

		// Wait just before BepInEx starts loading plugins to apply our SteamID spoofing patch
		if (eventArgs.Data is "Chainloader started") SodalitePatcher.CheckSpoofSteamUserID();
	}
}

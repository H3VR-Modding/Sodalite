using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using MonoMod.RuntimeDetour;
using Sodalite.Api;
using Sodalite.ModPanel;
using Sodalite.Patcher;
using Sodalite.UiWidgets;
using Sodalite.Utilities;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace Sodalite;

/// <summary>
///     Constant variables pertaining to Sodalite
/// </summary>
public static class SodaliteConstants
{
	/// <summary>
	///     BepInEx GUID of Sodalite
	/// </summary>
	public const string Guid = "nrgill28.Sodalite";

	/// <summary>
	///     BepInEx Name of Sodalite
	/// </summary>
	public const string Name = "Sodalite";

	/// <summary>
	///     Version of Sodalite
	/// </summary>
	public const string Version = ThisAssembly.Git.BaseVersion.Major + "." + ThisAssembly.Git.BaseVersion.Minor + "." + ThisAssembly.Git.BaseVersion.Patch;
}

/// <summary>
///     Sodalite main BepInEx plugin entrypoint
/// </summary>
[BepInPlugin(SodaliteConstants.Guid, SodaliteConstants.Name, SodaliteConstants.Version)]
[BepInProcess("h3vr.exe")]
public class Sodalite : BaseUnityPlugin, ILogListener
{
	private static ManualLogSource? _logger;

	// Static stuff
	private static readonly string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
	// Private fields
	internal static List<LogEventArgs> LogEvents = null!;
	internal static Dictionary<LogEventArgs, int> LogEventLineCount = null!;
	private LockablePanel _modPanel = null!;
	private UniversalModPanel? _modPanelComponent;
	private GameObject? _modPanelPrefab;

	// Config entries
	private ConfigEntry<bool> _autoRegisterConfigs = null!;
	private ConfigEntry<bool> _spoofSteamUserId = null!;

	// ReSharper disable once UnusedMember.Global
	internal static ManualLogSource StaticLogger => _logger ?? throw new InvalidOperationException("Cannot get logger before the behaviour is initialized!");

	/// <summary>
	///     Initialization code for Sodalite
	/// </summary>
	private void Awake()
	{
		// Register config values
		_autoRegisterConfigs = Config.Bind("General", "AutoRegisterConfigs", false, "Enable this to see config pages for mods which haven't explicitly added them.");
		_spoofSteamUserId = Config.Bind("Privacy", "SpoofSteamUserID", false, "Randomizes your Steam User ID on every startup (requires restart)");

		_autoRegisterConfigs.SettingChanged += (sender, args) =>
		{
			if (_autoRegisterConfigs.Value) UniversalModPanel.RegisterUnregisteredPluginConfigs();
		};
		UniversalModPanel.RegisterPluginSettings(Info, Config);

		// Hook a call to a compiler-generated method and replace it with one that doesn't use an unsafe GetTypes call
		// ReSharper disable once ObjectCreationAsStatement
		new Hook(
			typeof(PostProcessManager).GetMethod("<ReloadBaseTypes>m__0", BindingFlags.Static | BindingFlags.NonPublic),
			GetType().GetMethod(nameof(EnumerateTypesSafe), BindingFlags.Static | BindingFlags.NonPublic)
		);

		// Set our logger so it's accessible from anywhere
		_logger = Logger;

		// Register ourselves as the new log listener and try to grab what's already been captured
		BepInEx.Logging.Logger.Listeners.Add(this);

		// Setup the rest of the in-game log page
		LogEvents = SodalitePatcher.LogBuffer.LogEvents;
		LogEventLineCount = new Dictionary<LogEventArgs, int>();
		foreach (var evt in LogEvents)
		{
			LogEventLineCount[evt] = evt.ToString().CountLines();
		}
		SodalitePatcher.LogBuffer.Dispose();

		// Load our prefab. JUST in case I forget to bundle the bundle again.
		var bundle = AssetBundle.LoadFromFile(Path.Combine(BasePath, "universalpanel"));
		if (bundle)
		{
			_modPanelPrefab = bundle.LoadAsset<GameObject>("Universal Mod Panel");

			// Make a new LockablePanel for the console panel
			_modPanel = new LockablePanel();
			_modPanel.Configure += ConfigureModPanel;
			_modPanel.TextureOverride = SodaliteUtils.LoadTextureFromBytes(Assembly.GetExecutingAssembly().GetResource("LogPanel.png"));
			WristMenuAPI.Buttons.Add(new WristMenuButton("Spawn Mod Panel", int.MaxValue, SpawnModPanel));
		}
		else
		{
			Logger.LogError("Guess who forgot to bundle the universal panel in with the release again!!!");
		}

		// Try to log the game's build id. This can be useful for debugging but only works if the game is launched via Steam.
		// The game _usually_ is launched via Steam, even with r2mm, so this may only error if someone tries to launch the game via the exe directly.
		try
		{
			SteamAPI.Init();
			var beta = SteamApps.GetCurrentBetaName(out var betaName, 128);
			GameAPI.BetaName = beta ? betaName : string.Empty;
			GameAPI.BuildId = SteamApps.GetAppBuildId();
			Logger.LogMessage($"Game build ID: {GameAPI.BuildId} ({(beta ? betaName : "main")}).");
		}
		catch (InvalidOperationException)
		{
			Logger.LogWarning("Game build ID unknown: unable to initialize Steamworks.");
		}
	}

	private void Start()
	{
		// Try to set the game running modded. This will fail on versions below Update 100 Alpha 7
		GM.SetRunningModded();

		// If we want to auto-register configs, do that now.
		if (_autoRegisterConfigs.Value) UniversalModPanel.RegisterUnregisteredPluginConfigs();

		// Pull the button sprite and font for our use later
		var button = GameObject.Find("MainMenuSceneProtoBase/LevelLoadScreen/LevelLoadHolder/Canvas/Button").transform;
		WidgetStyle.DefaultButtonSprite = button.GetComponent<Image>().sprite;
		WidgetStyle.DefaultTextFont = button.GetChild(0).GetComponent<Text>().font;
	}

	// Hook over the lambda that PostProcessManager.ReloadBaseTypes uses so we can use a GetTypesSafe instead of GetTypes
	private static IEnumerable<Type> EnumerateTypesSafe(Assembly assembly)
	{
		return from t in assembly.GetTypesSafe()
			where t.IsSubclassOf(typeof(PostProcessEffectSettings)) && t.IsDefined(typeof(PostProcessAttribute), false)
			select t;
	}

	#region Log Panel Stuffs

	// Wrist menu button callback. Gets our panel instance and makes the hand retrieve it.
	private void SpawnModPanel(object sender, ButtonClickEventArgs args)
	{
		var wristMenu = WristMenuAPI.Instance;
		if (!wristMenu) return;
		var panel = _modPanel.GetOrCreatePanel();
		args.Hand.OtherHand.RetrieveObject(panel.GetComponent<FVRPhysicalObject>());
	}

	private void ConfigureModPanel(GameObject panel)
	{
		var canvasTransform = panel.transform.Find("OptionsCanvas_0_Main/Canvas");
		_modPanelComponent = Instantiate(_modPanelPrefab, canvasTransform.position, canvasTransform.rotation, canvasTransform.parent)!.GetComponent<UniversalModPanel>();
		_modPanelComponent.LogPage.UpdateText(true);
		Destroy(canvasTransform.gameObject);
	}

	void ILogListener.LogEvent(object sender, LogEventArgs eventArgs)
	{
		LogEvents!.Add(eventArgs);
		LogEventLineCount!.Add(eventArgs, eventArgs.ToString().CountLines());
		if (_modPanelComponent) _modPanelComponent!.LogPage.LogEvent(eventArgs);
	}

	void IDisposable.Dispose()
	{
		BepInEx.Logging.Logger.Listeners.Remove(this);
		LogEvents.Clear();
	}

	#endregion
}

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
	private List<LogEventArgs> _logEvents = null!;
	private LockablePanel _modPanel = null!;
	private UniversalModPanel? _modPanelComponent;
	private GameObject? _modPanelPrefab;

	// ReSharper disable once UnusedMember.Global
	internal static ManualLogSource StaticLogger => _logger ?? throw new InvalidOperationException("Cannot get logger before the behaviour is initialized!");

	/// <summary>
	///     Initialization code for Sodalite
	/// </summary>
	private void Awake()
	{
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
		_logEvents = SodalitePatcher.LogBuffer.LogEvents;
		SodalitePatcher.LogBuffer.Dispose();

		// Load our prefab
		var bundle = AssetBundle.LoadFromFile(Path.Combine(BasePath, "universalpanel"));
		_modPanelPrefab = bundle.LoadAsset<GameObject>("Universal Mod Panel");

		// Make a new LockablePanel for the console panel
		_modPanel = new LockablePanel();
		_modPanel.Configure += ConfigureModPanel;
		_modPanel.TextureOverride = SodaliteUtils.LoadTextureFromBytes(Assembly.GetExecutingAssembly().GetResource("LogPanel.png"));
		WristMenuAPI.Buttons.Add(new WristMenuButton("Spawn Mod Panel", int.MaxValue, SpawnModPanel));

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

		// TODO: TEMP FIELDS, REMOVE BEFORE RELEASE
		var boolField = Config.Bind("Basic Fields", "Boolean toggle", false, "Description for a the boolean config value.");
		var numField = Config.Bind("Basic Fields", "Raw Number", 10, "There was no specified acceptable range for this input, so it has a raw edit field.");
		var colorField = Config.Bind("Basic Fields", "Color Picker", new Color(0.5f, 0.8f, 0.2f, 1f), "Pick a color using the color pick page of the panel.");
		var enumField = Config.Bind("List Input Fields", "Enum Field", LogLevel.Debug, "Pick between any of the BepInEx log enum values.");
		var stringListField = Config.Bind("List Input Fields", "String List", "Foo", new ConfigDescription("Pick from one of three acceptable strings with the list picker.", new AcceptableValueList<string>("Foo", "Bar", "Biz")));
		var intListField = Config.Bind("List Input Fields", "Integer List", 1, new ConfigDescription("Pick from one of four acceptable integer values with the list picker.", new AcceptableValueList<int>(1, 2, 4, 8)));
		var intRangeField = Config.Bind("Range Input Fields", "Integer Range", 0, new ConfigDescription("Select a value between the acceptable range -10 to 10", new AcceptableValueRange<int>(-10, 10)));

		UniversalModPanel.RegisterPluginSettings(Info, boolField, numField, colorField, enumField, stringListField, intListField, intRangeField);
	}

	private void Start()
	{
		// Try to set the game running modded. This will fail on versions below Update 100 Alpha 7
		GM.SetRunningModded();

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
		_modPanelComponent.LogPage.CurrentEvents = _logEvents;
		_modPanelComponent.LogPage.UpdateText();
		Destroy(canvasTransform.gameObject);
	}

	void ILogListener.LogEvent(object sender, LogEventArgs eventArgs)
	{
		_logEvents.Add(eventArgs);
		if (_modPanelComponent) _modPanelComponent!.LogPage.LogEvent();
	}

	void IDisposable.Dispose()
	{
		BepInEx.Logging.Logger.Listeners.Remove(this);
		_logEvents.Clear();
	}

	#endregion
}

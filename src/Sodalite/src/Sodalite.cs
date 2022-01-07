using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
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

namespace Sodalite
{
	/// <summary>
	/// Constant variables pertaining to Sodalite
	/// </summary>
	public static class SodaliteConstants
	{
		/// <summary>
		/// BepInEx GUID of Sodalite
		/// </summary>
		public const string Guid = "nrgill28.Sodalite";

		/// <summary>
		/// BepInEx Name of Sodalite
		/// </summary>
		public const string Name = "Sodalite";

		/// <summary>
		/// Version of Sodalite
		/// </summary>
		public const string Version = ThisAssembly.Git.BaseVersion.Major + "." + ThisAssembly.Git.BaseVersion.Minor + "." + ThisAssembly.Git.BaseVersion.Patch;
	}

	/// <summary>
	/// Sodalite main BepInEx plugin entrypoint
	/// </summary>
	[BepInPlugin(SodaliteConstants.Guid, SodaliteConstants.Name, SodaliteConstants.Version)]
	[BepInProcess("h3vr.exe")]
	public class Sodalite : BaseUnityPlugin, ILogListener
	{
		// Private fields
		private List<LogEventArgs> _logEvents = null!;
		private LockablePanel _modPanel = null!;
		private GameObject? _modPanelPrefab;
		private UniversalModPanel? _modPanelComponent;
		private static ManualLogSource? _logger;

		// Static stuff
		private static readonly string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
		internal static ManualLogSource StaticLogger => _logger ?? throw new InvalidOperationException("Cannot get logger before the behaviour is initialized!");

		/// <summary>
		/// Initialization code for Sodalite
		/// </summary>
		private void Awake()
		{
			// Hook a call to a compiler-generated method and replace it with one that doesn't use an unsafe GetTypes call
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
			AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(BasePath, "universalpanel"));
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
				bool beta = SteamApps.GetCurrentBetaName(out string betaName, 128);
				GameAPI.BetaName = beta ? betaName : string.Empty;
				GameAPI.BuildId = SteamApps.GetAppBuildId();
				Logger.LogMessage($"Game build ID: {GameAPI.BuildId} ({(beta ? betaName : "main")}).");
			}
			catch (InvalidOperationException)
			{
				Logger.LogWarning("Game build ID unknown: unable to initialize Steamworks.");
			}

			var enumField = Config.Bind("Main", "Log Verbosity", LogLevel.Debug, "Choose the types of messages to see in the BepInEx Log");
			var colorField = Config.Bind("Main", "Random Color", new Color(0.5f, 0.8f, 0.2f, 1f), "This is a test color field :)");
			var numField = Config.Bind("Main", "Random Number", 10, "This is a series of numbers. Yeah!");
			var boolField = Config.Bind("Advanced", "Enable Awesome Mode", false, "This enables the awesome mode for Sodalite. Not recommended as it may be too powerful for you.");

			UniversalModPanel.RegisterPluginSettings(Info, enumField, colorField, boolField);
		}

		private void Start()
		{
			// Try to set the game running modded. This will fail on versions below Update 100 Alpha 7
			GM.SetRunningModded();

			// Pull the button sprite and font for our use later
			Transform button = GameObject.Find("MainMenuSceneProtoBase/LevelLoadScreen/LevelLoadHolder/Canvas/Button").transform;
			WidgetStyle.DefaultButtonSprite = button.GetComponent<Image>().sprite;
			WidgetStyle.DefaultTextFont = button.GetChild(0).GetComponent<Text>().font;
		}

		#region Log Panel Stuffs

		// Wrist menu button callback. Gets our panel instance and makes the hand retrieve it.
		private void SpawnModPanel(object sender, ButtonClickEventArgs args)
		{
			FVRWristMenu? wristMenu = WristMenuAPI.Instance;
			if (!wristMenu) return;
			GameObject panel = _modPanel.GetOrCreatePanel();
			args.Hand.OtherHand.RetrieveObject(panel.GetComponent<FVRPhysicalObject>());
		}

		private void ConfigureModPanel(GameObject panel)
		{
			Transform canvasTransform = panel.transform.Find("OptionsCanvas_0_Main/Canvas");
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

		// Hook over the lambda that PostProcessManager.ReloadBaseTypes uses so we can use a GetTypesSafe instead of GetTypes
		private static IEnumerable<Type> EnumerateTypesSafe(Assembly assembly)
		{
			return from t in assembly.GetTypesSafe()
				where t.IsSubclassOf(typeof(PostProcessEffectSettings)) && t.IsDefined(typeof(PostProcessAttribute), false)
				select t;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using FistVR;
using MonoMod.RuntimeDetour;
using Sodalite.Api;
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

#if RUNTIME
	/// <summary>
	/// Sodalite main BepInEx plugin entrypoint
	/// </summary>
	[BepInPlugin(SodaliteConstants.Guid, SodaliteConstants.Name, SodaliteConstants.Version)]
	[BepInProcess("h3vr.exe")]
	public class Sodalite : BaseUnityPlugin, ILogListener
	{
		// Private fields
		private List<LogEventArgs> _logEvents = null!;
		private LockablePanel _logPanel = null!;
		private BepInExLogPanel? _logPanelComponent;
		private static ManualLogSource? _logger;

		// Static stuff
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

			// Make a new LockablePanel for the console panel
			_logPanel = new LockablePanel();
			_logPanel.Configure += ConfigureLogPanel;
			_logPanel.TextureOverride = SodaliteUtils.LoadTextureFromBytes(Assembly.GetExecutingAssembly().GetResource("LogPanel.png"));
			WristMenuAPI.Buttons.Add(new WristMenuButton("Spawn Log Panel", int.MaxValue, SpawnLogPanel));

			// Try to log the game's build id. This can be useful for debugging but only works if the game is launched via Steam.
			// The game _usually_ is launched via Steam, even with r2mm, so this may only error if someone tries to launch the game via the exe directly.
			try
			{
				SteamAPI.Init();
				Logger.LogMessage($"Game build ID: {SteamApps.GetAppBuildId()}.");
			}
			catch (InvalidOperationException)
			{
				Logger.LogWarning("Game build ID unknown: unable to initialize Steamworks.");
			}
		}

		private void Start()
		{
			// Pull the button sprite and font for our use later
			Transform button = GameObject.Find("MainMenuSceneProtoBase/LevelLoadScreen/LevelLoadHolder/Canvas/Button").transform;
			WidgetStyle.DefaultButtonSprite = button.GetComponent<Image>().sprite;
			WidgetStyle.DefaultTextFont = button.GetChild(0).GetComponent<Text>().font;
		}

		#region Utility Panel (Widgets test)

		/*
		private static void ConfigureUtilityPanel(GameObject panel)
		{
			GameObject canvas = panel.transform.Find("OptionsCanvas_0_Main/Canvas").gameObject;
			UiWidget.CreateAndConfigureWidget(canvas, (GridLayoutWidget widget) =>
			{
				// Fill our parent and set pivot to top middle
				widget.RectTransform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
				widget.RectTransform.localPosition = Vector3.zero;
				widget.RectTransform.anchoredPosition = Vector2.zero;
				widget.RectTransform.sizeDelta = new Vector2(37f / 0.07f, 24f / 0.07f);
				widget.RectTransform.pivot = new Vector2(0.5f, 1f);

				// Adjust our grid settings
				widget.LayoutGroup.cellSize = new Vector2(171, 50);
				widget.LayoutGroup.spacing = Vector2.one * 4;
				widget.LayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
				widget.LayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
				widget.LayoutGroup.childAlignment = TextAnchor.UpperCenter;
				widget.LayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				widget.LayoutGroup.constraintCount = 3;

				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Button 1!");
				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Mod Configs");
				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Another button");
				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Another button 2");
				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Another button 3");
			});
		}

		private void SpawnUtilityPanel()
		{
			if (_api.WristMenu is null || !_api.WristMenu) return;
			GameObject panel = _utilityPanel.GetOrCreatePanel();
			_api.WristMenu.m_currentHand.RetrieveObject(panel.GetComponent<FVRPhysicalObject>());
		}
		*/

		#endregion

		#region Log Panel Stuffs

		// Wrist menu button callback. Gets our panel instance and makes the hand retrieve it.
		private void SpawnLogPanel(object sender, ButtonClickEventArgs args)
		{
			FVRWristMenu? wristMenu = WristMenuAPI.Instance;
			if (wristMenu is null || !wristMenu) return;
			GameObject panel = _logPanel.GetOrCreatePanel();
			wristMenu.m_currentHand.RetrieveObject(panel.GetComponent<FVRPhysicalObject>());
		}

		private void ConfigureLogPanel(GameObject panel)
		{
			Transform canvasTransform = panel.transform.Find("OptionsCanvas_0_Main/Canvas");
			_logPanelComponent = panel.AddComponent<BepInExLogPanel>();
			_logPanelComponent.CreateWithExisting(this, canvasTransform.gameObject, _logEvents);
		}

		void ILogListener.LogEvent(object sender, LogEventArgs eventArgs)
		{
			_logEvents.Add(eventArgs);
			if (_logPanelComponent && _logPanelComponent is not null) _logPanelComponent.LogEvent();
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
#endif
}

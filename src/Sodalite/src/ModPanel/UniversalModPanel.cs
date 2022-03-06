using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Sodalite.ModPanel.Pages;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable UnusedMember.Global

namespace Sodalite.ModPanel;

/// <summary>
///     Base class for creating a universal mod panel page
/// </summary>
public class UniversalModPanelPage : MonoBehaviour
{
	/// <summary>
	///     Reference to the panel
	/// </summary>
	public UniversalModPanel Panel = null!;

	/// <summary>
	///     Display name for this page used in the breadcrumb
	/// </summary>
	public string DisplayName = null!;
}

/// <summary>
///     Mod panel behaviour
/// </summary>
public class UniversalModPanel : MonoBehaviour
{
	#region Fields

	[Header("References")]
	[SerializeField] private Button HomeButton = null!;
	[SerializeField] private RectTransform PagesRoot = null!;
	[SerializeField] private Button BackButton = null!;
	[SerializeField] private RectTransform HomePage = null!;
	[SerializeField] private GameObject HomeScreenButtonPrefab = null!;
	[SerializeField] private Button PaginatePreviousButton = null!;
	[SerializeField] private Button PaginateNextButton = null!;
	[SerializeField] private Text PaginateText = null!;
	[SerializeField] private Text Breadcrumb = null!;
	[SerializeField] private List<UniversalModPanelPage> SerializedPages = null!;
	[SerializeField] internal UniversalModPanelLogPage LogPage = null!;

	internal static readonly Dictionary<PluginInfo, ConfigEntryBase[]> RegisteredConfigs = new();
	internal static readonly Dictionary<ConfigFieldBase, Func<ConfigEntryBase, bool>> RegisteredInputFields = new();
	private static readonly Dictionary<string, UniversalModPanelPage> RegisteredCustomPages = new();
	private static readonly Dictionary<string, string> CustomHomepageButtons = new();

	private readonly Dictionary<string, UniversalModPanelPage> _pages = new();
	private readonly Stack<UniversalModPanelPage> _stack = new();
	private UniversalModPanelPage _currentPage = null!;

	/// <summary>
	///     Instance of the mod panel, for people to access.
	/// </summary>
	public static UniversalModPanel Instance { get; private set; } = null!;

	#endregion

	#region MonoBehavior Methods

	private void Awake()
	{
		Instance = this;
		// Register and pages included in the prefab
		foreach (var page in SerializedPages)
		{
			_pages.Add(page.gameObject.name, page);
			page.gameObject.SetActive(false);
		}

		// Instantiate and add any custom pages
		foreach (var entry in RegisteredCustomPages)
		{
			var instance = Instantiate(entry.Value, PagesRoot.position, PagesRoot.rotation, PagesRoot);
			_pages.Add(entry.Key, instance);
			instance.Panel = this;
			instance.gameObject.SetActive(false);
		}

		foreach (var button in CustomHomepageButtons)
		{
			var b = Instantiate(HomeScreenButtonPrefab, HomePage);
			b.GetComponentInChildren<Text>().text = button.Key;
			b.GetComponent<Button>().onClick.AddListener(() => Navigate(button.Value));
		}

		// Make sure the home page is up
		NavigateHome();
	}

	/// <summary>
	///     Returns the first page registered in the panel with a given type
	/// </summary>
	/// <typeparam name="TPage">The type of the page to look for</typeparam>
	/// <returns>A page object, or null if one was not found.</returns>
	public TPage? GetPageOfType<TPage>() where TPage : UniversalModPanelPage
	{
		return _pages.Values.FirstOrDefault(p => p.GetType() == typeof(TPage)) as TPage;
	}

	/// <summary>
	///     Navigates the mod panel to the home page and clears the history stack
	/// </summary>
	public void NavigateHome()
	{
		_stack.Clear();
		HomeButton.interactable = false;
		BackButton.interactable = false;
		if (_currentPage) _currentPage.gameObject.SetActive(false);
		_currentPage = _pages["Home"];
		_currentPage.gameObject.SetActive(true);
		UpdatePaginationBar();
		UpdateBreadcrumb();
	}

	/// <summary>
	///     Navigates the mod panel back one page, popping a page from the history stack
	/// </summary>
	public void NavigateBack()
	{
		if (_stack.Count == 0) return;
		_currentPage.gameObject.SetActive(false);
		_currentPage = _stack.Pop();
		_currentPage.gameObject.SetActive(true);
		if (_stack.Count == 0) BackButton.interactable = false;
		UpdatePaginationBar();
		UpdateBreadcrumb();
	}

	/// <summary>
	///     Navigates the mod panel to a page, pushing the previous page onto the stack
	/// </summary>
	/// <param name="pageName">The string name of the page to navigate to</param>
	/// <exception cref="InvalidOperationException">A page with the given name couldn't be found</exception>
	public void Navigate(string pageName)
	{
		if (_pages.TryGetValue(pageName, out var page)) Navigate(page);
		else throw new InvalidOperationException("There is no page with the name '" + pageName + "'");
	}

	/// <summary>
	///     Navigates the mod panel to a page, pushing the previous page onto the stack
	/// </summary>
	/// <param name="page">The page behaviour to navigate to</param>
	public void Navigate(UniversalModPanelPage page)
	{
		// If the current page isn't the home page push it to the stack
		HomeButton.interactable = true;
		if (_currentPage.gameObject.name != "Home")
		{
			_stack.Push(_currentPage);
			BackButton.interactable = true;
		}

		_currentPage.gameObject.SetActive(false);
		_currentPage = page;
		_currentPage.gameObject.SetActive(true);

		// Update the breadcrumb and pagination bar
		UpdateBreadcrumb();
		UpdatePaginationBar();
	}

	/// <summary>
	///     On pages which are paginated, move to the previous sub-page
	/// </summary>
	public void PaginatePrevious()
	{
	}

	/// <summary>
	///     On pages which are paginated, move to the next sub-page
	/// </summary>
	public void PaginateNext()
	{
	}

	private void UpdatePaginationBar()
	{
	}

	private void UpdateBreadcrumb()
	{
		var breadcrumb = string.Join(" / ", _stack.Select(p => p.DisplayName)
			.Concat(new[] {_currentPage.DisplayName}).ToArray());
		Breadcrumb.text = breadcrumb;
	}

	#endregion

	#region Static Methods

	/// <summary>
	///     Call this method to let Sodalite know which config entries it should draw under your plugin
	/// </summary>
	/// <param name="plugin">Your plugin info</param>
	/// <param name="configEntries">The config entries you want to draw</param>
	public static void RegisterPluginSettings(PluginInfo plugin, params ConfigEntryBase[] configEntries)
	{
		RegisteredConfigs[plugin] = configEntries;
	}

	/// <summary>
	///     Call this method to draw all
	/// </summary>
	/// <param name="plugin"></param>
	/// <param name="configFile"></param>
	public static void RegisterPluginSettings(PluginInfo plugin, ConfigFile configFile)
	{
		// If there are no config entries, skip.
		if (configFile.Count == 0) return;

		// Get an array of the entries
		int i = 0;
		ConfigEntryBase[] entries = new ConfigEntryBase[configFile.Count];
		foreach (var key in configFile.Keys) entries[i++] = configFile[key];

		// Add them
		RegisterPluginSettings(plugin, entries);
	}

	/// <summary>
	///     Pass this method a prefab for a custom input field type should you need to create custom config fields
	/// </summary>
	/// <param name="inputField">The field prefab object</param>
	/// <param name="predicate">Predicate for selecting when this field should be drawn</param>
	public static void RegisterConfigField(ConfigFieldBase inputField, Func<ConfigEntryBase, bool> predicate)
	{
		RegisteredInputFields[inputField] = predicate;
	}

	/// <summary>
	///     Pass this method a prefab of a custom page to add to the panel to add a custom page.
	/// </summary>
	/// <remarks>This will only register the page in the panel, it will not create a button for it on the main page.</remarks>
	/// <param name="identifier">The identifier used to navigate to this page</param>
	/// <param name="page">The page prefab</param>
	/// <typeparam name="TPage">The type of the page to add</typeparam>
	public static void RegisterCustomPage<TPage>(string identifier, TPage page) where TPage : UniversalModPanelPage
	{
		if (string.IsNullOrEmpty(identifier)) throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));
		if (page is null) throw new ArgumentNullException(nameof(page));
		RegisteredCustomPages.Add(identifier, page);
	}

	/// <summary>
	/// Adds a new button on the homepage which when clicked, navigates the panel to the target page.
	/// </summary>
	/// <param name="buttonText">The text to put on the button</param>
	/// <param name="targetPage">The identifier of the target page</param>
	public static void AddHomepageButton(string buttonText, string targetPage)
	{
		if (string.IsNullOrEmpty(buttonText)) throw new ArgumentException("Button text cannot be empty", nameof(buttonText));
		if (string.IsNullOrEmpty(targetPage)) throw new ArgumentException("Target page cannot be empty", nameof(targetPage));
		CustomHomepageButtons.Add(buttonText, targetPage);
	}

	internal static void RegisterUnregisteredPluginConfigs()
	{
		// For each plugin just try to add all their keys
		foreach (var plugin in Chainloader.ManagerObject.GetComponents<BaseUnityPlugin>())
		{
			if (RegisteredConfigs.ContainsKey(plugin.Info)) continue;
			RegisterPluginSettings(plugin.Info, plugin.Config);
		}
	}

	#endregion
}

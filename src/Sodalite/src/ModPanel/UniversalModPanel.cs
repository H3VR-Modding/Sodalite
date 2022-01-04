using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Sodalite.ModPanel.Pages;
using UnityEngine;
using UnityEngine.UI;

namespace Sodalite.ModPanel
{
	/// <summary>
	/// Base class for creating a universal mod panel page
	/// </summary>
	public class UniversalModPanelPage : MonoBehaviour
	{
		/// <summary>
		/// Reference to the panel
		/// </summary>
		public UniversalModPanel Panel = null!;

		/// <summary>
		/// Display name for this page used in the breadcrumb
		/// </summary>
		public string DisplayName = null!;
	}

	/// <summary>
	/// Mod panel behaviour
	/// </summary>
	public class UniversalModPanel : MonoBehaviour
	{
		#region Fields

		[Header("References")] [SerializeField]
		private Button HomeButton = null!;

		[SerializeField] private Button BackButton = null!;
		[SerializeField] private Button PaginatePreviousButton = null!;
		[SerializeField] private Button PaginateNextButton = null!;
		[SerializeField] private Text PaginateText = null!;
		[SerializeField] private Text Breadcrumb = null!;
		[SerializeField] private List<UniversalModPanelPage> SerializedPages = null!;
		[SerializeField] internal UniversalModPanelLogPage LogPage = null!;


		internal static readonly Dictionary<PluginInfo, ConfigEntryBase[]> RegisteredConfigs = new();
		private readonly Dictionary<string, UniversalModPanelPage> _pages = new();
		private readonly Stack<UniversalModPanelPage> _stack = new();
		private UniversalModPanelPage _currentPage = null!;

		/// <summary>
		/// Instance of the mod panel, for people to access.
		/// </summary>
		public static UniversalModPanel Instance { get; private set; } = null!;

		#endregion

		#region MonoBehavior Methods

		private void Awake()
		{
			Instance = this;
			foreach (UniversalModPanelPage page in SerializedPages)
			{
				_pages.Add(page.gameObject.name, page);
				page.gameObject.SetActive(false);
			}

			NavigateHome();
		}

		/// <summary>
		/// Returns the first page registered in the panel with a given type
		/// </summary>
		/// <typeparam name="TPage">The type of the page to look for</typeparam>
		/// <returns>A page object, or null if one was not found.</returns>
		public TPage? GetPageOfType<TPage>() where TPage : UniversalModPanelPage
		{
			return _pages.Values.FirstOrDefault(p => p.GetType() == typeof(TPage)) as TPage;
		}

		/// <summary>
		/// Navigates the mod panel to the home page and clears the history stack
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
		/// Navigates the mod panel back one page, popping a page from the history stack
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
		/// Navigates the mod panel to a page, pushing the previous page onto the stack
		/// </summary>
		/// <param name="pageName">The string name of the page to navigate to</param>
		/// <exception cref="InvalidOperationException">A page with the given name couldn't be found</exception>
		public void Navigate(string pageName)
		{
			if (_pages.TryGetValue(pageName, out UniversalModPanelPage page)) Navigate(page);
			else throw new InvalidOperationException("There is no page with the name '" + pageName + "'");
		}

		/// <summary>
		/// Navigates the mod panel to a page, pushing the previous page onto the stack
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
		/// On pages which are paginated, move to the previous sub-page
		/// </summary>
		public void PaginatePrevious()
		{
		}

		/// <summary>
		/// On pages which are paginated, move to the next sub-page
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
		/// Registers the provided config entries for the settings of your plugin in the universal mod panel
		/// </summary>
		/// <param name="plugin">Your plugin</param>
		/// <param name="configEntries">The entries to register</param>
		public static void RegisterPluginSettings(PluginInfo plugin, params ConfigEntryBase[] configEntries)
		{
			RegisteredConfigs[plugin] = configEntries;
		}

		#endregion
	}
}

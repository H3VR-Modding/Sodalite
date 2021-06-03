using Sodalite.Api;

namespace Sodalite
{
	/// <summary>
	///		The main static class to retrieve the individual API classes from.
	/// </summary>
	public static class H3Api
	{
		/// <summary>
		///		Reference to the Wrist Menu API.
		///		See: <see cref="WristMenuAPI"/>
		/// </summary>
		public static WristMenuAPI WristMenu { get; } = new();

		/// <summary>
		///		Reference to the Lockable Panel API.
		///		See: <see cref="LockablePanelAPI"/>
		/// </summary>
		public static LockablePanelAPI LockablePanel { get; } = new();

		/// <summary>
		///		Reference to the Leaderboard API.
		///		See: <see cref="LeaderboardAPI"/>
		/// </summary>
		public static LeaderboardAPI Leaderboard { get; } = new();

		/// <summary>
		///		Reference to the Sosig API.
		///		See: <see cref="SosigAPI"/>
		/// </summary>
		public static SosigAPI Sosig { get; } = new();

		/// <summary>
		///		Reference to the Player API.
		///		See: <see cref="PlayerAPI"/>
		/// </summary>
		public static PlayerAPI Player { get; } = new();
	}
}

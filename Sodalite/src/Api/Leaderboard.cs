using System;
using System.Collections.Generic;
using Steamworks;

namespace Sodalite.Api
{
	/// <summary>
	///		Sodalite Leaderboard API provides methods for interfacing with the game's leaderboards.
	/// </summary>
	public class LeaderboardAPI
	{
		internal class LeaderboardDisableDisposable : IDisposable
		{
			private readonly HashSet<LeaderboardDisableDisposable> _disabled = new();

			internal LeaderboardDisableDisposable(HashSet<LeaderboardDisableDisposable> list)
			{
				_disabled.Add(this);
				_disabled = list;
			}

			public void Dispose()
			{
				_disabled.Remove(this);
			}
		}

		private readonly HashSet<LeaderboardDisableDisposable> _scoreboardDisabled = new();

		internal LeaderboardAPI()
		{
			// Disable any form of Steam leaderboard uploading
			On.Steamworks.SteamUserStats.UploadLeaderboardScore += (orig, leaderboard, method, score, details, count) =>
			{
				// If no mods have requested disabling leaderboards, let it pass
				if (_scoreboardDisabled.Count == 0) return orig(leaderboard, method, score, details, count);

				// Otherwise log that it's been disabled and return an invalid call
				Sodalite.StaticLogger.LogInfo("Scoreboard submission is disabled as requested by " + _scoreboardDisabled.Count + " mod(s)");
				return SteamAPICall_t.Invalid;
			};
		}

		/// <summary>
		///		Call this method to get a disposable leaderboard disable lock.
		///		Leaderboard submission will be disabled while you have the lock, dispose it when you want to re-enable leaderboard submission.
		/// </summary>
		public IDisposable GetLeaderboardDisableLock()
		{
			return new LeaderboardDisableDisposable(_scoreboardDisabled);
		}
	}
}

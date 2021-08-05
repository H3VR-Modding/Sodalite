using System;
using System.Collections.Generic;
using Steamworks;

namespace Sodalite.Api
{
	/// <summary>
	///	Sodalite Leaderboard API provides methods for interfacing with the game's leaderboards.
	/// </summary>
	public static class LeaderboardAPI
	{
		internal class LeaderboardDisableDisposable : IDisposable
		{
			private readonly HashSet<LeaderboardDisableDisposable> _disabled;

			internal LeaderboardDisableDisposable(HashSet<LeaderboardDisableDisposable> list)
			{
				_disabled = list;
				_disabled.Add(this);
			}

			public void Dispose()
			{
				_disabled.Remove(this);
			}
		}

		private static readonly HashSet<LeaderboardDisableDisposable> ScoreboardDisabled = new();

		static LeaderboardAPI()
		{
			// Disable any form of Steam leaderboard uploading
			On.Steamworks.SteamUserStats.UploadLeaderboardScore += (orig, leaderboard, method, score, details, count) =>
			{
				// If no mods have requested disabling leaderboards, let it pass
				if (ScoreboardDisabled.Count == 0) return orig(leaderboard, method, score, details, count);

				// Otherwise return an invalid call
				return SteamAPICall_t.Invalid;
			};

			// Also prevent the player from creating new Leaderboards
			On.Steamworks.SteamUserStats.FindOrCreateLeaderboard += (orig, name, method, type) =>
			{
				// If no mods have requested disabling leaderboards, let it pass
				if (ScoreboardDisabled.Count == 0) return orig(name, method, type);

				// Otherwise redirect this call to just the regular find and don't create
				return SteamUserStats.FindLeaderboard(name);
			};
		}

		/// <summary>
		///	Calling this method will let you disable the Steam leaderboards. Disposing of the object returned by
		/// this method will re-enable the leaderboards (as long as no other mod is holding their own lock)
		/// </summary>
		/// <returns>A disposable that while not disposed prevents any scores from submitting to Steam leaderboards.</returns>
		public static IDisposable GetLeaderboardDisableLock()
		{
			return new LeaderboardDisableDisposable(ScoreboardDisabled);
		}
	}
}

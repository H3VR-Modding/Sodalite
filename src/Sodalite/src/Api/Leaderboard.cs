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
				ScoreboardDisabled.Count == 0 ? orig(leaderboard, method, score, details, count) : SteamAPICall_t.Invalid;

			// Also prevent the player from creating new Leaderboards.
			On.Steamworks.SteamUserStats.FindOrCreateLeaderboard += (orig, name, method, type) =>
				ScoreboardDisabled.Count == 0 ? orig(name, method, type) : SteamUserStats.FindLeaderboard(name);
		}

		/// <summary>
		///	Calling this method will let you disable the Steam leaderboards. Disposing of the object returned by
		/// this method will re-enable the leaderboards (as long as no other mod is holding their own lock)
		/// </summary>
		/// <returns>A disposable that while not disposed prevents any scores from submitting to Steam leaderboards.</returns>
		public static IDisposable GetLeaderboardDisableLock() => new LeaderboardDisableDisposable(ScoreboardDisabled);
	}
}

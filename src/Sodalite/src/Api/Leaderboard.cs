using System;
using System.Collections.Generic;
using Sodalite.Utilities;
using Steamworks;

namespace Sodalite.Api
{
	/// <summary>
	///	Sodalite Leaderboard API provides methods for interfacing with the game's leaderboards.
	/// </summary>
	public static class LeaderboardAPI
	{
		/// <summary>
		/// Call the TakeLock method to lock Steam leaderboard functionality and dispose of the returned value to re-enable.
		/// </summary>
		public static readonly SafeMultiLock LeaderboardDisabled = new();

#if RUNTIME
		static LeaderboardAPI()
		{
			// Disable any form of Steam leaderboard uploading
			On.Steamworks.SteamUserStats.UploadLeaderboardScore += (orig, leaderboard, method, score, details, count) =>
				LeaderboardDisabled.IsLocked ? SteamAPICall_t.Invalid : orig(leaderboard, method, score, details, count);

			// Also prevent the player from creating new Leaderboards.
			On.Steamworks.SteamUserStats.FindOrCreateLeaderboard += (orig, name, method, type) =>
				LeaderboardDisabled.IsLocked ? SteamUserStats.FindLeaderboard(name) : orig(name, method, type);
		}
#endif

		/// <summary>
		///	Calling this method will let you disable the Steam leaderboards. Disposing of the object returned by
		/// this method will re-enable the leaderboards (as long as no other mod is holding their own lock)
		///
		/// OBSOLETE, please take a lock directly from LeaderboardAPI.LeaderboardDisabled instead.
		/// </summary>
		/// <returns>A disposable that while not disposed prevents any scores from submitting to Steam leaderboards.</returns>
		[Obsolete("Please take a lock directly from LeaderboardAPI.LeaderboardDisabled instead")]
		public static IDisposable GetLeaderboardDisableLock() => LeaderboardDisabled.TakeLock();
	}
}

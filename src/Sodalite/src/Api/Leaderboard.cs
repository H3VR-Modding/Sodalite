using System;
using HarmonyLib;
using Sodalite.Utilities;
using Steamworks;

namespace Sodalite.Api;

/// <summary>
///     Sodalite Leaderboard API provides methods for interfacing with the game's leaderboards.
/// </summary>
public static class LeaderboardAPI
{
	/// <summary>
	///     Call the TakeLock method to lock Steam leaderboard functionality and dispose of the returned value to re-enable.
	/// </summary>
	public static readonly SafeMultiLock LeaderboardDisabled = new();

	/// <summary>
	///     Calling this method will let you disable the Steam leaderboards. Disposing of the object returned by
	///     this method will re-enable the leaderboards (as long as no other mod is holding their own lock)
	///     OBSOLETE, please take a lock directly from LeaderboardAPI.LeaderboardDisabled instead.
	/// </summary>
	/// <returns>A disposable that while not disposed prevents any scores from submitting to Steam leaderboards.</returns>
	[Obsolete("Please take a lock directly from LeaderboardAPI.LeaderboardDisabled instead")]
	public static IDisposable GetLeaderboardDisableLock()
	{
		return LeaderboardDisabled.TakeLock();
	}

	[HarmonyPatch(typeof(SteamUserStats), nameof(SteamUserStats.UploadLeaderboardScore)), HarmonyPrefix]
	private static bool OnSteamUserStatsUploadLeaderboardScore(ref SteamAPICall_t __result)
	{
		// Stop scores from uploading if locked
		if (!LeaderboardDisabled.IsLocked) return true;
		__result = SteamAPICall_t.Invalid;
		return false;
	}

	[HarmonyPatch(typeof(SteamUserStats), nameof(SteamUserStats.FindOrCreateLeaderboard)), HarmonyPrefix]
	private static bool OnSteamUserStatsFindOrCreateLeaderboard(string pchLeaderboardName, ref SteamAPICall_t __result)
	{
		// Stop new leaderboards from being made
		if (!LeaderboardDisabled.IsLocked) return true;
		__result = SteamUserStats.FindLeaderboard(pchLeaderboardName);
		return false;
	}

}

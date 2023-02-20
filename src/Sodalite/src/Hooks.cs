using FistVR;
using HarmonyLib;
using Sodalite.Api;
using Steamworks;

// ReSharper disable InconsistentNaming

namespace Sodalite;

internal static class Hooks
{
	[HarmonyPatch(typeof(FVRWristMenu2), nameof(FVRWristMenu2.Awake)), HarmonyPostfix]
	private static void FVRWriteMenuOnAwake(FVRWristMenu2 __instance)
	{
		WristMenuAPI.WristMenuAwake(__instance);
	}

	[HarmonyPatch(typeof(SteamUserStats), nameof(SteamUserStats.UploadLeaderboardScore)), HarmonyPrefix]
	public static bool OnSteamUserStatsUploadLeaderboardScore(ref SteamAPICall_t __result)
	{
		// Stop scores from uploading if locked
		if (!LeaderboardAPI.LeaderboardDisabled.IsLocked) return true;
		__result = SteamAPICall_t.Invalid;
		return false;
	}

	[HarmonyPatch(typeof(SteamUserStats), nameof(SteamUserStats.FindOrCreateLeaderboard)), HarmonyPrefix]
	public static bool OnSteamUserStatsFindOrCreateLeaderboard(string pchLeaderboardName, ref SteamAPICall_t __result)
	{
		// Stop new leaderboards from being made
		if (!LeaderboardAPI.LeaderboardDisabled.IsLocked) return true;
		__result = SteamUserStats.FindLeaderboard(pchLeaderboardName);
		return false;
	}
}

using System.Collections.Generic;
using System.Linq;
using FistVR;
using Sodalite.Utilities;

namespace Sodalite.Api
{
	/// <summary>
	/// Player API for Sodalite. Contains methods relating to the player
	/// </summary>
	public static class PlayerAPI
	{
		/// <summary>
		/// Call the TakeLock method to lock the player from snap turning and dispose of the returned value to re-enable.
		/// </summary>
		public static readonly SafeMultiLock SnapTurnDisabled = new();

#if RUNTIME
		static PlayerAPI()
		{
			On.FistVR.FVRMovementManager.TurnClockWise += FVRMovementManagerOnTurnClockWise;
			On.FistVR.FVRMovementManager.TurnCounterClockWise += FVRMovementManagerOnTurnCounterClockWise;
		}

		private static void FVRMovementManagerOnTurnClockWise(On.FistVR.FVRMovementManager.orig_TurnClockWise orig, FVRMovementManager self)
		{
			if (!SnapTurnDisabled.IsLocked) orig(self);
		}

		private static void FVRMovementManagerOnTurnCounterClockWise(On.FistVR.FVRMovementManager.orig_TurnCounterClockWise orig, FVRMovementManager self)
		{
			if (!SnapTurnDisabled.IsLocked) orig(self);
		}
#endif

		/// <summary>
		/// Returns a list of the objects the player currently has equipped.
		/// This includes objects in the player's hands, in quickbelt slots, or in an equipped backpack slot.
		/// </summary>
		public static List<FVRPhysicalObject> GetEquippedObjects()
		{
			// Make a list of the objects and a helper method to add to it
			List<FVRPhysicalObject> objects = new();

			void AddObject(FVRPhysicalObject? obj)
			{
				// If this slot is holding a backpack also iterate over it's slots and add them
				if (obj is PlayerBackPack backpack)
				{
					objects.AddRange(from backpackSlot in backpack.Slots
						where backpackSlot.CurObject is not null
						select backpackSlot.CurObject);
				}
				// If the slot contains something, add it
				else if (obj is not null)
					objects.Add(obj);
			}

			// Get whatever the player is holding
			if (GM.CurrentMovementManager.Hands[0].CurrentInteractable is FVRPhysicalObject rightObj)
				AddObject(rightObj);
			if (GM.CurrentMovementManager.Hands[0].CurrentInteractable is FVRPhysicalObject leftObj)
				AddObject(leftObj);

			// Iterate over the quickbelt slots on the player
			foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QuickbeltSlots)
				AddObject(slot.CurObject);

			// Return the objects as a read only collection
			return objects;
		}
	}
}

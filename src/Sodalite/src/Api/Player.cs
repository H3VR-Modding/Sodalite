using System.Collections.Generic;
using System.Linq;
using FistVR;
using Sodalite.Utilities;

namespace Sodalite.Api;

/// <summary>
///     Player API for Sodalite. Contains methods relating to the player
/// </summary>
public static class PlayerAPI
{
	/// <summary>
	///     Returns a list of the objects the player currently has equipped.
	///     This includes objects in the player's hands, in quickbelt slots, or in an equipped backpack slot.
	/// </summary>
	public static List<FVRPhysicalObject> GetEquippedObjects()
	{
		// Make a list of the objects and a helper method to add to it
		List<FVRPhysicalObject> objects = new();

		void AddObject(FVRPhysicalObject? obj)
		{
			// If the slot contains something, add it
			if (obj is null) return;
			objects.Add(obj);

			// Check for any special cases
			switch (obj)
			{
				// If this slot is holding a backpack also iterate over it's slots and add them
				case PlayerBackPack backpack:
					objects.AddRange(from backpackSlot in backpack.Slots
						where backpackSlot.CurObject is not null
						select backpackSlot.CurObject);
					break;

				// If the object is a magazine also check if there's another one being palmed
				case FVRFireArmMagazine magazine when magazine.m_magChild is not null:
					objects.Add(magazine.m_magChild);
					break;
			}
		}

		// Get whatever the player is holding
		if (GM.CurrentMovementManager.Hands[0].CurrentInteractable is FVRPhysicalObject rightObj)
			AddObject(rightObj);
		if (GM.CurrentMovementManager.Hands[0].CurrentInteractable is FVRPhysicalObject leftObj)
			AddObject(leftObj);

		// Iterate over the quickbelt slots on the player
		foreach (var slot in GM.CurrentPlayerBody.QuickbeltSlots)
			AddObject(slot.CurObject);

		// Return the objects as a read only collection
		return objects;
	}
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FistVR;
using Valve.Newtonsoft.Json;

namespace Sodalite.Api
{
	public class PlayerAPI
	{
		internal PlayerAPI()
		{
		}

		/// <summary>
		/// Returns a read-only collection of the objects the player currently has equipped.
		/// This includes objects in the player's hands, in quickbelt slots, or in an equipped backpack slot.
		/// </summary>
		/// <returns></returns>
		public ReadOnlyCollection<FVRPhysicalObject> GetEquippedObjects()
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
			return objects.AsReadOnly();
		}
	}
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FistVR;
using Sodalite.Utilities;

namespace Sodalite.Api
{
	public static class FirearmAPI
	{
		/// <summary>
		/// Gets a collection of all the objects attached to this firearm
		/// </summary>
		/// <param name="fireArm">The firearm that is being scanned for attachments</param>
		/// <returns>A read-only collection containing the firearm, it's magazine, and all it's attachments</returns>
		public static ReadOnlyCollection<FVRPhysicalObject> GetAttachedObjects(this FVRFireArm fireArm)
		{
			// Add the gun itself, it's magazine, and any attachments into a list
			List<FVRPhysicalObject> detectedObjects = new() { fireArm };
			if (fireArm.Magazine is not null && !fireArm.Magazine.IsIntegrated && fireArm.Magazine.ObjectWrapper is not null) detectedObjects.Add(fireArm.Magazine);
			detectedObjects.AddRange(fireArm.Attachments.Where(attachment => attachment.ObjectWrapper is not null).Cast<FVRPhysicalObject>());

			// Return the list as read only
			return detectedObjects.AsReadOnly();
		}

		/// <summary>
		/// Checks if all the components contained in this saved gun are available.
		/// Mainly useful for verifying that a saved gun does not contain components from uninstalled mods
		/// </summary>
		/// <param name="savedGun">The SavedGun object (vault file)</param>
		/// <returns>True if everything is available</returns>
		public static bool AllComponentsLoaded(this SavedGun savedGun)
		{
			return savedGun.Components.All(component => IM.OD.ContainsKey(component.ObjectID));
		}


		/// <summary>
		/// Merge the compatible magazines, clips, and speed loaders into one list
		/// </summary>
		public static ReadOnlyCollection<FVRObject> GetCompatibleMagazines(this FVRObject firearm)
		{
			firearm = IM.OD[firearm.ItemID];
			return firearm.CompatibleMagazines.Concat(firearm.CompatibleClips).Concat(firearm.CompatibleSpeedLoaders).ToList().AsReadOnly();
		}

		/// <summary>
		/// Checks if the object has any compatible ammo containers
		/// </summary>
		/// <param name="item">The FVRObject to check</param>
		/// <returns>True if the FVRObject has any compatible rounds, clips, magazines, or speedloaders. False if it contains none of these</returns>
		public static bool HasAmmo(this FVRObject item)
		{
			//Refresh the FVRObject to have data directly from object dictionary
			item = IM.OD[item.ItemID];
			return item.CompatibleSingleRounds is { Count: > 0 } || item.HasMagazine();
		}


		/// <summary>
		/// Returns true if the given FVRObject has any compatible clips, magazines, or speed loaders
		/// </summary>
		/// <param name="item">The FVRObject that is being checked</param>
		/// <returns>True if the FVRObject has any compatible clips, magazines, or speed loaders. False if it contains none of these</returns>
		public static bool HasMagazine(this FVRObject item)
		{
			//Refresh the FVRObject to have data directly from object dictionary
			item = IM.OD[item.ItemID];

			// Return true if any of the compatible object dictionaries have anything in them
			return item.CompatibleClips is { Count: > 0 } ||
			       item.CompatibleMagazines is { Count: > 0 } ||
			       item.CompatibleSpeedLoaders is { Count: > 0 };
		}

		public static FVRObject? GetNextHighestCapacityMagazine(IEnumerable<FVRObject> pool, int currentCap)
		{
			// Find out what the next highest capacity from the pool is
			int nextHighestCap = pool.Select(x => x.MagazineCapacity).Where(x => x > currentCap).Min();

			// Select all the magazines with that capacity
			FVRObject[] valid = pool.Where(x => x.MagazineCapacity == nextHighestCap).ToArray();

			// Return a random item from the array or null if there are none
			return valid.Length == 0 ? null : valid.GetRandom();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FistVR;
using Sodalite.Utilities;

namespace Sodalite.Api
{
	/// <summary>
	/// Sodalite API for interacting with firearms and magazines
	/// </summary>
	public static class FirearmAPI
	{
		/// <summary>
		/// Gets a collection of all the objects attached to this firearm
		/// </summary>
		/// <param name="fireArm">The firearm that is being scanned for attachments</param>
		/// <returns>A read-only collection containing the firearm, it's magazine, and all it's attachments</returns>
		public static FVRPhysicalObject[] GetAttachedObjects(this FVRFireArm fireArm)
		{
			// Add the gun itself, it's magazine, and any attachments into a list
			List<FVRPhysicalObject> detectedObjects = new() {fireArm};
			if (fireArm.Magazine is not null && !fireArm.Magazine.IsIntegrated && fireArm.Magazine.ObjectWrapper is not null) detectedObjects.Add(fireArm.Magazine);
			detectedObjects.AddRange(fireArm.Attachments.Where(attachment => attachment.ObjectWrapper is not null).Cast<FVRPhysicalObject>());

			// Return the list as read only
			return detectedObjects.ToArray();
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
		public static FVRObject[] GetCompatibleMagazines(this FVRObject firearm)
		{
			firearm = IM.OD[firearm.ItemID];
			return firearm.CompatibleMagazines.Concat(firearm.CompatibleClips).Concat(firearm.CompatibleSpeedLoaders).ToArray();
		}

		/// <summary>
		/// Checks if the object has any compatible ammo containers
		/// </summary>
		/// <param name="item">The FVRObject to check</param>
		/// <returns>True if the FVRObject has any compatible rounds, clips, magazines, or speed loaders. False if it contains none of these</returns>
		public static bool HasAmmo(this FVRObject item)
		{
			//Refresh the FVRObject to have data directly from object dictionary
			item = IM.OD[item.ItemID];
			return item.CompatibleSingleRounds is {Count: > 0} || item.HasMagazine();
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
			return item.CompatibleClips is {Count: > 0} ||
			       item.CompatibleMagazines is {Count: > 0} ||
			       item.CompatibleSpeedLoaders is {Count: > 0};
		}

		/// <summary>
		/// Gets a magazine from the provided pool that has a capacity one step up from the capacity provided
		/// </summary>
		/// <param name="pool">The collection of magazines to select from</param>
		/// <param name="currentCap">The capacity to compare against</param>
		/// <param name="filter">Optional func to filter the results</param>
		/// <returns>The FVRObject of a magazine, clip, or speed loader</returns>
		public static FVRObject? GetNextHighestCapacityMagazine(IList<FVRObject> pool, int currentCap, Func<FVRObject, bool>? filter = null)
		{
			return filter is not null
				? GetSmallestMagazine(pool, x => x.MagazineCapacity >= currentCap && filter(x))
				: GetSmallestMagazine(pool, x => x.MagazineCapacity >= currentCap);
		}

		/// <summary>
		/// Returns a magazine that is compatible with any of the (firearm) objects in the pool
		/// </summary>
		/// <param name="pool">The collection of objects to get a compatible magazine for</param>
		/// <param name="filter">The optional filter to apply to the result</param>
		/// <returns>The FVRObject of a magazine, clip, or speed loader</returns>
		public static FVRObject? GetMagazineCompatibleWithAny(IEnumerable<FVRObject> pool, Func<FVRObject, bool>? filter = null)
		{
			// Create a list of all the compatible magazines
			List<FVRObject> compatible = (
				from item in pool
				from mag in item.GetCompatibleMagazines()
				where filter is null || filter(mag)
				select mag
			).ToList();

			// Return a random one
			return compatible.GetRandom();
		}

		/// <summary>
		/// Returns the smallest magazine from the pool that passes the filter or null if there are none
		/// </summary>
		/// <param name="pool">The collection of objects to get a compatible magazine for</param>
		/// <param name="filter">The optional filter to apply to the result</param>
		/// <returns>The FVRObject of a magazine, clip, or speed loader</returns>
		public static FVRObject? GetSmallestMagazine(IList<FVRObject> pool, Func<FVRObject, bool>? filter = null)
		{
			// Find out what the smallest capacity is
			int smallestCap = (filter is null ? pool : pool.Where(filter)).Select(x => x.MagazineCapacity).Min();

			// Select all the magazines with that capacity
			IEnumerable<FVRObject> valid = (filter is null ? pool : pool.Where(filter)).Where(x => x.MagazineCapacity == smallestCap);

			// Return a random item from the array or null if there are none
			FVRObject[] array = valid.ToArray();
			return array.Length == 0 ? null : array.GetRandom();
		}

		/// <summary>
		/// Returns the smallest magazine compatible with the provided firearm that passes the filter or null if there are none
		/// </summary>
		/// <param name="firearm">The FVRObject of the firearm</param>
		/// <param name="filter">The optional filter to apply to the result</param>
		/// <returns>The FVRObject of a magazine, clip, or speed loader</returns>
		public static FVRObject? GetSmallestMagazine(FVRObject firearm, Func<FVRObject, bool>? filter = null)
		{
			return GetSmallestMagazine(firearm.GetCompatibleMagazines(), filter);
		}

		/// <summary>
		/// Returns the chambers of this weapon.
		/// Unfortunately the definitions for each chamber in the game aren't shared so this needs to exist because of that.
		/// </summary>
		/// <param name="firearm">The firearm to get the chambers of</param>
		/// <returns>An array of chambers belonging to this weapon</returns>
		public static FVRFireArmChamber[] GetFirearmChambers(FVRFireArm firearm)
		{
			// Return a new array with the chamber, or chambers if the gun has multiple.
			return firearm switch
			{
				BAP bap => new[] {bap.Chamber},
				BoltActionRifle boltActionRifle => new[] {boltActionRifle.Chamber},
				BreakActionWeapon breakActionWeapon => breakActionWeapon.Barrels.Select(x => x.Chamber).ToArray(),
				ClosedBoltWeapon closedBoltWeapon => new[] {closedBoltWeapon.Chamber},
				Derringer derringer => derringer.Barrels.Select(x => x.Chamber).ToArray(),
				Flaregun flaregun => new[] {flaregun.Chamber},
				Handgun handgun => new[] {handgun.Chamber},
				HCB hcb => new[] {hcb.Chamber},
				LAPD2019 lapd2019 => lapd2019.Chambers,
				LeverActionFirearm leverActionFirearm => new[] {leverActionFirearm.Chamber, leverActionFirearm.Chamber2},
				M72 m72 => new[] {m72.Chamber},
				OpenBoltReceiver openBoltReceiver => new[] {openBoltReceiver.Chamber},
				PotatoGun potatoGun => new[] {potatoGun.Chamber},
				Revolver revolver => revolver.Chambers,
				RevolvingShotgun revolvingShotgun => revolvingShotgun.Chambers,
				RGM40 rgm40 => new[] {rgm40.Chamber},
				RollingBlock rollingBlock => new[] {rollingBlock.Chamber},
				RPG7 rpg7 => new[] {rpg7.Chamber},
				SimpleLauncher simpleLauncher => new[] {simpleLauncher.Chamber},
				SingleActionRevolver singleActionRevolver => singleActionRevolver.Cylinder.Chambers,
				TubeFedShotgun tubeFedShotgun => new[] {tubeFedShotgun.Chamber},
				MF2_RL mf2Rl => new[] {mf2Rl.Chamber},
				_ => new FVRFireArmChamber[0],
			};
		}
	}
}

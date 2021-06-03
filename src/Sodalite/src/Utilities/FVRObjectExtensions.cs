using FistVR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sodalite
{
	public static class FVRObjectExtensions
	{
		#region Firearm extensions

		/// <summary>
		/// Enumerates over the compatible magazines, clips, and speed loaders for this firearm.
		/// </summary>
		/// <param name="firearm">The FVRObject of the firearm</param>
		/// <returns>An enumerator of the compatible ammo containersd</returns>
		public static IEnumerable<FVRObject> EnumerateCompatibleAmmoContainers(this FVRObject firearm)
		{
			//Refresh the FVRObject to have data directly from object dictionary
			firearm = IM.OD[firearm.ItemID];

			// Create an enumerator over the possible containers and apply our filter
			return firearm.CompatibleMagazines
				.Concat(firearm.CompatibleClips)
				.Concat(firearm.CompatibleSpeedLoaders);
		}


		/// <summary>
		/// Returns the smallest capacity magazine from the given list of magazine FVRObjects
		/// </summary>
		/// <param name="magazines">A list of magazine FVRObjects</param>
		/// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will be excluded</param>
		/// <returns>An FVRObject for the smallest magazine. Can be null if magazines list is empty</returns>
		public static FVRObject GetSmallestCapacityMagazine(List<FVRObject> magazines, List<string>? blacklistedMagazines = null)
		{
			if (magazines is null || magazines.Count == 0) return null;

			//This was done with a list because whenever there are multiple smallest magazines of the same size, we want to return a random one from those options
			List<FVRObject> smallestMagazines = new List<FVRObject>();

			foreach (FVRObject magazine in magazines)
			{
				if (blacklistedMagazines is not null && blacklistedMagazines.Contains(magazine.ItemID)) continue;

				else if (smallestMagazines.Count == 0) smallestMagazines.Add(magazine);

				//If we find a new smallest mag, clear the list and add the new smallest
				else if (magazine.MagazineCapacity < smallestMagazines[0].MagazineCapacity)
				{
					smallestMagazines.Clear();
					smallestMagazines.Add(magazine);
				}

				//If the magazine is the same capacity as current smallest, add it to the list
				else if (magazine.MagazineCapacity == smallestMagazines[0].MagazineCapacity)
				{
					smallestMagazines.Add(magazine);
				}
			}


			if (smallestMagazines.Count == 0) return null;

			//Return a random magazine from the smallest
			return smallestMagazines.GetRandom();
		}


		/// <summary>
		/// Returns the smallest capacity magazine that is compatible with the given firearm
		/// </summary>
		/// <param name="firearm">The FVRObject of the firearm</param>
		/// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will be excluded</param>
		/// <returns>An FVRObject for the smallest magazine. Can be null if firearm has no magazines</returns>
		public static FVRObject? GetSmallestCapacityMagazine(FVRObject firearm, List<string>? blacklistedMagazines = null)
		{
			//Refresh the FVRObject to have data directly from object dictionary
			firearm = IM.OD[firearm.ItemID];

			return GetSmallestCapacityMagazine(firearm.CompatibleMagazines, blacklistedMagazines);
		}


		/// <summary>
		/// Checks if the object has any compatible ammo containers
		/// </summary>
		/// <param name="item">The FVRObject to check</param>
		/// <returns>True if the FVRObject has any compatible rounds, clips, magazines, or speedloaders. False if it contains none of these</returns>
		public static bool HasAmmoObjects(this FVRObject item)
		{
			//Refresh the FVRObject to have data directly from object dictionary
			item = IM.OD[item.ItemID];
			return item.CompatibleSingleRounds is {Count: > 0} || item.HasAmmoContainer();
		}


		/// <summary>
		/// Returns true if the given FVRObject has any compatible clips, magazines, or speedloaders
		/// </summary>
		/// <param name="item">The FVRObject that is being checked</param>
		/// <returns>True if the FVRObject has any compatible clips, magazines, or speedloaders. False if it contains none of these</returns>
		public static bool HasAmmoContainer(this FVRObject item)
		{
			//Refresh the FVRObject to have data directly from object dictionary
			item = IM.OD[item.ItemID];

			// Return true if any of the compatible object dictionaries have anything in them
			return item.CompatibleClips is {Count: > 0} ||
			       item.CompatibleMagazines is {Count: > 0} ||
			       item.CompatibleSpeedLoaders is {Count: > 0};
		}


		/// <summary>
		/// Returns the next largest magazine when compared to the current magazine. Only magazines from the possibleMagazines list are considered as next largest magazine candidates
		/// </summary>
		/// <param name="currentMagazine">The base magazine FVRObject, for which we are getting the next largest magazine</param>
		/// <param name="possibleMagazines">A list of magazine FVRObjects, which are the candidates for being the next largest magazine</param>
		/// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will be excluded</param>
		/// <returns>An FVRObject for the next largest magazine. Can be null if no next largest magazine is found</returns>
		public static FVRObject? GetNextHighestCapacityMagazine(FVRObject currentMagazine, List<FVRObject> possibleMagazines, List<string>? blacklistedMagazines = null)
		{
			if (possibleMagazines is null || possibleMagazines.Count == 0) return null;

			//We make this a list so that when several next largest mags have the same capacity, we can return a random magazine from that selection
			List<FVRObject> nextLargestMagazines = new List<FVRObject>();

			foreach (FVRObject magazine in possibleMagazines)
			{
				if (blacklistedMagazines is not null && blacklistedMagazines.Contains(magazine.ItemID)) continue;

				else if (nextLargestMagazines.Count == 0) nextLargestMagazines.Add(magazine);

				//If our next largest mag is the same size as the original, then we take the new larger mag
				if (magazine.MagazineCapacity > currentMagazine.MagazineCapacity && currentMagazine.MagazineCapacity == nextLargestMagazines[0].MagazineCapacity)
				{
					nextLargestMagazines.Clear();
					nextLargestMagazines.Add(magazine);
				}

				//We want the next largest mag size, so the minimum mag size that's also greater than the current mag size
				else if (magazine.MagazineCapacity > currentMagazine.MagazineCapacity && magazine.MagazineCapacity < nextLargestMagazines[0].MagazineCapacity)
				{
					nextLargestMagazines.Clear();
					nextLargestMagazines.Add(magazine);
				}

				//If this magazine has the same capacity as the next largest magazines, add it to the list of options
				else if (magazine.MagazineCapacity == nextLargestMagazines[0].MagazineCapacity)
				{
					nextLargestMagazines.Add(magazine);
				}
			}

			//If the capacity has not increased compared to the original, we should return null
			if (nextLargestMagazines[0].MagazineCapacity == currentMagazine.MagazineCapacity) return null;

			return nextLargestMagazines.GetRandom();
		}
	}

	#endregion
}

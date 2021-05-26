using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sodalite
{
	public static class FirearmUtils
	{

		/// <summary>
		/// Returns a list of magazines compatible with the firearm, and also within any of the optional criteria
		/// </summary>
		/// <param name="firearm">The FVRObject of the firearm</param>
		/// <param name="minCapacity">The minimum capacity for desired magazines</param>
		/// <param name="maxCapacity">The maximum capacity for desired magazines. If this values is zero or negative, it is interpreted as no capacity ceiling</param>
		/// <param name="smallestIfEmpty">If true, when the returned list would normally be empty, will instead return the smallest capacity magazine compatible with the firearm</param>
		/// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will be excluded</param>
		/// <returns> A list of magazine FVRObjects that are compatible with the given firearm </returns>
		public static List<FVRObject> GetCompatibleMagazines(FVRObject firearm, int minCapacity = 0, int maxCapacity = 9999, bool smallestIfEmpty = true, List<string>? blacklistedMagazines = null)
		{
			//If the FVRObjects has no compatible magazines, we return an empty list
			if (firearm.CompatibleMagazines is null || firearm.CompatibleMagazines.Count == 0) return new List<FVRObject>();

			//If the max capacity is zero or negative, we iterpret that as no limit on max capacity
			if (maxCapacity <= 0) maxCapacity = 9999;

			//If we're including all magazine capacities, and there is no blacklist, we can just return the known compatible magazines
			if (blacklistedMagazines is null && minCapacity <= 0 && maxCapacity >= 9999) return firearm.CompatibleMagazines;

			//Go through the magazines and remove any that don't match criteria
			List<FVRObject> compatibleMagazines = new List<FVRObject>(firearm.CompatibleMagazines);
			for(int i = compatibleMagazines.Count - 1; i >= 0; i--)
			{
				if(blacklistedMagazines is not null && blacklistedMagazines.Contains(compatibleMagazines[i].ItemID))
				{
					compatibleMagazines.RemoveAt(i);
				}

				if(compatibleMagazines[i].MagazineCapacity < minCapacity || compatibleMagazines[i].MagazineCapacity > maxCapacity)
				{
					compatibleMagazines.RemoveAt(i);
				}
			}

			//If the resulting list is empty, and smallestIfEmpty is true, add the smallest capacity magazine to the list
			if(compatibleMagazines.Count == 0)
			{
				FVRObject? magazine = GetSmallestCapacityMagazine(firearm.CompatibleMagazines);
				if (magazine is not null) compatibleMagazines.Add(magazine);
			}

			return compatibleMagazines;
		}



		/// <summary>
		/// Returns the smallest capacity magazine from the given list of magazine FVRObjects
		/// </summary>
		/// <param name="magazines">A list of magazine FVRObjects</param>
		/// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will be excluded</param>
		/// <returns>An FVRObject for the smallest magazine. Can be null if magazines list is empty</returns>
		public static FVRObject? GetSmallestCapacityMagazine(List<FVRObject> magazines, List<string>? blacklistedMagazines = null)
		{
			if (magazines is null || magazines.Count == 0) return null;

			//This was done with a list because whenever there are multiple smallest magazines of the same size, we want to return a random one from those options
			List<FVRObject> smallestMagazines = new List<FVRObject>();
			
			foreach(FVRObject magazine in magazines)
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
			return GetSmallestCapacityMagazine(firearm.CompatibleMagazines, blacklistedMagazines);
		}



		/// <summary>
		/// Returns true if the given FVRObject has any compatible rounds, clips, magazines, or speedloaders
		/// </summary>
		/// <param name="item">The FVRObject that is being checked</param>
		/// <returns>True if the FVRObject has any compatible rounds, clips, magazines, or speedloaders. False if it contains none of these</returns>
		public static bool FVRObjectHasAmmoObject(FVRObject item)
		{
			if (item == null) return false;
			return (item.CompatibleSingleRounds != null && item.CompatibleSingleRounds.Count != 0) || (item.CompatibleClips != null && item.CompatibleClips.Count > 0) || (item.CompatibleMagazines != null && item.CompatibleMagazines.Count > 0) || (item.CompatibleSpeedLoaders != null && item.CompatibleSpeedLoaders.Count != 0);
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
}

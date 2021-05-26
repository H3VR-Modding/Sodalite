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
		/// <param name="firearmItemID">The ItemID of the firearms FVRObject</param>
		/// <param name="minCapacity">The minimum capacity for desired magazines</param>
		/// <param name="maxCapacity">The maximum capacity for desired magazines. If this values is zero or negative, it is interpreted as no capacity ceiling</param>
		/// <param name="smallestIfEmpty">If true, when the returned list would normally be empty, will instead return the smallest capacity magazine compatible with the firearm</param>
		/// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will not be included</param>
		/// <returns> A list of magazine FVRObjects that are compatible with the given firearm </returns>
		public static List<FVRObject> GetCompatibleMagazines(string firearmItemID, int minCapacity = 0, int maxCapacity = 9999, bool smallestIfEmpty = true, List<string>? blacklistedMagazines = null)
		{
			//We get the FVRObject directly from the object dictionary because we want any changes made on those FVRObjects
			FVRObject firearm = IM.OD[firearmItemID];

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
				compatibleMagazines.Add(GetSmallestCapacityMagazine(firearm.CompatibleMagazines));
			}

			return compatibleMagazines;
		}



		/// <summary>
		/// Returns the smallest capacity magazine from the given list of magazine FVRObjects
		/// </summary>
		/// <param name="magazines">A list of magazine FVRObjects</param>
		/// <returns>An FVRObject for the smallest magazine</returns>
		public static FVRObject GetSmallestCapacityMagazine(List<FVRObject> magazines)
		{
			if(magazines is null || magazines.Count == 0) throw new IndexOutOfRangeException("The list of compatible magazines was null or empty!");

			//This was done with a list because whenever there are multiple smallest magazines of the same size, we want to return a random one from those options
			List<FVRObject> smallestMagazines = new List<FVRObject>();
			smallestMagazines.Add(magazines[0]);

			for(int i = 1; i < magazines.Count; i++)
			{
				//If we find a new smallest mag, clear the list and add the new smallest
				if(magazines[i].MagazineCapacity < smallestMagazines[0].MagazineCapacity)
				{
					smallestMagazines.Clear();
					smallestMagazines.Add(magazines[i]);
				}

				//If the magazine is the same capacity as current smallest, add it to the list
				else if(magazines[i].MagazineCapacity == smallestMagazines[0].MagazineCapacity) {
					smallestMagazines.Add(magazines[i]);
				}
			}

			//Return a random magazine from the smallest
			return smallestMagazines.GetRandom();
		}



		/// <summary>
		/// Returns the smallest capacity magazine that is compatible with the given firearm
		/// </summary>
		/// <param name="firearmItemID">The ItemID of the firearm</param>
		/// <returns>An FVRObject for the smallest magazine</returns>
		public static FVRObject GetSmallestCapacityMagazine(string firearmItemID)
		{
			return GetSmallestCapacityMagazine(IM.OD[firearmItemID].CompatibleMagazines);
		}


	}
}

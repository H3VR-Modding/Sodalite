using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FistVR;

namespace Sodalite
{
	public static class FirearmExtensions
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
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FistVR;
using UnityEngine;

namespace Sodalite.Api
{
	/// <summary>
	/// Vault API for Sodalite. This class offers methods for creating serialized vault guns and spawning them back.
	/// </summary>
	public static class VaultAPI
	{
		/// <summary>
		/// Scans the given firearm and creates a vault file based on everything that is attached
		/// </summary>
		/// <remarks>
		/// This is a static adaptation of the compiled vault code. The contents have been tidied and documented
		/// </remarks>
		/// <param name="firearm">The firearm for which the vault file is being created</param>
		/// <returns>A SavedGunSerializable which represents the vault file for the given firearm</returns>
		public static SavedGun SaveGun(FVRFireArm firearm)
		{
			// Create our SavedGun object and assign the creation time
			SavedGun savedGun = new()
			{
				DateMade = DateTime.Now
			};

			// Get a list of all the attached objects (magazine, attachments) on this firearm
			IList<FVRPhysicalObject> detectedObjects = firearm.GetAttachedObjects();

			// Reset the rotation of the scanned weapon
			Vector3 forward = detectedObjects[0].transform.forward;
			Vector3 up = detectedObjects[0].transform.up;
			detectedObjects[0].transform.rotation = Quaternion.identity;

			//Go through each detected object and add them as saved components
			for (int i = 0; i < detectedObjects.Count; i++)
			{
				// Create the new object for this component
				SavedGunComponent savedGunComponent = new()
				{
					Index = i,
					ObjectID = detectedObjects[i].ObjectWrapper.ItemID,
					Flags = detectedObjects[i].GetFlagDic()
				};

				//If this is not the root item, we calculate all the offsets based on this object
				if (i > 0)
				{
					Transform transform = detectedObjects[0].transform;
					savedGunComponent.PosOffset = transform.InverseTransformPoint(detectedObjects[i].transform.position);
					savedGunComponent.OrientationForward = detectedObjects[i].transform.forward;
					savedGunComponent.OrientationUp = detectedObjects[i].transform.up;
				}

				switch (detectedObjects[i])
				{
					//If this is a firearm, we should try to save the state of the magazine
					case FVRFireArm firearmObj:
					{
						savedGunComponent.isFirearm = true;
						if (firearmObj.Magazine != null)
						{
							for (int j = 0; j < firearmObj.Magazine.m_numRounds; j++)
							{
								savedGun.LoadedRoundsInMag.Add(firearmObj.Magazine.LoadedRounds[j].LR_Class);
							}
						}

						break;
					}

					// If it's a magazine, set that flag
					case FVRFireArmMagazine:
						savedGunComponent.isMagazine = true;
						break;

					// If it's a clip, save it's rounds
					case FVRFireArmClip firearmClip:
					{
						for (int k = 0; k < firearmClip.m_numRounds; k++)
						{
							savedGun.LoadedRoundsInMag.Add(firearmClip.LoadedRounds[k].LR_Class);
						}

						break;
					}

					// If it's a speed loader save it's rounds
					case Speedloader speedloader:
					{
						foreach (SpeedloaderChamber c in speedloader.Chambers.Where(c => c.IsLoaded))
						{
							savedGun.LoadedRoundsInMag.Add(c.LoadedClass);
						}

						break;
					}

					// For each attachment, we want to save the object that it is attached to
					case FVRFireArmAttachment attachment:
					{
						savedGunComponent.isAttachment = true;
						FVRFireArmAttachmentMount curMount = attachment.curMount;
						FVRPhysicalObject myObject = curMount.MyObject;
						savedGunComponent.ObjectAttachedTo = detectedObjects.IndexOf(myObject);
						savedGunComponent.MountAttachedTo = myObject.AttachmentMounts.IndexOf(curMount);
						break;
					}
				}

				// Add the component to the saved gun
				savedGun.Components.Add(savedGunComponent);
			}

			// Save the last info
			savedGun.LoadedRoundsInChambers = firearm.GetChamberRoundList();
			savedGun.SavedFlags = firearm.GetFlagList();

			// Reset the rotation (again? Is this needed?)
			detectedObjects[0].transform.rotation = Quaternion.LookRotation(forward, up);

			// Return the saved gun
			return savedGun;
		}

		/// <summary>
		/// Spawns a vaulted gun based on the given saved gun object
		/// </summary>
		/// <remarks>
		///	This is a static adaptation of the compiled vault code. The contents have been tidied and documented
		/// </remarks>
		/// <param name="gun">The serializable version of a SavedGun object</param>
		/// <param name="position">The position the gun should spawn at</param>
		/// <param name="rotation">The rotation the gun should spawn with</param>
		public static void SpawnGun(SavedGun gun, Vector3 position, Quaternion rotation)
		{
			AnvilManager.Instance.StartCoroutine(SpawnGun_Internal(gun, position, rotation));
		}

		// TODO: This code is still super ugly since it's been adapted from the game's decompiled code.
		private static IEnumerator SpawnGun_Internal(SavedGun gun, Vector3 position, Quaternion rotation)
		{
			// Make sure we're safe to spawn it in
			if (!gun.AllComponentsLoaded()) throw new InvalidOperationException("This saved gun contains components that are not in the object dictionary!");

			FVRFireArm? baseGun = null;
			List<GameObject> attachments = new();
			List<GameObject> trayObjects = new();
			List<int> validIndexes = new();
			Dictionary<GameObject, SavedGunComponent> dicGO = new();
			Dictionary<int, GameObject> dicByIndex = new();

			// Make sure we've got all the objects used in this gun loaded
			List<AnvilCallback<GameObject>> callbackList = gun.Components.Select(c => IM.OD[c.ObjectID].GetGameObjectAsync()).ToList();
			yield return callbackList;


			//Spawn each of the components
			for (int j = 0; j < gun.Components.Count; j++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(callbackList[j].Result);
				dicGO.Add(gameObject, gun.Components[j]);
				dicByIndex.Add(gun.Components[j].Index, gameObject);

				//If this is the root firearm, apply some additional properties to it
				if (gun.Components[j].isFirearm)
				{
					baseGun = gameObject.GetComponent<FVRFireArm>();
					validIndexes.Add(j);
					gameObject.transform.position = position;
					gameObject.transform.rotation = Quaternion.identity;
				}

				//If this is a magazine, load it into the gun
				else if (gun.Components[j].isMagazine)
				{
					FVRFireArmMagazine myMagazine = gameObject.GetComponent<FVRFireArmMagazine>();
					validIndexes.Add(j);
					if (myMagazine != null)
					{
						if (baseGun is null) throw new NullReferenceException("Base gun component for vaulted gun was null");

						gameObject.transform.position = baseGun.GetMagMountPos(myMagazine.IsBeltBox).position;
						gameObject.transform.rotation = baseGun.GetMagMountPos(myMagazine.IsBeltBox).rotation;
						myMagazine.Load(baseGun);
						myMagazine.IsInfinite = false;
					}
				}

				//If this is an attachment, we deal with it later
				else if (gun.Components[j].isAttachment)
				{
					attachments.Add(gameObject);
				}

				//If this is a speedloader or clip, we load it with ammo
				else
				{
					trayObjects.Add(gameObject);
					if (gameObject.GetComponent<Speedloader>() != null && gun.LoadedRoundsInMag.Count > 0)
					{
						Speedloader component = gameObject.GetComponent<Speedloader>();
						component.ReloadSpeedLoaderWithList(gun.LoadedRoundsInMag);
					}
					else if (gameObject.GetComponent<FVRFireArmClip>() != null && gun.LoadedRoundsInMag.Count > 0)
					{
						FVRFireArmClip component2 = gameObject.GetComponent<FVRFireArmClip>();
						component2.ReloadClipWithList(gun.LoadedRoundsInMag);
					}
				}

				gameObject.GetComponent<FVRPhysicalObject>().ConfigureFromFlagDic(gun.Components[j].Flags);
			}


			if (baseGun is null) throw new NullReferenceException("Base gun component for vaulted gun was null");

			//If the gun has a magazine in it, fill it with ammo
			if (baseGun.Magazine != null && gun.LoadedRoundsInMag.Count > 0)
			{
				baseGun.Magazine.ReloadMagWithList(gun.LoadedRoundsInMag);
				baseGun.Magazine.IsInfinite = false;
			}


			//Attach all of the attachments to the gun
			int breakIterator = 200;
			while (attachments.Count > 0 && breakIterator > 0)
			{
				breakIterator--;
				for (int k = attachments.Count - 1; k >= 0; k--)
				{
					SavedGunComponent savedGunComponent = dicGO[attachments[k]];
					if (validIndexes.Contains(savedGunComponent.ObjectAttachedTo))
					{
						GameObject gameObject2 = attachments[k];
						FVRFireArmAttachment component3 = gameObject2.GetComponent<FVRFireArmAttachment>();
						FVRFireArmAttachmentMount mount = dicByIndex[savedGunComponent.ObjectAttachedTo].GetComponent<FVRPhysicalObject>()
							.AttachmentMounts[savedGunComponent.MountAttachedTo];
						gameObject2.transform.rotation = Quaternion.LookRotation(savedGunComponent.OrientationForward, savedGunComponent.OrientationUp);
						gameObject2.transform.position = GetPositionRelativeToGun(savedGunComponent, baseGun.transform);
						if (component3.CanScaleToMount && mount.CanThisRescale())
						{
							component3.ScaleToMount(mount);
						}

						component3.AttachToMount(mount, false);
						if (component3 is Suppressor suppressor)
						{
							suppressor.AutoMountWell();
						}

						validIndexes.Add(savedGunComponent.Index);
						attachments.RemoveAt(k);
					}
				}
			}

			//Move all the tray objects into position above the gun
			for (int l = 0; l < trayObjects.Count; l++)
			{
				trayObjects[l].transform.position = position + l * 0.1f * Vector3.up;
				trayObjects[l].transform.rotation = rotation;
			}

			baseGun.SetLoadedChambers(gun.LoadedRoundsInChambers);
			baseGun.SetFromFlagList(gun.SavedFlags);
			baseGun.transform.rotation = rotation;
		}

		private static Vector3 GetPositionRelativeToGun(SavedGunComponent data, Transform gun)
		{
			Vector3 a = gun.position;
			a += gun.up * data.PosOffset.y;
			a += gun.right * data.PosOffset.x;
			return a + gun.forward * data.PosOffset.z;
		}
	}
}

using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sodalite
{
	public static class VaultUtils
	{


		/// <summary>
		/// Scans the given firearm and creates a vault file based on everything that is attached
		/// </summary>
		/// <remarks>
		/// This is a static adaptation of the compiled vault code. The contents have been tidied and documented
		/// </remarks>
		/// <param name="fireArm">The firearm for which the vault file is being created</param>
		/// <returns>A SavedGunSerializable which represents the vault file for the given firearm</returns>
		public static SavedGunSerializable CreateVaultFile(FVRFireArm fireArm)
		{
			List<FVRPhysicalObject> detectedObjects = FVRObjectExtensions.GetAllAttachedObjects(fireArm, true);

			SavedGun savedGun = new SavedGun();
			savedGun.DateMade = DateTime.Now;

			Vector3 forward = detectedObjects[0].transform.forward;
			Vector3 up = detectedObjects[0].transform.up;
			detectedObjects[0].transform.rotation = Quaternion.identity;


			//Go through each detected object and add them as saved components
			for (int i = 0; i < detectedObjects.Count; i++)
			{

				SavedGunComponent savedGunComponent = new SavedGunComponent();
				savedGunComponent.Index = i;
				savedGunComponent.ObjectID = detectedObjects[i].ObjectWrapper.ItemID;

				Dictionary<string, string> flagDic = detectedObjects[i].GetFlagDic();
				if (flagDic != null)
				{
					savedGunComponent.Flags = flagDic;
				}

				//If this is the root item, we calculate all the offsets based on this object
				if (i > 0)
				{
					Transform transform = detectedObjects[0].transform;
					savedGunComponent.PosOffset = transform.InverseTransformPoint(detectedObjects[i].transform.position);
					savedGunComponent.OrientationForward = detectedObjects[i].transform.forward;
					savedGunComponent.OrientationUp = detectedObjects[i].transform.up;
				}

				//If this is a firearm, we should try to save the state of the magazine
				if (detectedObjects[i] is FVRFireArm fvrfireArm)
				{
					savedGunComponent.isFirearm = true;
					if (fvrfireArm.Magazine != null)
					{
						for (int j = 0; j < fvrfireArm.Magazine.m_numRounds; j++)
						{
							savedGun.LoadedRoundsInMag.Add(fvrfireArm.Magazine.LoadedRounds[j].LR_Class);
						}
					}
				}


				else if (detectedObjects[i] is FVRFireArmMagazine)
				{
					savedGunComponent.isMagazine = true;
				}


				else if (detectedObjects[i] is FVRFireArmClip fvrfireArmClip)
				{
					for (int k = 0; k < fvrfireArmClip.m_numRounds; k++)
					{
						savedGun.LoadedRoundsInMag.Add(fvrfireArmClip.LoadedRounds[k].LR_Class);
					}
				}


				else if (detectedObjects[i] is Speedloader speedloader)
				{
					for (int l = 0; l < speedloader.Chambers.Count; l++)
					{
						if (speedloader.Chambers[l].IsLoaded)
						{
							savedGun.LoadedRoundsInMag.Add(speedloader.Chambers[l].LoadedClass);
						}
					}
				}


				//For each attachment, we want to save the object that it is attached to
				else if (detectedObjects[i] is FVRFireArmAttachment fvrfireArmAttachment)
				{
					savedGunComponent.isAttachment = true;
					FVRFireArmAttachmentMount curMount = fvrfireArmAttachment.curMount;
					FVRPhysicalObject myObject = curMount.MyObject;
					savedGunComponent.ObjectAttachedTo = detectedObjects.IndexOf(myObject);
					savedGunComponent.MountAttachedTo = myObject.AttachmentMounts.IndexOf(curMount);
				}

				savedGun.Components.Add(savedGunComponent);
			}


			List<FireArmRoundClass> chamberRoundList = fireArm.GetChamberRoundList();
			List<string> flagList = fireArm.GetFlagList();
			if (chamberRoundList != null)
			{
				savedGun.LoadedRoundsInChambers = chamberRoundList;
			}
			if (flagList != null)
			{
				savedGun.SavedFlags = flagList;
			}

			detectedObjects[0].transform.rotation = Quaternion.LookRotation(forward, up);

			return new SavedGunSerializable(savedGun, fireArm);
		}




		/// <summary>
		/// Spawns a vaulted gun based on the given saved gun object
		/// </summary>
		/// <remarks>
		///	This is a static adaptation of the compiled vault code. The contents have been tidied and documented
		/// </remarks>
		/// <param name="savedGun">The serializable version of a SavedGun object</param>
		/// <param name="position">The position the gun should spawn at</param>
		/// <param name="rotation">The rotation the gun should spawn with</param>
		public static IEnumerator SpawnVaultedGun(SavedGunSerializable savedGun, Vector3 position, Quaternion rotation)
		{
			FVRFireArm? baseGun = null;

			List<GameObject> attachments = new List<GameObject>();
			List<GameObject> trayObjects = new List<GameObject>();
			List<int> validIndexes = new List<int>();
			Dictionary<GameObject, SavedGunComponent> dicGO = new Dictionary<GameObject, SavedGunComponent>();
			Dictionary<int, GameObject> dicByIndex = new Dictionary<int, GameObject>();
			List<AnvilCallback<GameObject>> callbackList = new List<AnvilCallback<GameObject>>();

			SavedGun gun = savedGun.GetSavedGun();

			//Go through all components of the gun, and get each gameobject
			for (int i = 0; i < gun.Components.Count; i++)
			{
				callbackList.Add(IM.OD[gun.Components[i].ObjectID].GetGameObjectAsync());
			}
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
					savedGun.ApplyFirearmProperties(baseGun);

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


				//If this is a apeedloader or clip, we load it with ammo
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
			int BreakIterator = 200;
			while (attachments.Count > 0 && BreakIterator > 0)
			{
				BreakIterator--;
				for (int k = attachments.Count - 1; k >= 0; k--)
				{
					SavedGunComponent savedGunComponent = dicGO[attachments[k]];
					if (validIndexes.Contains(savedGunComponent.ObjectAttachedTo))
					{
						GameObject gameObject2 = attachments[k];
						FVRFireArmAttachment component3 = gameObject2.GetComponent<FVRFireArmAttachment>();
						FVRFireArmAttachmentMount mount = GetMount(dicByIndex[savedGunComponent.ObjectAttachedTo], savedGunComponent.MountAttachedTo);
						gameObject2.transform.rotation = Quaternion.LookRotation(savedGunComponent.OrientationForward, savedGunComponent.OrientationUp);
						gameObject2.transform.position = GetPositionRelativeToGun(savedGunComponent, baseGun.transform);
						if (component3.CanScaleToMount && mount.CanThisRescale())
						{
							component3.ScaleToMount(mount);
						}
						component3.AttachToMount(mount, false);
						if (component3 is Suppressor)
						{
							((Suppressor)component3).AutoMountWell();
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
			yield break;
		}



		/// <summary>
		/// Returns true if the given saved gun (vault file) has all of its components loaded
		/// </summary>
		/// <remarks>
		/// This is primarily useful for verifying that vault files with modded items can be spawned
		/// </remarks>
		/// <param name="savedGun">The serializable version of a SavedGun object (vault file)</param>
		/// <returns>True if everything is loaded. False otherwise</returns>
		public static bool AllComponentsLoaded(SavedGunSerializable savedGun)
		{
			foreach (SavedGunComponentSerializable component in savedGun.Components)
			{
				if (!IM.OD.ContainsKey(component.ObjectID))
				{
					return false;
				}
			}
			return true;
		}



		/// <summary>
		/// Returns true if the given saved gun (vault file) has all of its components loaded
		/// </summary>
		/// <remarks>
		/// This is primarily useful for verifying that vault files with modded items can be spawned
		/// </remarks>
		/// <param name="savedGun">The SavedGun object (vault file)</param>
		/// <returns>True if everything is loaded. False otherwise</returns>
		public static bool AllComponentsLoaded(SavedGun savedGun)
		{
			foreach (SavedGunComponent component in savedGun.Components)
			{
				if (!IM.OD.ContainsKey(component.ObjectID))
				{
					return false;
				}
			}
			return true;
		}



		private static FVRFireArmAttachmentMount GetMount(GameObject obj, int index)
		{
			return obj.GetComponent<FVRPhysicalObject>().AttachmentMounts[index];
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

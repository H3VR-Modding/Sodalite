using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sodalite
{


	/// <summary>
	/// A serializable version of the SavedGun object (vault file). Useful for saving vault files to text files such as json.
	/// A SavedGun object can be converted to this serializable version by passing it as a parameter to this objects constructor
	/// </summary>
	public class SavedGunSerializable
	{
		public string FileName;
		public List<FireArmRoundClass> LoadedRoundsInMag;
		public List<FireArmRoundClass> LoadedRoundsInChambers;
		public List<string> SavedFlags;

		public bool overrideFireRate;
		public bool overrideFireSelectors;

		public float springSpeedForward;
		public float springSpeedRearward;
		public float springStiffness;

		public List<FireSelectorMode> fireSelectorModes = new List<FireSelectorMode>();

		public List<SavedGunComponentSerializable> Components;


		public SavedGunSerializable(SavedGun gun)
		{
			FileName = gun.FileName;
			Components = gun.Components.Select(o => new SavedGunComponentSerializable(o)).ToList();
			LoadedRoundsInMag = gun.LoadedRoundsInMag;
			LoadedRoundsInChambers = gun.LoadedRoundsInChambers;
			SavedFlags = gun.SavedFlags;

			LoadFirearmProperties(gun);
		}



		public SavedGun GetSavedGun()
		{
			SavedGun gun = new SavedGun();
			gun.FileName = FileName;
			gun.Components = Components.Select(o => o.GetGunComponent()).ToList();
			gun.LoadedRoundsInMag = LoadedRoundsInMag;
			gun.LoadedRoundsInChambers = LoadedRoundsInChambers;
			gun.SavedFlags = SavedFlags;
			gun.DateMade = default(DateTime);

			return gun;
		}


		/// <summary>
		/// Returns the base gun object of the vault file
		/// </summary>
		/// <returns>An FVRObject for the base gun component</returns>
		/// <exception cref="NullReferenceException">Throws when no base gun is found</exception>
		public FVRObject GetGunObject()
		{
			foreach (SavedGunComponentSerializable component in Components)
			{
				if (component.IsFirearm) return IM.OD[component.ObjectID];
			}

			throw new NullReferenceException("Vault file has no base gun object!");
		}


		/// <summary>
		/// Loads additional firearm properties into this SavedGunSerializable based on the root gun of the given vault file
		/// </summary>
		/// <param name="gun">The SavedGun object (vault file)</param>
		private void LoadFirearmProperties(SavedGun gun)
		{
			FVRFireArm firearmComp = GetGunObject().GetGameObject().GetComponent<FVRFireArm>();
			LoadFirearmProperties(firearmComp);
		}


		/// <summary>
		/// Loads additional firearm properties into this SavedGunSerializable based on the given firearm object
		/// </summary>
		/// <param name="firearm">The firearm object whose properties will be applied to this object</param>
		private void LoadFirearmProperties(FVRFireArm firearm)
		{
			if (firearm is Handgun handgun)
			{
				foreach (Handgun.FireSelectorMode mode in handgun.FireSelectorModes)
				{
					fireSelectorModes.Add(new FireSelectorMode(mode));
				}

				springSpeedForward = handgun.Slide.Speed_Forward;
				springSpeedRearward = handgun.Slide.Speed_Rearward;
				springStiffness = handgun.Slide.SpringStiffness;

				return;
			}

			if (firearm is ClosedBoltWeapon closedBolt)
			{
				foreach (ClosedBoltWeapon.FireSelectorMode mode in closedBolt.FireSelector_Modes)
				{
					fireSelectorModes.Add(new FireSelectorMode(mode));
				}

				springSpeedForward = closedBolt.Bolt.Speed_Forward;
				springSpeedRearward = closedBolt.Bolt.Speed_Rearward;
				springStiffness = closedBolt.Bolt.SpringStiffness;

				return;
			}

			if (firearm is OpenBoltReceiver openBolt)
			{
				foreach (OpenBoltReceiver.FireSelectorMode mode in openBolt.FireSelector_Modes)
				{
					fireSelectorModes.Add(new FireSelectorMode(mode));
				}

				springSpeedForward = openBolt.Bolt.BoltSpeed_Forward;
				springSpeedRearward = openBolt.Bolt.BoltSpeed_Rearward;
				springStiffness = openBolt.Bolt.BoltSpringStiffness;

				return;
			}
		}


		/// <summary>
		/// Applies the saved firearm properties from this vault file into the given firearm object
		/// </summary>
		/// <param name="firearm">The firearm that is having this SavedGunSerializables properties applied to it</param>
		public void ApplyFirearmProperties(FVRFireArm firearm)
		{
			if (!overrideFireRate && !overrideFireSelectors) return;

			if (firearm is Handgun handgun)
			{

				if (overrideFireSelectors)
				{
					List<Handgun.FireSelectorMode> modeList = new List<Handgun.FireSelectorMode>();
					foreach (FireSelectorMode mode in fireSelectorModes)
					{
						modeList.Add(mode.GetHandgunMode());
					}
					handgun.FireSelectorModes = modeList.ToArray();
				}

				if (overrideFireRate)
				{
					handgun.Slide.Speed_Forward = springSpeedForward;
					handgun.Slide.Speed_Rearward = springSpeedRearward;
					handgun.Slide.SpringStiffness = springStiffness;
				}

				return;
			}

			if (firearm is ClosedBoltWeapon closedBolt)
			{
				if (overrideFireSelectors)
				{
					List<ClosedBoltWeapon.FireSelectorMode> modeList = new List<ClosedBoltWeapon.FireSelectorMode>();
					foreach (FireSelectorMode mode in fireSelectorModes)
					{
						modeList.Add(mode.GetClosedBoltMode());
					}
					closedBolt.FireSelector_Modes = modeList.ToArray();
				}

				if (overrideFireRate)
				{
					closedBolt.Bolt.Speed_Forward = springSpeedForward;
					closedBolt.Bolt.Speed_Rearward = springSpeedRearward;
					closedBolt.Bolt.SpringStiffness = springStiffness;
				}

				return;
			}

			if (firearm is OpenBoltReceiver openBolt)
			{
				if (overrideFireSelectors)
				{
					List<OpenBoltReceiver.FireSelectorMode> modeList = new List<OpenBoltReceiver.FireSelectorMode>();
					foreach (FireSelectorMode mode in fireSelectorModes)
					{
						modeList.Add(mode.GetOpenBoltMode());
					}
					openBolt.FireSelector_Modes = modeList.ToArray();
				}

				if (overrideFireRate)
				{
					openBolt.Bolt.BoltSpeed_Forward = springSpeedForward;
					openBolt.Bolt.BoltSpeed_Rearward = springSpeedRearward;
					openBolt.Bolt.BoltSpringStiffness = springStiffness;
				}

				return;
			}
		}
	}


	/// <summary>
	/// A serializable version of the SavedGunComponent object
	/// </summary>
	public class SavedGunComponentSerializable
	{
		public int Index;
		public string ObjectID;
		public Vector3Serializable PosOffset;
		public Vector3Serializable OrientationForward;
		public Vector3Serializable OrientationUp;
		public int ObjectAttachedTo;
		public int MountAttachedTo;
		public bool IsFirearm;
		public bool IsMagazine;
		public bool IsAttachment;
		public Dictionary<string, string> Flags;


		public SavedGunComponentSerializable(SavedGunComponent component)
		{
			Index = component.Index;
			ObjectID = component.ObjectID;
			PosOffset = new Vector3Serializable(component.PosOffset);
			OrientationForward = new Vector3Serializable(component.OrientationForward);
			OrientationUp = new Vector3Serializable(component.OrientationUp);
			ObjectAttachedTo = component.ObjectAttachedTo;
			MountAttachedTo = component.MountAttachedTo;
			IsFirearm = component.isFirearm;
			IsMagazine = component.isMagazine;
			IsAttachment = component.isAttachment;
			Flags = component.Flags;

		}

		public SavedGunComponent GetGunComponent()
		{
			SavedGunComponent component = new SavedGunComponent();

			component.Index = Index;
			component.ObjectID = ObjectID;
			component.PosOffset = PosOffset.GetVector3();
			component.OrientationForward = OrientationForward.GetVector3();
			component.OrientationUp = OrientationUp.GetVector3();
			component.ObjectAttachedTo = ObjectAttachedTo;
			component.MountAttachedTo = MountAttachedTo;
			component.isFirearm = IsFirearm;
			component.isMagazine = IsMagazine;
			component.isAttachment = IsAttachment;
			component.Flags = Flags;
				
			return component;
		}

	}


	/// <summary>
	/// This object unifies the fire selector modes of three different firearm classes under a single class
	/// </summary>
	public class FireSelectorMode
	{
		public float selectorPosition;
		public FireSelectorModeType modeType;
		public int burstAmount;

		public FireSelectorMode() { }

		public FireSelectorMode(Handgun.FireSelectorMode mode)
		{
			selectorPosition = mode.SelectorPosition;
			modeType = (FireSelectorModeType)Enum.Parse(typeof(FireSelectorModeType), mode.ModeType.ToString());
			burstAmount = mode.BurstAmount;
		}

		public FireSelectorMode(ClosedBoltWeapon.FireSelectorMode mode)
		{
			selectorPosition = mode.SelectorPosition;
			modeType = (FireSelectorModeType)Enum.Parse(typeof(FireSelectorModeType), mode.ModeType.ToString());
			burstAmount = mode.BurstAmount;
		}

		public FireSelectorMode(OpenBoltReceiver.FireSelectorMode mode)
		{
			selectorPosition = mode.SelectorPosition;
			modeType = (FireSelectorModeType)Enum.Parse(typeof(FireSelectorModeType), mode.ModeType.ToString());
			burstAmount = -1;
		}

		public Handgun.FireSelectorMode GetHandgunMode()
		{
			Handgun.FireSelectorMode mode = new Handgun.FireSelectorMode();
			mode.SelectorPosition = selectorPosition;
			mode.ModeType = (Handgun.FireSelectorModeType)Enum.Parse(typeof(Handgun.FireSelectorModeType), modeType.ToString());
			mode.BurstAmount = burstAmount;
			return mode;
		}

		public OpenBoltReceiver.FireSelectorMode GetOpenBoltMode()
		{
			OpenBoltReceiver.FireSelectorMode mode = new OpenBoltReceiver.FireSelectorMode();
			mode.SelectorPosition = selectorPosition;
			mode.ModeType = (OpenBoltReceiver.FireSelectorModeType)Enum.Parse(typeof(OpenBoltReceiver.FireSelectorModeType), modeType.ToString());
			return mode;
		}

		public ClosedBoltWeapon.FireSelectorMode GetClosedBoltMode()
		{
			ClosedBoltWeapon.FireSelectorMode mode = new ClosedBoltWeapon.FireSelectorMode();
			mode.SelectorPosition = selectorPosition;
			mode.ModeType = (ClosedBoltWeapon.FireSelectorModeType)Enum.Parse(typeof(ClosedBoltWeapon.FireSelectorModeType), modeType.ToString());
			mode.BurstAmount = burstAmount;
			return mode;
		}
	}


	public enum FireSelectorModeType
	{
		Safe,
		Single,
		Burst,
		FullAuto,
		SuperFastBurst
	}

}


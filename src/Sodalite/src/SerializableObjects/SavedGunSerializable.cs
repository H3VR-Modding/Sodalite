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


		/// <summary>
		/// When true, spawning this vaulted gun will apply changes to fire rate (spring properties)
		/// </summary>
		public bool OverrideFireRate;

		/// <summary>
		/// When true, spawning this vaulted gun will override the guns fire selectors
		/// </summary>
		public bool OverrideFireSelectors;

		/// <summary>
		/// The speed at which the guns main spring moves forward
		/// </summary>
		/// <remarks>
		/// Only applied when OverrideFireRate is true
		/// </remarks>
		public float SpeedForward;

		/// <summary>
		/// The speed at which the guns main spring moves rearward
		/// </summary>
		/// <remarks>
		/// Only applied when OverrideFireRate is true
		/// </remarks>
		public float SpeedRearward;

		/// <summary>
		/// The overall stiffness of firearms main spring. Higher values mean more spring tension
		/// </summary>
		/// <remarks>
		/// Only applied when OverrideFireRate is true
		/// </remarks>
		public float SpringStiffness;

		/// <summary>
		/// A list of fire selector modes for the gun
		/// </summary>
		/// <remarks>
		/// Only applied when OverrideFireSelectors is true
		/// </remarks>
		public List<FireSelectorMode> FireSelectorModes = new List<FireSelectorMode>();

		/// <summary>
		/// A list of components which make up the vaulted gun
		/// </summary>
		public List<SavedGunComponentSerializable> Components;


		/// <summary>
		/// Creates a serializable version of the given SavedGunObject
		/// </summary>
		/// <remarks>
		/// Passing the firearm component for the vault file is slightly faster than just passing the SavedGun object
		/// </remarks>
		/// <param name="gun">The SavedGun (vault file) that this object will be based off of</param>
		/// <param name="firearm">A reference to the base firearm for the vault file, from which properties like firerate will be taken</param>
		public SavedGunSerializable(SavedGun gun, FVRFireArm? firearm = null)
		{
			FileName = gun.FileName;
			Components = gun.Components.Select(o => new SavedGunComponentSerializable(o)).ToList();
			LoadedRoundsInMag = gun.LoadedRoundsInMag;
			LoadedRoundsInChambers = gun.LoadedRoundsInChambers;
			SavedFlags = gun.SavedFlags;

			if (firearm is not null) LoadFirearmProperties(firearm);
			else LoadFirearmProperties(gun);
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
					FireSelectorModes.Add(new FireSelectorMode(mode));
				}

				SpeedForward = handgun.Slide.Speed_Forward;
				SpeedRearward = handgun.Slide.Speed_Rearward;
				SpringStiffness = handgun.Slide.SpringStiffness;

				return;
			}

			if (firearm is ClosedBoltWeapon closedBolt)
			{
				foreach (ClosedBoltWeapon.FireSelectorMode mode in closedBolt.FireSelector_Modes)
				{
					FireSelectorModes.Add(new FireSelectorMode(mode));
				}

				SpeedForward = closedBolt.Bolt.Speed_Forward;
				SpeedRearward = closedBolt.Bolt.Speed_Rearward;
				SpringStiffness = closedBolt.Bolt.SpringStiffness;

				return;
			}

			if (firearm is OpenBoltReceiver openBolt)
			{
				foreach (OpenBoltReceiver.FireSelectorMode mode in openBolt.FireSelector_Modes)
				{
					FireSelectorModes.Add(new FireSelectorMode(mode));
				}

				SpeedForward = openBolt.Bolt.BoltSpeed_Forward;
				SpeedRearward = openBolt.Bolt.BoltSpeed_Rearward;
				SpringStiffness = openBolt.Bolt.BoltSpringStiffness;

				return;
			}
		}


		/// <summary>
		/// Applies the saved firearm properties from this vault file into the given firearm object
		/// </summary>
		/// <param name="firearm">The firearm that is having this SavedGunSerializables properties applied to it</param>
		public void ApplyFirearmProperties(FVRFireArm firearm)
		{
			if (!OverrideFireRate && !OverrideFireSelectors) return;

			if (firearm is Handgun handgun)
			{

				if (OverrideFireSelectors)
				{
					List<Handgun.FireSelectorMode> modeList = new List<Handgun.FireSelectorMode>();
					foreach (FireSelectorMode mode in FireSelectorModes)
					{
						modeList.Add(mode.GetHandgunMode());
					}

					handgun.FireSelectorModes = modeList.ToArray();
				}

				if (OverrideFireRate)
				{
					handgun.Slide.Speed_Forward = SpeedForward;
					handgun.Slide.Speed_Rearward = SpeedRearward;
					handgun.Slide.SpringStiffness = SpringStiffness;
				}

				return;
			}

			if (firearm is ClosedBoltWeapon closedBolt)
			{
				if (OverrideFireSelectors)
				{
					List<ClosedBoltWeapon.FireSelectorMode> modeList = new List<ClosedBoltWeapon.FireSelectorMode>();
					foreach (FireSelectorMode mode in FireSelectorModes)
					{
						modeList.Add(mode.GetClosedBoltMode());
					}

					closedBolt.FireSelector_Modes = modeList.ToArray();
				}

				if (OverrideFireRate)
				{
					closedBolt.Bolt.Speed_Forward = SpeedForward;
					closedBolt.Bolt.Speed_Rearward = SpeedRearward;
					closedBolt.Bolt.SpringStiffness = SpringStiffness;
				}

				return;
			}

			if (firearm is OpenBoltReceiver openBolt)
			{
				if (OverrideFireSelectors)
				{
					List<OpenBoltReceiver.FireSelectorMode> modeList = new List<OpenBoltReceiver.FireSelectorMode>();
					foreach (FireSelectorMode mode in FireSelectorModes)
					{
						modeList.Add(mode.GetOpenBoltMode());
					}

					openBolt.FireSelector_Modes = modeList.ToArray();
				}

				if (OverrideFireRate)
				{
					openBolt.Bolt.BoltSpeed_Forward = SpeedForward;
					openBolt.Bolt.BoltSpeed_Rearward = SpeedRearward;
					openBolt.Bolt.BoltSpringStiffness = SpringStiffness;
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
		public SerializableVector3 PosOffset;
		public SerializableVector3 OrientationForward;
		public SerializableVector3 OrientationUp;
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
			PosOffset = component.PosOffset;
			OrientationForward = component.OrientationForward;
			OrientationUp = component.OrientationUp;
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
			component.PosOffset = PosOffset;
			component.OrientationForward = OrientationForward;
			component.OrientationUp = OrientationUp;
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

		public FireSelectorMode()
		{
		}

		public FireSelectorMode(Handgun.FireSelectorMode mode)
		{
			selectorPosition = mode.SelectorPosition;
			modeType = (FireSelectorModeType) Enum.Parse(typeof(FireSelectorModeType), mode.ModeType.ToString());
			burstAmount = mode.BurstAmount;
		}

		public FireSelectorMode(ClosedBoltWeapon.FireSelectorMode mode)
		{
			selectorPosition = mode.SelectorPosition;
			modeType = (FireSelectorModeType) Enum.Parse(typeof(FireSelectorModeType), mode.ModeType.ToString());
			burstAmount = mode.BurstAmount;
		}

		public FireSelectorMode(OpenBoltReceiver.FireSelectorMode mode)
		{
			selectorPosition = mode.SelectorPosition;
			modeType = (FireSelectorModeType) Enum.Parse(typeof(FireSelectorModeType), mode.ModeType.ToString());
			burstAmount = -1;
		}

		public Handgun.FireSelectorMode GetHandgunMode()
		{
			Handgun.FireSelectorMode mode = new Handgun.FireSelectorMode();
			mode.SelectorPosition = selectorPosition;
			mode.ModeType = (Handgun.FireSelectorModeType) Enum.Parse(typeof(Handgun.FireSelectorModeType), modeType.ToString());
			mode.BurstAmount = burstAmount;
			return mode;
		}

		public OpenBoltReceiver.FireSelectorMode GetOpenBoltMode()
		{
			OpenBoltReceiver.FireSelectorMode mode = new OpenBoltReceiver.FireSelectorMode();
			mode.SelectorPosition = selectorPosition;
			mode.ModeType = (OpenBoltReceiver.FireSelectorModeType) Enum.Parse(typeof(OpenBoltReceiver.FireSelectorModeType), modeType.ToString());
			return mode;
		}

		public ClosedBoltWeapon.FireSelectorMode GetClosedBoltMode()
		{
			ClosedBoltWeapon.FireSelectorMode mode = new ClosedBoltWeapon.FireSelectorMode();
			mode.SelectorPosition = selectorPosition;
			mode.ModeType = (ClosedBoltWeapon.FireSelectorModeType) Enum.Parse(typeof(ClosedBoltWeapon.FireSelectorModeType), modeType.ToString());
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

﻿using System;
using System.Collections.Generic;
using FistVR;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Sodalite.Api
{
	/// <summary>
	///		Sodalite Sosig API for spawning Sosigs.
	/// </summary>
	public class SosigAPI
	{
		/// <summary>
		///		Spawns a Sosig with the given template, options position and rotation.
		/// </summary>
		/// <remarks>
		///		This code has been adapted from the game's SosigSpawner class. The contents have been tidied and documented.
		/// </remarks>
		/// <param name="template">The Sosig Enemy Template to spawn from. This defines the equipment, accessories, etc.</param>
		/// <param name="spawnOptions">The spawning options such as the Sosig's IFF, activation and general state</param>
		/// <param name="position">The position to spawn the Sosig at</param>
		/// <param name="rotation">The rotation to spawn the Sosig with</param>
		/// <returns>The spawned Sosig object</returns>
		public Sosig Spawn(SosigEnemyTemplate template, SpawnOptions spawnOptions, Vector3 position, Quaternion rotation)
		{
			// Get all our objects ready
			FVRObject prefab = template.SosigPrefabs[Random.Range(0, template.SosigPrefabs.Count)];
			SosigConfigTemplate configTemplate = template.ConfigTemplates[Random.Range(0, template.ConfigTemplates.Count)];
			SosigOutfitConfig outfitConfig = template.OutfitConfig[Random.Range(0, template.OutfitConfig.Count)];
			Sosig sosig = SpawnSosigAndConfigureSosig(prefab.GetGameObject(), position, rotation, configTemplate, outfitConfig);

			// Initialize some stuff
			sosig.InitHands();
			sosig.Inventory.Init();

			void SpawnWeapon(IList<FVRObject> o)
			{
				// Spawn and return the SosigWeapon object
				GameObject weaponObj = o[Random.Range(0, o.Count)].GetGameObject();
				SosigWeapon weapon = Object.Instantiate(weaponObj).GetComponent<SosigWeapon>();
				weapon.SetAutoDestroy(true);
				sosig.ForceEquip(weapon);
				if (weapon.Type == SosigWeapon.SosigWeaponType.Gun && spawnOptions.SpawnWithFullAmmo)
					sosig.Inventory.FillAmmoWithType(weapon.AmmoType);
			}

			// Spawn the primary weapon
			bool spawnWithPrimaryWeapon = spawnOptions.EquipmentMode.HasFlag(SpawnOptions.EquipmentSlots.Primary);
			if (template.WeaponOptions.Count > 0 && spawnWithPrimaryWeapon) SpawnWeapon(template.WeaponOptions);

			// Spawn the secondary weapon
			bool spawnWithSecondaryWeapon = spawnOptions.EquipmentMode.HasFlag(SpawnOptions.EquipmentSlots.Secondary) && Random.Range(0.0f, 1f) >= template.SecondaryChance;
			if (template.WeaponOptions_Secondary.Count > 0 && spawnWithSecondaryWeapon) SpawnWeapon(template.WeaponOptions_Secondary);

			// Spawn the tertiary weapon
			bool spawnWithTertiaryWeapon = spawnOptions.EquipmentMode.HasFlag(SpawnOptions.EquipmentSlots.Tertiary) && Random.Range(0.0f, 1f) >= template.TertiaryChance;
			if (template.WeaponOptions_Tertiary.Count > 0 && spawnWithTertiaryWeapon) SpawnWeapon(template.WeaponOptions_Tertiary);

			// Set the IFF (team) of the Sosig
			int sosigIFF = spawnOptions.IFF;
			if (sosigIFF >= 5) sosigIFF = Random.Range(6, 10000);
			sosig.E.IFFCode = sosigIFF;

			// Set the Sosig's order
			sosig.CurrentOrder = Sosig.SosigOrder.Disabled;
			sosig.FallbackOrder = spawnOptions.SpawnState;
			if (spawnOptions.SpawnActivated) sosig.SetCurrentOrder(sosig.FallbackOrder);

			// Set the Sosig's guard and assault points
			sosig.UpdateGuardPoint(spawnOptions.SosigTargetPosition);
			sosig.SetDominantGuardDirection(spawnOptions.SosigTargetRotation);
			sosig.UpdateAssaultPoint(spawnOptions.SosigTargetPosition);

			// Return the Sosig object
			return sosig;
		}

		private static Sosig SpawnSosigAndConfigureSosig(GameObject prefab, Vector3 pos, Quaternion rot, SosigConfigTemplate template, SosigOutfitConfig outfit)
		{
			// Get the Sosig component
			Sosig sosig = Object.Instantiate(prefab, pos, rot).GetComponentInChildren<Sosig>();

			// Spawn the accessories
			if (Random.Range(0.0f, 1f) < outfit.Chance_Headwear) SpawnAccessoryToLink(outfit.Headwear, sosig.Links[0]);
			if (Random.Range(0.0f, 1f) < outfit.Chance_Facewear) SpawnAccessoryToLink(outfit.Facewear, sosig.Links[0]);
			if (Random.Range(0.0f, 1f) < outfit.Chance_Eyewear) SpawnAccessoryToLink(outfit.Eyewear, sosig.Links[0]);
			if (Random.Range(0.0f, 1f) < outfit.Chance_Torsowear) SpawnAccessoryToLink(outfit.Torsowear, sosig.Links[1]);
			if (Random.Range(0.0f, 1f) < outfit.Chance_Pantswear) SpawnAccessoryToLink(outfit.Pantswear, sosig.Links[2]);
			if (Random.Range(0.0f, 1f) < outfit.Chance_Pantswear_Lower) SpawnAccessoryToLink(outfit.Pantswear_Lower, sosig.Links[3]);
			if (Random.Range(0.0f, 1f) < outfit.Chance_Backpacks) SpawnAccessoryToLink(outfit.Backpacks, sosig.Links[1]);

			// If the Sosig spawns an item when it's link is destroyed register that
			if (template.UsesLinkSpawns)
			{
				for (int i = 0; i < sosig.Links.Count; ++i)
				{
					if (Random.Range(0.0f, 1f) < template.LinkSpawnChance[i])
						sosig.Links[i].RegisterSpawnOnDestroy(template.LinkSpawns[i]);
				}
			}

			// Configure and return this sosig
			sosig.Configure(template);
			return sosig;

			static void SpawnAccessoryToLink(IList<FVRObject> gs, SosigLink l)
			{
				// Spawn the accessory and parent it to the sosig link
				if (gs.Count < 1) return;
				Transform linkTransform = l.transform;
				GameObject accessory = Object.Instantiate(gs[Random.Range(0, gs.Count)].GetGameObject(), linkTransform.position, linkTransform.rotation, linkTransform);
				accessory.GetComponent<SosigWearable>().RegisterWearable(l);
			}
		}

		// ReSharper disable once ClassNeverInstantiated.Global
		public class SpawnOptions
		{
			[Flags]
			public enum EquipmentSlots
			{
				Primary,
				Secondary,
				Tertiary,
				All = Primary | Secondary | Tertiary
			}

			// ReSharper disable UnusedAutoPropertyAccessor.Global
			// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
			public bool SpawnActivated { get; set; } = false;
			public int IFF { get; set; } = 0;
			public bool SpawnWithFullAmmo { get; set; } = true;
			public EquipmentSlots EquipmentMode { get; set; } = EquipmentSlots.All;
			public Sosig.SosigOrder SpawnState { get; set; } = Sosig.SosigOrder.Disabled;
			public Vector3 SosigTargetPosition { get; set; } = Vector3.zero;
			public Vector3 SosigTargetRotation { get; set; } = Vector3.zero;
			// ReSharper restore UnusedAutoPropertyAccessor.Global
			// ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
		}
	}
}
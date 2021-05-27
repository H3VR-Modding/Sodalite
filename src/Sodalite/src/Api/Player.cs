using System.Collections.Generic;
using FistVR;
using UnityEngine;
using FVRPhysicalObject		=	FistVR.FVRPhysicalObject;
using FVRPlayerBody			=	FistVR.FVRPlayerBody;
using FVRViveHand			=	FistVR.FVRViveHand;
using GM					=	FistVR.GM;

namespace Sodalite.Api
{
	/// <summary>
	/// API for interacting with the player
	/// <remarks>
	///	This entire thing is basically a big abstraction of <code>FVRPlayerBody</code>
	/// </remarks>
	/// </summary>
	public class PlayerAPI
	{
		#region Properties
		/// <summary>
		/// Used in the PlayerHands Dictionary - Positions of the hands
		/// </summary>
		public enum HandType
		{
			LeftHand,
			RightHand
		}
		/// <summary>
		/// Current health of the player
		/// </summary>
		public float PlayerHealth
		{
			get => GM.GetPlayerHealth();
			set => PlayerBody.Health = value;
		}

		/// <summary>
		/// Dictionary of the left and right hands of the player
		/// <example>
		///	<code>
		/// PlayerAPI.PlayerHands[...]...
		/// </code>
		/// </example>
		/// </summary>
		public Dictionary<HandType, FVRViveHand> PlayerHands => new()
		{
			{
				HandType.LeftHand,
				PlayerBody.LeftHand.gameObject.GetComponent<FVRViveHand>()
			},
			{
				HandType.RightHand,
				PlayerBody.RightHand.gameObject.GetComponent<FVRViveHand>()
			}
		};

		/// <summary>
		/// Body of the player
		/// </summary>
		public FVRPlayerBody PlayerBody => GM.CurrentPlayerBody;

		/// <summary>
		/// Current objects (both hands) the player is holding. Returns a dictionary with the hand type and the object
		/// <example>
		/// <code>
		///	PlayerAPI.HeldObjects[PlayerAPI.HandType...]...
		///	</code>
		/// </example>
		/// </summary>
		public Dictionary<HandType, FVRPhysicalObject> HeldObjects
		{
			get
			{
				var objs = new Dictionary<HandType, FVRPhysicalObject>();

				foreach (var hand in PlayerHands)
				{
					if (hand.Value.CurrentInteractable is FVRPhysicalObject obj)
					{
						objs.Add(hand.Key, obj);
					}
				}

				return objs;
			}
		}

		/// <summary>
		/// Position of the player
		/// </summary>
		public Transform PlayerPositon
		{
			get => GM.CurrentPlayerRoot;

			set
			{
				GM.CurrentPlayerRoot.position = value.position;
				GM.CurrentPlayerRoot.rotation = value.rotation;
				PlayerBody.UpdatePlayerBodyPositions();
			}
		}

		/// <summary>
		/// Speed of the player
		/// </summary>
		public float PlayerSpeed => PlayerBody.GetBodyMovementSpeed();

		/// <summary>
		/// IFF (AI interaction layer) of the player
		/// </summary>
		public int PlayerIFF
		{
			get => PlayerBody.GetPlayerIFF();
			set => PlayerBody.SetPlayerIFF(value);
		}

		public SosigEnemyTemplate PlayerOutfit
		{
			//get =>; //TODO: Implement getting the current player outfit
			set
			{
				PlayerBody.SetOutfit(value);
				PlayerBody.UpdateSosigPlayerBodyState();
			}
		}
		#endregion

		/// <summary>
		///	Kills player
		/// </summary>
		public void DamagePlayer() => PlayerBody.KillPlayer(false);

		/// <summary>
		/// Heals player
		/// </summary>
		public void HealPlayer() => PlayerBody.HealPercent(100);

		/// <summary>
		/// Damages the player
		/// </summary>
		/// <param name="amount">Amount to damage</param>
		public void DamagePlayer(float amount) => PlayerBody.Health -= amount;

		/// <summary>
		/// Heals player
		/// </summary>
		/// <param name="amount">Amount to heal</param>
		public void HealPlayer(float amount) => PlayerBody.Health += amount;

		/// <summary>
		/// Teleports player
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="additive">If this is true, the "position" parameter will be added onto the player position, rather than setting it</param>
		public void TeleportPlayer(Vector3 position, bool additive = true)
		{
			if (additive)
				PlayerPositon.position += position;
			else
				PlayerPositon.position = position;
			PlayerBody.UpdatePlayerBodyPositions();
		}



		internal PlayerAPI() {}

	}
}
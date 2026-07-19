using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Data;
using LabFusion.Extensions;
using LabFusion.Marrow;
using LabFusion.Marrow.Integration;
using LabFusion.Marrow.Pool;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.RPC;
using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Triggers;
using LabFusion.Senders;
using LabFusion.UI.Popups;
using LabFusion.Utilities;
using LabFusion.Voice;
using MelonLoader;
using UnityEngine;

namespace TroubleInFordTown;

/// <summary>
/// Manages loot spawn positions, physics-grab pickup detection,
/// respawn timers, and interacts with the gamemode on collection.
/// </summary>
public class LootManager
{
	private const float GrabRadius = 1.2f;

	private const float RespawnDelay = 30f;

	// Barcode for the loot orb base item.
	// Must be a physics-grabbable item in the BONELAB asset warehouse.
	// Replace with a dedicated loot orb crate once one is available.
	private const string LootBarcode = "c1534c5a-8eb7-4150-9fef-fc4e65737562";

	// Default hardcoded loot spawn positions (relative to map origin).
	// These should be tuned per-map or replaced with GamemodeMarker positions.
	private static readonly Vector3[] DefaultPositions = new Vector3[]
	{
		new Vector3(5f, 1f, 5f),
		new Vector3(-5f, 1f, 5f),
		new Vector3(5f, 1f, -5f),
		new Vector3(-5f, 1f, -5f),
		new Vector3(0f, 1f, 8f),
		new Vector3(0f, 1f, -8f),
		new Vector3(8f, 1f, 0f),
		new Vector3(-8f, 1f, 0f),
	};

	private readonly List<ActiveLoot> _activeLoot = new List<ActiveLoot>();

	private TroubleInFordTownGamemode _gamemode;

	private bool _initialized;

	private AudioClip _pickupSound;

	private bool _soundLoaded;

	/// <summary>Fired when any loot is collected by the local player.</summary>
	public event Action OnLootCollected;

	/// <summary>
	/// Fired when a Bystander's loot count reaches or exceeds the threshold.
	/// The gamemode should spawn a revolver in response.
	/// </summary>
	public event Action OnLootThresholdReached;

	/// <summary>
	/// Initialize the manager with a reference to the gamemode.
	/// Call once during OnGamemodeStarted.
	/// </summary>
	public void Initialize(TroubleInFordTownGamemode gamemode)
	{
		_gamemode = gamemode;
		_initialized = true;
		TryLoadPickupSound();
	}

	private void TryLoadPickupSound()
	{
		if (_soundLoaded) return;
		_soundLoaded = true;
		try
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			System.IO.Stream stream = assembly.GetManifestResourceStream("windchime2.wav");
			if (stream != null)
			{
				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				stream.Dispose();
				_pickupSound = WavLoader.Load(data, "LootPickup");
			}
		}
		catch
		{
		}
	}

	/// <summary>Spawn all loot items at the defined positions.</summary>
	public void OnRoundStart()
	{
		if (!_initialized) return;

		OnRoundEnd();

		if (NetworkInfo.IsHost)
		{
			foreach (Vector3 pos in DefaultPositions)
			{
				SpawnLootItem(pos);
			}
		}
	}

	/// <summary>Despawn all loot items and reset tracking state.</summary>
	public void OnRoundEnd()
	{
		foreach (ActiveLoot loot in _activeLoot)
		{
			if (loot.EntityId != 0)
			{
				try
				{
					NetworkAssetSpawner.Despawn(new NetworkAssetSpawner.DespawnRequestInfo
					{
						EntityID = loot.EntityId,
						DespawnEffect = false
					});
				}
				catch
				{
				}
			}
		}
		_activeLoot.Clear();
	}

	/// <summary>
	/// Per-frame update: proximity + grip detection and respawn timers.
	/// Call from the gamemode's OnUpdate.
	/// </summary>
	public void Update()
	{
		if (!_initialized || !RigData.HasPlayer)
		{
			return;
		}

		// Cache hand positions
		Hand leftHand = RigData.Refs.LeftHand;
		Transform leftHandTransform = (leftHand != null) ? ((Component)leftHand).transform : null;
		Hand rightHand = RigData.Refs.RightHand;
		Transform rightHandTransform = (rightHand != null) ? ((Component)rightHand).transform : null;
		Vector3 leftHandPos = (leftHandTransform != null) ? leftHandTransform.position : Vector3.zero;
		Vector3 rightHandPos = (rightHandTransform != null) ? rightHandTransform.position : Vector3.zero;

		float dt = Time.deltaTime;

		for (int i = _activeLoot.Count - 1; i >= 0; i--)
		{
			ActiveLoot loot = _activeLoot[i];

			// Collected items — advance respawn timer
			if (loot.Collected)
			{
				loot.RespawnTimeRemaining -= dt;
				if (loot.RespawnTimeRemaining <= 0f && NetworkInfo.IsHost)
				{
					loot.Collected = false;
					loot.RespawnTimeRemaining = 0f;
					SpawnLootItem(loot.SpawnPos, i);
				}
				_activeLoot[i] = loot;
				continue;
			}

			// Null-check the spawned GameObject
			if (loot.Go == null || loot.Go.Equals(null))
			{
				_activeLoot.RemoveAt(i);
				continue;
			}

			// Proximity-to-hands check (reuses CorpseManager's pattern)
			Vector3 lootPos = loot.Go.transform.position;
			float dist = Mathf.Min(
				Vector3.Distance(leftHandPos, lootPos),
				Vector3.Distance(rightHandPos, lootPos));

			bool isHeld = IsHeldByLocal(loot.Go);

			// Grip-event detection: hand is close AND item just became held
			if (dist < GrabRadius && isHeld && !loot.WasHeldLastFrame)
			{
				OnPlayerPickupLoot(i);
			}

			loot.WasHeldLastFrame = isHeld;
			_activeLoot[i] = loot;
		}
	}

	private void OnPlayerPickupLoot(int lootIndex)
	{
		ActiveLoot loot = _activeLoot[lootIndex];

		// Play the pickup sound at the loot's position
		PlayPickupSound(loot.Go.transform.position);

		// Increment the gamemode's loot counter
		if (_gamemode != null)
		{
			_gamemode.IncrementLootCount();
		}

		// Notify the gamemode
		OnLootCollected?.Invoke();

		// Despawn this loot item (host only)
		if (NetworkInfo.IsHost && loot.EntityId != 0)
		{
			try
			{
				NetworkAssetSpawner.Despawn(new NetworkAssetSpawner.DespawnRequestInfo
				{
					EntityID = loot.EntityId,
					DespawnEffect = false
				});
			}
			catch
			{
			}
		}

		// Start respawn timer
		loot.Collected = true;
		loot.RespawnTimeRemaining = RespawnDelay;
		loot.WasHeldLastFrame = false;
		loot.Go = null;
		_activeLoot[lootIndex] = loot;

		// Check threshold for Bystanders
		if (_gamemode != null)
		{
			Team localTeam = _gamemode.TeamManager.GetLocalTeam();
			if (localTeam != null && localTeam == _gamemode.BystanderTeam &&
			    _gamemode.CurrentLootCount >= _gamemode.LootThreshold)
			{
				OnLootThresholdReached?.Invoke();
			}
		}
	}

	private void SpawnLootItem(Vector3 position, int replaceIndex = -1)
	{
		if (!RigData.HasPlayer || !NetworkInfo.IsHost)
		{
			return;
		}

		Spawnable spawnable = LocalAssetSpawner.CreateSpawnable(LootBarcode);
		NetworkAssetSpawner.Spawn(new NetworkAssetSpawner.SpawnRequestInfo
		{
			Spawnable = spawnable,
			Position = position,
			Rotation = Quaternion.identity,
			SpawnEffect = false,
			SpawnCallback = delegate(NetworkAssetSpawner.SpawnCallbackInfo info)
			{
				GameObject go = info.Spawned;
				if (go == null || go.Equals(null))
				{
					return;
				}

				// Attach the visual/glow component
				LootPickup pickup = go.AddComponent<LootPickup>();
				pickup.SpawnIndex = (replaceIndex >= 0) ? replaceIndex : _activeLoot.Count;

				ActiveLoot entry = new ActiveLoot
				{
					SpawnPos = position,
					Go = go,
					EntityId = info.Entity.ID,
					Collected = false,
					RespawnTimeRemaining = 0f,
					WasHeldLastFrame = false
				};

				if (replaceIndex >= 0 && replaceIndex < _activeLoot.Count)
				{
					_activeLoot[replaceIndex] = entry;
				}
				else
				{
					_activeLoot.Add(entry);
				}
			}
		});
	}

	private void PlayPickupSound(Vector3 position)
	{
		if (_pickupSound == null || _pickupSound.Equals(null))
		{
			return;
		}
		try
		{
			GameObject temp = new GameObject("LootPickupAudio");
			temp.transform.position = position;
			AudioSource source = temp.AddComponent<AudioSource>();
			source.clip = _pickupSound;
			source.pitch = UnityEngine.Random.Range(0.4f, 1.6f);
			source.volume = 1f;
			source.spatialBlend = 1f;
			source.Play();
			UnityEngine.Object.Destroy(temp, _pickupSound.length + 0.5f);
		}
		catch
		{
		}
	}

	private static bool IsHeldByLocal(GameObject go)
	{
		if (go == null || go.Equals(null) || !RigData.HasPlayer)
		{
			return false;
		}
		try
		{
			PhysicsRig rig = RigData.Refs.RigManager.physicsRig;
			Hand[] hands = new Hand[2] { rig.leftHand, rig.rightHand };
			foreach (Hand hand in hands)
			{
				if (hand == null)
				{
					continue;
				}
				GameObject attached = hand.m_CurrentAttachedGO;
				if (attached == null || attached.Equals(null))
				{
					continue;
				}
				if (attached == go ||
				    attached.transform.IsChildOf(go.transform) ||
				    go.transform.IsChildOf(attached.transform))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private struct ActiveLoot
	{
		public Vector3 SpawnPos;
		public GameObject Go;
		public ushort EntityId;
		public bool Collected;
		public float RespawnTimeRemaining;
		public bool WasHeldLastFrame;
	}
}

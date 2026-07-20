using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Data;
using LabFusion.Entities;
using LabFusion.Extensions;
using LabFusion.Marrow;
using LabFusion.Marrow.Integration;
using LabFusion.Marrow.Pool;
using LabFusion.Menu.Data;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.RPC;
using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Points;
using LabFusion.SDK.Triggers;
using LabFusion.Senders;
using LabFusion.UI.Popups;
using LabFusion.Utilities;
using LabFusion.Voice;
using MelonLoader;
using UnityEngine;

namespace TroubleInFordTown;

public class TroubleInFordTownGamemode : Gamemode
{
	public static class Defaults
	{
		public const int TraitorCount = 1;

		public const int PrepSeconds = 30;

		public const int RoundMinutes = 5;

		public const int WinnerBitReward = 100;

		public const int LoserBitReward = 25;

		public const int CorrectKillBitReward = 50;

		public static readonly MonoDiscReference[] Tracks = FusionMonoDiscPlaylists.AmbientPlaylist;

		public static readonly string[] Sidearms = new string[3] { "c1534c5a-fcfc-4f43-8fb0-d29531393131", "c1534c5a-2a4f-481f-8542-cc9545646572", "c1534c5a-bcb7-4f02-a4f5-da9550333530" };

		public static readonly string[] Primaries = new string[9] { "c1534c5a-a6b5-4177-beb8-04d947756e41", "c1534c5a-9112-49e5-b022-9c955269666c", "c1534c5a-c061-4c5c-a5e2-3d955269666c", "c1534c5a-d00c-4aa8-adfd-3495534d474d", "c1534c5a-8d03-42de-93c7-f595534d4755", "c1534c5a-4c47-428d-b5a5-b05747756e56", "c1534c5a-7f05-402f-9320-609647756e35", "c1534c5a-2774-48db-84fd-778447756e46", "c1534c5a-04d7-41a0-b7b8-5a95534d4750" };

		public const string TraitorKnife = "c1534c5a-1fb8-477c-afbe-2a95436f6d62";
	}

	private const string MusicMetaKey = "tift_music";

	private readonly HashSet<ushort> _deadPlayers = new HashSet<ushort>();

	private const string MurdererColor = "#b40d19";

	// Barcode for the revolver spawned when a Bystander reaches LootThreshold.
	// Uses the first TiFT sidearm barcode as placeholder — replace with actual revolver crate barcode.
	private const string RevolverBarcode = "c1534c5a-fcfc-4f43-8fb0-d29531393131";

	private const string BystanderColor = "#0d76ea";

	private const string SpectatorColor = "#aaaaaa";

	private readonly MusicPlaylist _playlist = new MusicPlaylist();

	private readonly TeamManager _teamManager = new TeamManager();

	private readonly Team _murdererTeam = new Team("Murderers");

	private readonly Team _bystanderTeam = new Team("Bystanders");

	private readonly Team _spectatorTeam = new Team("Spectators");

	private ushort _gunHolderSmallID;

	private int _currentLootCount;

	public int CurrentLootCount => _currentLootCount;

	public void IncrementLootCount()
	{
		_currentLootCount++;
	}

	private readonly LootManager _lootManager = new LootManager();

	private bool _roundStarted;

	private float _elapsedTime;

	private bool _oneMinuteLeft;

	private bool _gameEnded;

	private bool _localDied;

	private Vector3 _localDeathPos = Vector3.zero;

	private bool _hasLocalDeathPos;

	private Vector3 _localLastAlivePos = Vector3.zero;

	private bool _hasLastAlivePos;

	private Vector3 _prevHeadPos = Vector3.zero;

	private bool _hasPrevHeadPos;

	private int _teleportCooldown;

	private float _roundLengthSeconds;

	private int _lastWristSecond = int.MinValue;

	private object _lastWristTeam;

	private bool _wristTextInit;

	private float _spectatorTimer;

	private bool _stopScheduled;

	private float _stopDelayRemaining;

	private readonly WristIndicator _wristIndicator = new WristIndicator();

	private readonly StatsPage _statsPage = new StatsPage();

	private readonly RolesInfoMenu _rolesInfoMenu = new RolesInfoMenu();

	private readonly RoleLabels _roleLabels = new RoleLabels();

	private readonly HolsterHider _holsterHider = new HolsterHider();

	private readonly NametagSystem _nametagSystem = new NametagSystem();

	private readonly BlackScreenController _blackScreen = new BlackScreenController();

	private bool _prepRolesRevealed;

	private float _prepRevealTimer;

	private readonly List<ushort> _spawnedLoadoutItems = new List<ushort>();

	private readonly Dictionary<ushort, Vector3> _lastKnownPositions = new Dictionary<ushort, Vector3>();

	private readonly Dictionary<ushort, ushort> _recentKillers = new Dictionary<ushort, ushort>();

	private readonly Dictionary<ushort, float> _recentKillTime = new Dictionary<ushort, float>();

	private const float KillFreshnessSeconds = 4f;

	private readonly Dictionary<ushort, string> _recentWeapons = new Dictionary<ushort, string>();

	public override string Title => "Trouble In FordTown";

	public override string Author => "JonLandonMods";

	public override string Barcode => "JonLandonMods.TroubleInFordTown";

	public override string Description => "MurderLab - A social deduction gamemode for BONELAB. One Murderer, the rest are Bystanders. Can you survive?";

	public int PrepSeconds { get; set; } = 5;

	public int RoundMinutes { get; set; } = 5;

	public bool WinMusicEnabled { get; set; } = true;

	public int LootThreshold { get; set; } = 5;

	public int GameVariant { get; set; } = 0;

	public bool SprintEnabled { get; set; } = true;

	public bool FootprintsEnabled { get; set; } = true;

	public bool BlackFogEnabled { get; set; } = true;

	public bool DisguiseEnabled { get; set; } = true;

	public int BlackFogTimer { get; set; } = 120;

	public bool RemoveDisguiseOnKill { get; set; } = true;

	public int MinPlayers { get; set; } = 3;

	public override bool DisableDevTools => true;

	public override bool DisableSpawnGun => true;

	public override bool DisableManualUnragdoll => true;

	public MusicPlaylist Playlist => _playlist;

	public TeamManager TeamManager => _teamManager;

	public Team MurdererTeam => _murdererTeam;

	public Team BystanderTeam => _bystanderTeam;

	public Team SpectatorTeam => _spectatorTeam;

	public TriggerEvent RoundStartedEvent { get; set; }

	public TriggerEvent OneMinuteLeftEvent { get; set; }

	public TriggerEvent BystanderVictoryEvent { get; set; }

	public TriggerEvent MurdererVictoryEvent { get; set; }

	public TriggerEvent TimeUpEvent { get; set; }

	public TriggerEvent CorpseSpawnEvent { get; set; }

	public override GroupElementData CreateSettingsGroup()
	{
		GroupElementData groupElementData = base.CreateSettingsGroup();
		GroupElementData generalGroup = new GroupElementData("General");
		groupElementData.AddElement(generalGroup);
		generalGroup.AddElement(new IntElementData
		{
			Title = "Round Timer (0 = Unlimited)",
			Value = RoundMinutes,
			Increment = 1,
			MinValue = 0,
			MaxValue = 15,
			OnValueChanged = delegate(int v)
			{
				RoundMinutes = v;
			}
		});
		generalGroup.AddElement(new IntElementData
		{
			Title = "Prep Time (Seconds)",
			Value = PrepSeconds,
			Increment = 1,
			MinValue = 3,
			MaxValue = 30,
			OnValueChanged = delegate(int v)
			{
				PrepSeconds = v;
			}
		});
		generalGroup.AddElement(new IntElementData
		{
			Title = "Min Players",
			Value = MinPlayers,
			Increment = 1,
			MinValue = 2,
			MaxValue = 12,
			OnValueChanged = delegate(int v)
			{
				MinPlayers = v;
			}
		});
		GroupElementData modeGroup = new GroupElementData("Mode");
		groupElementData.AddElement(modeGroup);
		modeGroup.AddElement(new IntElementData
		{
			Title = "Game Variant",
			Value = GameVariant,
			Increment = 1,
			MinValue = 0,
			MaxValue = 1,
			OnValueChanged = delegate(int v)
			{
				GameVariant = v;
			}
		});
		modeGroup.AddElement(new IntElementData
		{
			Title = "Gold Threshold",
			Value = LootThreshold,
			Increment = 1,
			MinValue = 3,
			MaxValue = 10,
			OnValueChanged = delegate(int v)
			{
				LootThreshold = v;
			}
		});
		GroupElementData abilitiesGroup = new GroupElementData("Murderer Abilities");
		groupElementData.AddElement(abilitiesGroup);
		abilitiesGroup.AddElement(new BoolElementData
		{
			Title = "Enable Sprint",
			Value = SprintEnabled,
			OnValueChanged = delegate(bool v)
			{
				SprintEnabled = v;
			}
		});
		abilitiesGroup.AddElement(new BoolElementData
		{
			Title = "Enable Footprints",
			Value = FootprintsEnabled,
			OnValueChanged = delegate(bool v)
			{
				FootprintsEnabled = v;
			}
		});
		abilitiesGroup.AddElement(new BoolElementData
		{
			Title = "Enable Black Fog",
			Value = BlackFogEnabled,
			OnValueChanged = delegate(bool v)
			{
				BlackFogEnabled = v;
			}
		});
		abilitiesGroup.AddElement(new BoolElementData
		{
			Title = "Enable Corpse Disguise",
			Value = DisguiseEnabled,
			OnValueChanged = delegate(bool v)
			{
				DisguiseEnabled = v;
			}
		});
		abilitiesGroup.AddElement(new IntElementData
		{
			Title = "Black Fog Timer (Seconds)",
			Value = BlackFogTimer,
			Increment = 10,
			MinValue = 30,
			MaxValue = 300,
			OnValueChanged = delegate(int v)
			{
				BlackFogTimer = v;
			}
		});
		abilitiesGroup.AddElement(new BoolElementData
		{
			Title = "Remove Disguise on Kill",
			Value = RemoveDisguiseOnKill,
			OnValueChanged = delegate(bool v)
			{
				RemoveDisguiseOnKill = v;
			}
		});
		GroupElementData audioGroup = new GroupElementData("Audio");
		groupElementData.AddElement(audioGroup);
		audioGroup.AddElement(new BoolElementData
		{
			Title = "Play Win Song",
			Value = WinMusicEnabled,
			OnValueChanged = delegate(bool v)
			{
				WinMusicEnabled = v;
			}
		});
		return groupElementData;
	}

	public override bool CheckReadyConditions()
	{
		return PlayerIDManager.PlayerIDs.Count >= 2;
	}

	public override void OnGamemodeRegistered()
	{
		MultiplayerHooking.OnPlayerAction += OnPlayerAction;
		FusionOverrides.OnValidateNametag += OnValidateNametag;
		TeamManager.Register(this);
		TeamManager.AddTeam(MurdererTeam);
		TeamManager.AddTeam(BystanderTeam);
		TeamManager.AddTeam(SpectatorTeam);
		TeamManager.OnAssignedToTeam += OnAssignedToTeam;
		RoundStartedEvent = new TriggerEvent("RoundStarted", base.Relay, serverOnly: true);
		RoundStartedEvent.OnTriggeredWithValue += OnRoundStarted;
		OneMinuteLeftEvent = new TriggerEvent("OneMinuteLeft", base.Relay, serverOnly: true);
		OneMinuteLeftEvent.OnTriggered += OnOneMinuteLeft;
		BystanderVictoryEvent = new TriggerEvent("BystanderVictory", base.Relay, serverOnly: true);
		BystanderVictoryEvent.OnTriggered += OnBystanderVictory;
		MurdererVictoryEvent = new TriggerEvent("MurdererVictory", base.Relay, serverOnly: true);
		MurdererVictoryEvent.OnTriggered += OnMurdererVictory;
		TimeUpEvent = new TriggerEvent("TimeUp", base.Relay, serverOnly: true);
		TimeUpEvent.OnTriggered += OnTimeUp;
		CorpseSpawnEvent = new TriggerEvent("CorpseSpawn", base.Relay);
		CorpseSpawnEvent.OnTriggeredWithValue += OnCorpseSpawn;
		try
		{
			_statsPage.Register();
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("[MurderLab] Stats page unavailable: " + ex.Message);
		}
		try
		{
			_rolesInfoMenu.Register();
		}
		catch (Exception ex2)
		{
			MelonLogger.Warning("[MurderLab] Roles Info menu unavailable: " + ex2.Message);
		}
		try
		{
			WinMusic.Initialize();
		}
		catch (Exception ex3)
		{
			MelonLogger.Warning("[MurderLab] Win music unavailable: " + ex3.Message);
		}
		LocalHealth.OnRespawn += OnLocalRespawn;
	}

	private void OnLocalRespawn()
	{
		if (!base.IsStarted)
		{
			return;
		}
		try
		{
			LocalPlayer.ReleaseGrips();
		}
		catch
		{
		}
	}

	public override void OnGamemodeUnregistered()
	{
		MultiplayerHooking.OnPlayerAction -= OnPlayerAction;
		FusionOverrides.OnValidateNametag -= OnValidateNametag;
		LocalHealth.OnRespawn -= OnLocalRespawn;
		TeamManager.Unregister();
		TeamManager.OnAssignedToTeam -= OnAssignedToTeam;
		RoundStartedEvent.UnregisterEvent();
		RoundStartedEvent = null;
		OneMinuteLeftEvent.UnregisterEvent();
		OneMinuteLeftEvent = null;
		BystanderVictoryEvent.UnregisterEvent();
		BystanderVictoryEvent = null;
		MurdererVictoryEvent.UnregisterEvent();
		MurdererVictoryEvent = null;
		TimeUpEvent.UnregisterEvent();
		TimeUpEvent = null;
		CorpseSpawnEvent.UnregisterEvent();
		CorpseSpawnEvent = null;
		try
		{
			_statsPage.Unregister();
		}
		catch
		{
		}
		try
		{
			_rolesInfoMenu.Unregister();
		}
		catch
		{
		}
	}

	public override void OnGamemodeStarted()
	{
		base.OnGamemodeStarted();
		_roundStarted = false;
		_elapsedTime = 0f;
		_oneMinuteLeft = false;
		_gameEnded = false;
		_localDied = false;
		_hasLocalDeathPos = false;
		_hasLastAlivePos = false;
		_hasPrevHeadPos = false;
		_teleportCooldown = 0;
		_wristTextInit = false;
		_spectatorTimer = 0f;
		_roundLengthSeconds = 0f;
		_gunHolderSmallID = 0;
		_currentLootCount = 0;
		_spawnedLoadoutItems.Clear();
		_lastKnownPositions.Clear();
		_recentKillers.Clear();
		_recentKillTime.Clear();
		_recentWeapons.Clear();
		_deadPlayers.Clear();
		if (KarmaManager.Enabled)
		{
			foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
			{
				KarmaManager.EnsurePlayer(playerID);
			}
		}
		if (NetworkInfo.IsHost)
		{
			try
			{
				base.Metadata.TrySetMetadata("tift_music", WinMusicEnabled ? "1" : "0");
			}
			catch
			{
			}
		}
		LocalHealth.MortalityOverride = true;
		LocalControls.DisableSlowMo = true;
		GamemodeHelper.SetSpawnPoints(GamemodeMarker.FilterMarkers());
		GamemodeHelper.TeleportToSpawnPoint();
		FusionOverrides.ForceUpdateOverrides();
		_lootManager.Initialize(this);
		// MurderLab: Show black screen immediately instead of free-roam notification.
		// Roles are revealed after the prep timer expires.
		_blackScreen.Create();
		_blackScreen.Show();
		_prepRolesRevealed = false;
	}

	public override void OnGamemodeStopped()
	{
		base.OnGamemodeStopped();
		_roundStarted = false;
		_elapsedTime = 0f;
		_oneMinuteLeft = false;
		_localDied = false;
		_deadPlayers.Clear();
		_currentLootCount = 0;
		_lootManager.OnLootThresholdReached -= OnLootThresholdReached;
		ClearSpectatorEffects();
		_wristIndicator.Destroy();
		_roleLabels.ClearAll();
		_holsterHider.ClearAll();
		_lootManager.OnRoundEnd();
		_nametagSystem.OnRoundEnd();
		CorpseManager.ClearAll();
		CorpseManager.DestroyUI();
		DespawnLoadoutItems();
		Playlist.StopPlaylist();
		LocalHealth.MortalityOverride = null;
		LocalHealth.VitalityOverride = null;
		LocalControls.DisableSlowMo = false;
		GamemodeHelper.ResetSpawnPoints();
		if (NetworkInfo.IsHost)
		{
			TeamManager.UnassignAllPlayers();
		}
		FusionOverrides.ForceUpdateOverrides();
		_statsPage.Refresh();
	}

	protected override void OnUpdate()
	{
		if (!base.IsStarted)
		{
			return;
		}
		_elapsedTime += TimeReferences.DeltaTime;
		if (_roundStarted && !_localDied && RigData.HasPlayer)
		{
			try
			{
				Transform headset = RigData.Refs.Headset;
				if ((Object)(object)headset != (Object)null)
				{
					Vector3 val = headset.position - Vector3.up * 0.6f;
					if (_hasPrevHeadPos)
					{
						Vector3 val2 = val - _prevHeadPos;
						if (val2.sqrMagnitude > 6.25f)
						{
							_teleportCooldown = 8;
						}
					}
					if (_teleportCooldown > 0)
					{
						_teleportCooldown--;
					}
					else
					{
						_localLastAlivePos = val;
						_hasLastAlivePos = true;
					}
					_prevHeadPos = val;
					_hasPrevHeadPos = true;
				}
			}
			catch
			{
			}
		}
		UpdateSpectators();
		UpdateWristIndicator();
		CorpseManager.Update();
		_lootManager.Update();
		_roleLabels.Update(this);
		_nametagSystem.Update(this);
		if (_roundStarted)
		{
			_holsterHider.Update();
		}
		if (NetworkInfo.IsHost && _stopScheduled)
		{
			_stopDelayRemaining -= TimeReferences.DeltaTime;
			if (_stopDelayRemaining <= 0f)
			{
				_stopScheduled = false;
				GamemodeManager.StopGamemode();
			}
		}
		if (!NetworkInfo.IsHost || _gameEnded)
		{
			return;
		}
		// MurderLab prep phase:
		// 1. Black screen shows immediately (from OnGamemodeStarted)
		// 2. At PrepSeconds elapsed: assign roles, reveal role text
		// 3. After 3s display + 1s fade: start the round
		if (!_roundStarted && !_prepRolesRevealed && _elapsedTime >= (float)PrepSeconds)
		{
			AssignRoles();
			_prepRolesRevealed = true;
			_prepRevealTimer = 4f;
			ShowPrepRoleText();
		}
		if (_prepRolesRevealed && !_roundStarted)
		{
			_prepRevealTimer -= Time.deltaTime;
			if (_prepRevealTimer <= 1f && !_blackScreen.IsFading)
			{
				_blackScreen.StartFadeOut(1f);
			}
			if (_prepRevealTimer <= 0f)
			{
				_roundStarted = true;
				_elapsedTime = 0f;
				_prepRolesRevealed = false;
				RoundStartedEvent.TryInvoke(RoundMinutes.ToString());
			}
		}
		if (_roundStarted)
		{
			float num = (float)RoundMinutes * 60f;
			if (!_oneMinuteLeft && num - _elapsedTime <= 60f)
			{
				_oneMinuteLeft = true;
				OneMinuteLeftEvent.TryInvoke();
			}
			if (_elapsedTime >= num)
			{
				_gameEnded = true;
				TimeUpEvent.TryInvoke();
				ScheduleStop(WinMusic.GetMaxDuration(WinSide.Innocents, WinMusicEnabled));
			}
		}
	}

	private void AssignRoles()
	{
		List<PlayerID> players = new List<PlayerID>(PlayerIDManager.PlayerIDs);
		players.Shuffle();

		if (players.Count < 3) return;

		TeamManager.TryAssignTeam(players[0], MurdererTeam);
		players.RemoveAt(0);

		TeamManager.TryAssignTeam(players[0], BystanderTeam);
		_gunHolderSmallID = players[0].SmallID;
		players.RemoveAt(0);

		foreach (var p in players)
			TeamManager.TryAssignTeam(p, BystanderTeam);
	}

	private void ShowPrepRoleText()
	{
		Team localTeam = TeamManager.GetLocalTeam();
		if (localTeam == null)
		{
			return;
		}
		string title;
		string subtitle;
		string footer1;
		string footer2;
		Color titleColor;
		Color subtitleColor;
		if (localTeam == MurdererTeam)
		{
			title = "You are the murderer";
			subtitle = "Kill everyone";
			footer1 = "Don't get caught";
			footer2 = "";
			titleColor = Color.red;
			subtitleColor = Color.red;
		}
		else if (TeamManager.GetLocalTeam() == BystanderTeam && PlayerIDManager.LocalID != null && PlayerIDManager.LocalID.SmallID == _gunHolderSmallID)
		{
			title = "You are a bystander";
			subtitle = "with a secret weapon";
			footer1 = "There is a murderer on the loose";
			footer2 = "Find and kill him";
			titleColor = Color.blue;
			subtitleColor = new Color(0.5f, 0f, 0.5f);
		}
		else
		{
			title = "You are a bystander";
			subtitle = "";
			footer1 = "There is a murderer on the loose";
			footer2 = "Don't get killed";
			titleColor = Color.blue;
			subtitleColor = Color.blue;
		}
		_blackScreen.DisplayRoleText(title, subtitle, footer1, footer2, titleColor, subtitleColor);
	}

	private void UpdateSpectators()
	{
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		bool flag = _roundStarted && SpectatorTeam.HasPlayer(PlayerIDManager.LocalID);
		if (flag && RigData.HasPlayer)
		{
			try
			{
				PhysicsRig physicsRig = RigData.Refs.RigManager.physicsRig;
				Hand leftHand = physicsRig.leftHand;
				if ((Object)(object)((leftHand != null) ? leftHand.m_CurrentAttachedGO : null) != (Object)null)
				{
					physicsRig.leftHand.TryDetach();
				}
				Hand rightHand = physicsRig.rightHand;
				if ((Object)(object)((rightHand != null) ? rightHand.m_CurrentAttachedGO : null) != (Object)null)
				{
					physicsRig.rightHand.TryDetach();
				}
			}
			catch
			{
			}
		}
		_spectatorTimer -= TimeReferences.DeltaTime;
		if (_spectatorTimer > 0f)
		{
			return;
		}
		_spectatorTimer = 0.1f;
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (playerID.IsMe || !NetworkPlayerManager.TryGetPlayer(playerID.SmallID, out var player))
			{
				continue;
			}
			bool flag2 = _roundStarted && SpectatorTeam.HasPlayer(playerID);
			bool num = flag2 || _deadPlayers.Contains(playerID.SmallID);
			if (player.ForceHide != flag2)
			{
				player.ForceHide = flag2;
			}
			bool flag3 = _localDied || flag;
			bool flag4 = num && !flag3;
			VoiceSource voiceSource = player.VoiceSource?.VoiceSource;
			if ((Object)(object)voiceSource != (Object)null && voiceSource.Muted != flag4)
			{
				voiceSource.Muted = flag4;
			}
			try
			{
				Transform val = player.RigRefs?.Headset;
				if ((Object)(object)val != (Object)null)
				{
					_lastKnownPositions[playerID.SmallID] = val.position - Vector3.up * 0.6f;
				}
			}
			catch
			{
			}
		}
	}

	private void ClearSpectatorEffects()
	{
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (!playerID.IsMe && NetworkPlayerManager.TryGetPlayer(playerID.SmallID, out var player))
			{
				player.ForceHide = false;
				VoiceSource voiceSource = player.VoiceSource?.VoiceSource;
				if ((Object)(object)voiceSource != (Object)null)
				{
					voiceSource.Muted = false;
				}
			}
		}
	}

	private void UpdateWristIndicator()
	{
		if (_wristIndicator.TryCreate())
		{
			int num = (_roundStarted ? Mathf.CeilToInt(Mathf.Max(_roundLengthSeconds - _elapsedTime, 0f)) : (-1));
			object obj = (_roundStarted ? TeamManager.GetLocalTeam() : null);
			if (!_wristTextInit || num != _lastWristSecond || obj != _lastWristTeam)
			{
				_wristTextInit = true;
				_lastWristSecond = num;
				_lastWristTeam = obj;
				_wristIndicator.SetText(GetWristText());
			}
			_wristIndicator.Update();
		}
	}

	private string GetWristText()
	{
		if (!_roundStarted)
			return "GET READY\nRoles incoming...";

		string timer = TimeSpan.FromSeconds(Mathf.Max(_roundLengthSeconds - _elapsedTime, 0f)).ToString("mm\\:ss");
		Team localTeam = TeamManager.GetLocalTeam();

		if (localTeam == MurdererTeam)
			return $"<color=#b40d19>MURDERER</color>\n{GetLocalRPName()} | Loot: {_currentLootCount}\n{timer}";
		else if (localTeam == BystanderTeam)
			return $"<color=#0d76ea>BYSTANDER</color>\n{GetLocalRPName()} | Loot: {_currentLootCount}\n{timer}";
		else if (localTeam == SpectatorTeam)
			return "<color=#aaaaaa>SPECTATOR</color>";
		else
			return "NO ROLE";
	}

	private string GetLocalRPName() => _nametagSystem.GetLocalRPName();

	private void SpawnLoadout()
	{
		Team localTeam = TeamManager.GetLocalTeam();
		if (localTeam == SpectatorTeam) return;

		if (localTeam == MurdererTeam)
		{
			GiveMurdererKnife();
		}
		else if (localTeam == BystanderTeam && GetLocalSmallID() == _gunHolderSmallID)
		{
			SpawnLoadoutItem(Defaults.Sidearms[0], false, null);
		}
	}

	private static ushort GetLocalSmallID()
	{
		PlayerID local = PlayerIDManager.LocalID;
		return local != null ? local.SmallID : (ushort)0;
	}

	public void GiveMurdererKnife()
	{
		string barcode = "c1534c5a-1fb8-477c-afbe-2a95436f6d62";
		SpawnLoadoutItem(barcode, preferLowSlot: true, null, delegate(GameObject go)
		{
			HolsterHider.RegisterLocalKnife(go);
		});
	}

	private static bool SpawnableExists(string barcode)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		if (string.IsNullOrEmpty(barcode))
		{
			return false;
		}
		try
		{
			AssetWarehouse instance = AssetWarehouse.Instance;
			return instance != null && instance.HasCrate(new Barcode(barcode));
		}
		catch
		{
			return false;
		}
	}

	private void SpawnLoadoutItem(string barcode, bool preferLowSlot = false, Action onDone = null, Action<GameObject> onSpawned = null)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		if (!RigData.HasPlayer)
		{
			onDone?.Invoke();
			return;
		}
		Transform headset = RigData.Refs.Headset;
		Vector3 val = headset.forward;
		val.y = 0f;
		val = ((val.sqrMagnitude > 0.0001f) ? val.normalized : Vector3.forward);
		Vector3 position = headset.position + val * 0.5f - Vector3.up * 0.55f;
		Spawnable spawnable = LocalAssetSpawner.CreateSpawnable(barcode);
		NetworkAssetSpawner.Spawn(new NetworkAssetSpawner.SpawnRequestInfo
		{
			Spawnable = spawnable,
			Position = position,
			Rotation = Quaternion.identity,
			SpawnEffect = false,
			SpawnCallback = delegate(NetworkAssetSpawner.SpawnCallbackInfo info)
			{
				GameObject val2 = null;
				try
				{
					_spawnedLoadoutItems.Add(info.Entity.ID);
					val2 = info.Spawned;
				}
				catch
				{
				}
				try
				{
					onSpawned?.Invoke(val2);
				}
				catch
				{
				}
				if ((Object)(object)val2 != (Object)null)
				{
					MelonCoroutines.Start(HolsterAfterInit(val2, preferLowSlot));
				}
				onDone?.Invoke();
			}
		});
	}

	private IEnumerator HolsterAfterInit(GameObject item, bool preferLowSlot)
	{
		yield return (object)new WaitForSeconds(0.2f);
		if (!((Object)(object)item == (Object)null) && !IsHeldByLocal(item))
		{
			TryHolster(item, preferLowSlot);
		}
	}

	private static bool IsHeldByLocal(GameObject go)
	{
		if ((Object)(object)go == (Object)null || !RigData.HasPlayer)
		{
			return false;
		}
		try
		{
			PhysicsRig physicsRig = RigData.Refs.RigManager.physicsRig;
			Hand[] array = (Hand[])(object)new Hand[2] { physicsRig.leftHand, physicsRig.rightHand };
			foreach (Hand obj in array)
			{
				GameObject val = ((obj != null) ? obj.m_CurrentAttachedGO : null);
				if (!((Object)(object)val == (Object)null) && ((Object)(object)val == (Object)(object)go || val.transform.IsChildOf(go.transform) || go.transform.IsChildOf(val.transform)))
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

	public void TryHolster(GameObject item, bool preferLowSlot = false)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)item == (Object)null || !RigData.HasPlayer)
		{
			return;
		}
		WeaponSlot componentInChildren = item.GetComponentInChildren<WeaponSlot>(true);
		if ((Object)(object)componentInChildren == (Object)null || (Object)(object)componentInChildren.interactableHost == (Object)null)
		{
			return;
		}
		List<InventorySlotReceiver> list = new List<InventorySlotReceiver>();
		InventorySlotReceiver[] rigSlots = RigData.Refs.RigSlots;
		foreach (InventorySlotReceiver val in rigSlots)
		{
			if (!((Object)(object)val == (Object)null) && !((Object)(object)val._slottedWeapon != (Object)null) && (val.slotType & componentInChildren.slotType) != 0)
			{
				list.Add(val);
			}
		}
		try
		{
			list.Sort(delegate(InventorySlotReceiver a, InventorySlotReceiver b)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				float y = ((Component)a).transform.position.y;
				float y2 = ((Component)b).transform.position.y;
				return (!preferLowSlot) ? y2.CompareTo(y) : y.CompareTo(y2);
			});
		}
		catch
		{
		}
		foreach (InventorySlotReceiver item2 in list)
		{
			try
			{
				item2.InsertInSlot(componentInChildren.interactableHost);
			}
			catch
			{
				continue;
			}
			if ((Object)(object)item2._slottedWeapon != (Object)null)
			{
				return;
			}
		}
		rigSlots = RigData.Refs.RigSlots;
		foreach (InventorySlotReceiver val2 in rigSlots)
		{
			if (!((Object)(object)val2 == (Object)null) && !((Object)(object)val2._slottedWeapon != (Object)null))
			{
				try
				{
					val2.InsertInSlot(componentInChildren.interactableHost);
				}
				catch
				{
					continue;
				}
				if ((Object)(object)val2._slottedWeapon != (Object)null)
				{
					break;
				}
			}
		}
	}

	private void DespawnLoadoutItems()
	{
		try
		{
			LocalPlayer.ReleaseGrips();
		}
		catch
		{
		}
		foreach (ushort spawnedLoadoutItem in _spawnedLoadoutItems)
		{
			NetworkAssetSpawner.Despawn(new NetworkAssetSpawner.DespawnRequestInfo
			{
				EntityID = spawnedLoadoutItem,
				DespawnEffect = false
			});
		}
		_spawnedLoadoutItems.Clear();
	}

	private void OnAssignedToTeam(PlayerID player, Team team)
	{
		FusionOverrides.ForceUpdateOverrides();
		if (KarmaManager.Enabled)
		{
			KarmaManager.EnsurePlayer(player);
		}
		if (team == SpectatorTeam)
		{
			_deadPlayers.Add(player.SmallID);
		}
		else if (team != null)
		{
			_deadPlayers.Remove(player.SmallID);
		}
		if (player.IsMe)
		{
			_statsPage.Refresh();
		}
		if (!player.IsMe)
		{
			return;
		}
		if (team == MurdererTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#b40d19>YOU ARE THE MURDERER!</color>",
				Message = "Eliminate all Bystanders. Use your knife!",
				ShowPopup = true,
				PopupLength = 6f,
				Type = NotificationType.INFORMATION
			});
		}
		else if (team == BystanderTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#0d76ea>YOU ARE A BYSTANDER!</color>",
				Message = "There is a Murderer among you. Stay alive!",
				ShowPopup = true,
				PopupLength = 6f,
				Type = NotificationType.INFORMATION
			});
		}
		else if (team == SpectatorTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#aaaaaa>YOU ARE NOW SPECTATING</color>",
				Message = "You can hear other dead players. Wait for the round to end.",
				ShowPopup = true,
				PopupLength = 5f,
				Type = NotificationType.WARNING
			});
			LocalHealth.MortalityOverride = false;
			if (!_hasLocalDeathPos)
			{
				try
				{
					GamemodeHelper.TeleportToSpawnPoint();
				}
				catch
				{
				}
			}
			else
			{
				MelonCoroutines.Start(MoveSpectatorToBodyRoutine(_localDeathPos));
			}
		}
	}

	private static IEnumerator MoveSpectatorToBodyRoutine(Vector3 bodyPos)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		Vector3 target = bodyPos + new Vector3(0.9f, 0.75f, 0f);
		yield return (object)new WaitForSeconds(0.25f);
		for (int i = 0; i < 16; i++)
		{
			try
			{
				if (RigData.HasPlayer)
				{
					RigData.Refs.RigManager.TeleportToPosition(target);
				}
			}
			catch
			{
			}
			yield return (object)new WaitForSeconds(0.1f);
		}
	}

	private void OnRoundStarted(string value)
	{
		_roundStarted = true;
		_elapsedTime = 0f;
		_roundLengthSeconds = (int.TryParse(value, out var result) ? ((float)result * 60f) : 300f);
		SpawnLoadout();
		if (TeamManager.GetLocalTeam() != SpectatorTeam)
		{
			LocalInventory.SetAmmo(100000);
		}
		_lootManager.OnRoundStart();
		_nametagSystem.OnRoundStart();
		_lootManager.OnLootThresholdReached += OnLootThresholdReached;
	}

	private void OnLootThresholdReached()
	{
		// Bystander reached LootThreshold — spawn a revolver for them
		SpawnLoadoutItem(RevolverBarcode, false, null);
		Notifier.Send(new Notification
		{
			Title = "<color=#0d76ea>LOOT THRESHOLD REACHED!</color>",
			Message = "A revolver has been spawned at your position.",
			ShowPopup = true,
			PopupLength = 4f,
			Type = NotificationType.INFORMATION
		});
	}

	private void OnPlayerAction(PlayerID player, PlayerActionType type, PlayerID otherPlayer = null)
	{
		if (!base.IsStarted || _gameEnded)
		{
			return;
		}
		if (type == PlayerActionType.DYING_BY_OTHER_PLAYER && otherPlayer != null)
		{
			_recentKillers[player.SmallID] = otherPlayer.SmallID;
			_recentKillTime[player.SmallID] = Time.time;
			RigManager rig = null;
			NetworkPlayer player2;
			if (otherPlayer.IsMe)
			{
				rig = RigData.Refs.RigManager;
			}
			else if (NetworkPlayerManager.TryGetPlayer(otherPlayer.SmallID, out player2))
			{
				rig = player2.RigRefs?.RigManager;
			}
			string value = TryGetHeldWeaponName(rig);
			if (!string.IsNullOrEmpty(value))
			{
				_recentWeapons[player.SmallID] = value;
			}
		}
		switch (type)
		{
		case PlayerActionType.DEATH:
			OnPlayerDeath(player);
			break;
		case PlayerActionType.DYING_BY_OTHER_PLAYER:
			OnPlayerKilled(player, otherPlayer);
			break;
		}
	}

	private void OnPlayerDeath(PlayerID player)
	{
		if (_roundStarted)
		{
			_deadPlayers.Add(player.SmallID);
		}
		if (player.IsMe)
		{
			_localDied = true;
			try
			{
				_localDeathPos = GetDeathPosition(player);
				_hasLocalDeathPos = true;
			}
			catch
			{
			}
			try
			{
				LocalPlayer.ReleaseGrips();
			}
			catch
			{
			}
		}
		if (_roundStarted && player.IsMe)
		{
			Team playerTeam = TeamManager.GetPlayerTeam(player);
			string text = (playerTeam == MurdererTeam) ? "Murderer" : ((playerTeam == BystanderTeam) ? "Bystander" : "Spectator");
			string text2 = (playerTeam == MurdererTeam) ? "#b40d19" : "#0d76ea";
			string value;
			string text3 = (_recentWeapons.TryGetValue(player.SmallID, out value) ? value : "");
			string text4 = "";
			try
			{
				text4 = RigData.GetAvatarBarcode() ?? "";
			}
			catch
			{
			}
			Vector3 val = (_hasLocalDeathPos ? _localDeathPos : GetDeathPosition(player));
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			string value2 = string.Join("|", player.SmallID.ToString(), val.x.ToString(invariantCulture), val.y.ToString(invariantCulture), val.z.ToString(invariantCulture), text, text2, text3, text4);
			try
			{
				CorpseSpawnEvent.TryInvoke(value2);
			}
			catch
			{
			}
		}
		if (KarmaManager.Enabled)
		{
			KarmaManager.AwardRoundBonus(player);
		}
		if (NetworkInfo.IsHost && _roundStarted)
		{
			if (SpectatorTeam.HasPlayer(player))
			{
				return;
			}
			TeamManager.TryAssignTeam(player, SpectatorTeam);
			CheckWinConditions(player);
		}
	}

	private void OnPlayerKilled(PlayerID victim, PlayerID killer)
	{
		if (killer == null || !killer.IsMe || victim.IsMe || !_roundStarted)
		{
			return;
		}
		Team playerTeam = TeamManager.GetPlayerTeam(victim);
		if (playerTeam == SpectatorTeam)
		{
			return;
		}
		TeamManager.TryAssignTeam(victim, SpectatorTeam);
		CheckWinConditions(victim);
	}

	private Vector3 GetDeathPosition(PlayerID player)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if (player.IsMe)
		{
			if (_hasLastAlivePos)
			{
				return _localLastAlivePos;
			}
			try
			{
				if (RigData.HasPlayer && (Object)(object)RigData.Refs.Headset != (Object)null)
				{
					return RigData.Refs.Headset.position - Vector3.up * 0.6f;
				}
			}
			catch
			{
			}
			return Vector3.zero;
		}
		if (_lastKnownPositions.TryGetValue(player.SmallID, out var value))
		{
			return value;
		}
		try
		{
			if (NetworkPlayerManager.TryGetPlayer(player.SmallID, out var player2))
			{
				Transform val = player2.RigRefs?.Headset;
				if ((Object)(object)val != (Object)null)
				{
					return val.position - Vector3.up * 0.6f;
				}
			}
		}
		catch
		{
		}
		return Vector3.zero;
	}

	private void CheckWinConditions(PlayerID dyingPlayer = null)
	{
		if (!_gameEnded)
		{
			int num = CountAlive(MurdererTeam, dyingPlayer);
			int num2 = CountAlive(BystanderTeam, dyingPlayer);
			bool bystandersWin = num2 > 0 && num == 0;
			bool murderersWin = num > 0 && num2 == 0;
			if (murderersWin)
			{
				_gameEnded = true;
				MurdererVictoryEvent.TryInvoke();
				ScheduleStop(WinMusic.GetMaxDuration(WinSide.Traitors, WinMusicEnabled));
			}
			else if (bystandersWin)
			{
				_gameEnded = true;
				BystanderVictoryEvent.TryInvoke();
				ScheduleStop(WinMusic.GetMaxDuration(WinSide.Innocents, WinMusicEnabled));
			}
			else if (num2 == 0 && num == 0)
			{
				_gameEnded = true;
				ScheduleStop(0f);
			}
		}
	}

	private void ScheduleStop(float musicLength)
	{
		if (NetworkInfo.IsHost)
		{
			if (musicLength > 0.1f)
			{
				_stopScheduled = true;
				_stopDelayRemaining = musicLength + 1.5f;
			}
			else
			{
				GamemodeManager.StopGamemode();
			}
		}
	}

	private int CountAlive(Team team, PlayerID dyingPlayer)
	{
		int num = team.PlayerCount;
		if (dyingPlayer != null && team.HasPlayer(dyingPlayer))
		{
			num--;
		}
		return Mathf.Max(0, num);
	}

	private static PlayerID FindPlayerBySmallID(ushort smallId)
	{
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (playerID.SmallID == smallId)
			{
				return playerID;
			}
		}
		return null;
	}

	private bool SongsEnabledByHost()
	{
		try
		{
			if (base.Metadata.TryGetMetadata("tift_music", out var value))
			{
				return value != "0";
			}
		}
		catch
		{
		}
		return true;
	}

	private void OnCorpseSpawn(string value)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string[] array = value.Split('|');
			if (array.Length >= 8)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				ushort num = ushort.Parse(array[0]);
				Vector3 deathPosition = default(Vector3);
				deathPosition = new Vector3(float.Parse(array[1], invariantCulture), float.Parse(array[2], invariantCulture), float.Parse(array[3], invariantCulture));
				string text = array[4];
				string roleColor = array[5];
				string weaponName = (string.IsNullOrEmpty(array[6]) ? null : array[6]);
				string avatarBarcode = (string.IsNullOrEmpty(array[7]) ? null : array[7]);
				string playerName = "Unknown";
				PlayerID playerID = FindPlayerBySmallID(num);
				if (playerID != null && playerID.TryGetDisplayName(out var name))
				{
					playerName = name;
				}
				bool wasInnocentSide = text == "Bystander";
				CorpseManager.RegisterDeath(playerName, text, roleColor, deathPosition, weaponName, avatarBarcode, num, wasInnocentSide);
			}
		}
		catch
		{
		}
	}

	private static string TryGetHeldWeaponName(RigManager rig)
	{
		if ((Object)(object)rig == (Object)null)
		{
			return null;
		}
		Hand[] array = (Hand[])(object)new Hand[2]
		{
			rig.physicsRig.leftHand,
			rig.physicsRig.rightHand
		};
		foreach (Hand val in array)
		{
			try
			{
				GameObject val2 = ((val != null) ? val.m_CurrentAttachedGO : null);
				if (!((Object)(object)val2 == (Object)null))
				{
					GameObject val3 = val2;
					MarrowEntity componentInParent = val2.GetComponentInParent<MarrowEntity>();
					val3 = ((!((Object)(object)componentInParent != (Object)null)) ? ((Component)val2.transform.root).gameObject : ((Component)componentInParent).gameObject);
					string text = CleanWeaponName(((Object)val3).name);
					if (!string.IsNullOrEmpty(text))
					{
						return text;
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static string CleanWeaponName(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return null;
		}
		string text = raw.Replace("(Clone)", "").Replace("[item]", "").Replace("_", " ")
			.Trim();
		int num = text.LastIndexOf(' ');
		if (num > 0 && int.TryParse(text.Substring(num + 1), out var _))
		{
			text = text.Substring(0, num).Trim();
		}
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return null;
	}

	protected override void OnPlayerJoined(PlayerID playerId)
	{
		if (KarmaManager.Enabled)
		{
			KarmaManager.EnsurePlayer(playerId);
		}
		if (NetworkInfo.IsHost && _roundStarted)
		{
			TeamManager.TryAssignTeam(playerId, SpectatorTeam);
		}
	}

	protected override void OnPlayerLeft(PlayerID playerId)
	{
		KarmaManager.RemovePlayer(playerId);
		_lastKnownPositions.Remove(playerId.SmallID);
		_recentKillers.Remove(playerId.SmallID);
		if (NetworkInfo.IsHost && _roundStarted)
		{
			CheckWinConditions(playerId);
		}
	}

	private void OnOneMinuteLeft()
	{
		Notifier.Send(new Notification
		{
			Title = "MurderLab",
			Message = "One minute left!",
			ShowPopup = true,
			PopupLength = 4f,
			Type = NotificationType.INFORMATION
		});
	}

	private void OnBystanderVictory()
	{
		WinMusic.PlayWin(WinSide.Innocents, SongsEnabledByHost());
		bool localWon = TeamManager.GetLocalTeam() == BystanderTeam;
		SendVictoryNotification("<color=#0d76ea>BYSTANDERS WIN!</color>", "The Murderer has been eliminated!", localWon);
	}

	private void OnMurdererVictory()
	{
		WinMusic.PlayWin(WinSide.Traitors, SongsEnabledByHost());
		bool localWon = TeamManager.GetLocalTeam() == MurdererTeam;
		SendVictoryNotification("<color=#b40d19>MURDERER WINS!</color>", "All Bystanders have been eliminated!", localWon);
	}

	private void OnTimeUp()
	{
		WinMusic.PlayWin(WinSide.Innocents, SongsEnabledByHost());
		bool localWon = TeamManager.GetLocalTeam() == BystanderTeam;
		SendVictoryNotification("<color=#0d76ea>BYSTANDERS WIN!</color>", "The Murderer ran out of time!", localWon);
	}

	private void SendVictoryNotification(string title, string message, bool localWon)
	{
		Notifier.Send(new Notification
		{
			Title = title,
			Message = message + (localWon ? " YOU WON!" : " You lost."),
			ShowPopup = true,
			PopupLength = 6f,
			Type = NotificationType.INFORMATION
		});
		if (KarmaManager.Enabled && localWon)
		{
			KarmaManager.AwardRoundBonus(PlayerIDManager.LocalID);
		}
		PointItemManager.RewardBits(localWon ? 100 : 25);
	}

	protected bool OnValidateNametag(PlayerID id)
	{
		if (!base.IsStarted || !_roundStarted)
		{
			return true;
		}
		// Our NametagSystem handles all nametag rendering during rounds
		return false;
	}

	public override bool CanAttack(PlayerID player)
	{
		if (!base.IsStarted || !_roundStarted)
		{
			return !base.IsStarted;
		}
		Team localTeam = TeamManager.GetLocalTeam();
		Team playerTeam = TeamManager.GetPlayerTeam(player);
		if (localTeam == SpectatorTeam || playerTeam == SpectatorTeam)
		{
			return false;
		}
		return true;
	}
}

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

	private enum JesterDeath
	{
		BecamePsychopath,
		KilledByEnemy,
		NoKillerYet
	}

	public const string BrainwashGunBarcode = "JonLandon.BrainwashingGunTFT.Spawnable.BrainwashingGun";

	private const string HypnotistMetaKey = "tift_hypnotist";

	private ushort _hypnotistSmallID;

	private bool _brainwashGunGiven;

	private float _brainwashGunRetry;

	private const string MusicMetaKey = "tift_music";

	private const string OneStabMetaKey = "tift_onestab";

	public const string OneStabKnifeBarcode = "JonLandon.TFTknife.Spawnable.TFTknife";

	private GameObject _oneStabKnife;

	private string _localPrimaryBarcode;

	private readonly HashSet<ushort> _deadPlayers = new HashSet<ushort>();

	private float _killedJesterTime = -999f;

	private const string TraitorColor = "#ff3333";

	private const string InnocentColor = "#33ff33";

	private const string DetectiveColor = "#3399ff";

	private const string SpectatorColor = "#aaaaaa";

	public const string JesterColor = "#ff69b4";

	public const string PsychopathColor = "#cc0000";

	public const string LoneWolfColor = "#ff8c00";

	public const string GlitchColor = "#00ffff";

	public const string HypnotistColor = "#9b30ff";

	public const string ZombieColor = "#5ecc1a";

	private readonly MusicPlaylist _playlist = new MusicPlaylist();

	private readonly TeamManager _teamManager = new TeamManager();

	private readonly Team _traitorTeam = new Team("Traitors");

	private readonly Team _innocentTeam = new Team("Innocents");

	private readonly Team _detectiveTeam = new Team("Detectives");

	private readonly Team _spectatorTeam = new Team("Spectators");

	private readonly Team _jesterTeam = new Team("Jesters");

	private readonly Team _psychopathTeam = new Team("Psychopaths");

	private readonly Team _loneWolfTeam = new Team("Lone Wolves");

	private readonly Team _glitchTeam = new Team("Glitches");

	private readonly Team _zombieTeam = new Team("Zombies");

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

	private int _lastWristTP = int.MinValue;

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

	private readonly HypnotistController _hypnotist = new HypnotistController();

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

	public override string Description => "Trouble In Traitor Town, in BONELAB! Traitors secretly eliminate Innocents. The Detective leads the good side. New roles: Jester (trick an Innocent into killing you to become Psychopath!), Lone Wolf (be the last one standing), and Glitch (appears as Traitor but wins with Innocents).";

	public int TraitorCount { get; set; } = 1;

	public int PrepSeconds { get; set; } = 5;

	public int RoundMinutes { get; set; } = 5;

	public bool WinMusicEnabled { get; set; } = true;

	public int LootThreshold { get; set; } = 5;

	public int GameVariant { get; set; } = 0;

	public bool SprintEnabled { get; set; } = true;

	public bool FootprintsEnabled { get; set; } = true;

	public bool BlackFogEnabled { get; set; } = true;

	public bool DisguiseEnabled { get; set; } = true;

	public bool OneStabKnifeEnabled { get; set; } = true;

	public bool DetectiveEnabled { get; set; } = true;

	public bool JesterEnabled { get; set; } = true;

	public bool LoneWolfEnabled { get; set; } = true;

	public bool GlitchEnabled { get; set; } = true;

	public bool ZombieEnabled { get; set; } = true;

	public bool HypnotistEnabled { get; set; } = true;

	public int BlackFogTimer { get; set; } = 120;

	public bool RemoveDisguiseOnKill { get; set; } = true;

	public int MinPlayers { get; set; } = 3;

	public override bool DisableDevTools => true;

	public override bool DisableSpawnGun => true;

	public override bool DisableManualUnragdoll => true;

	public MusicPlaylist Playlist => _playlist;

	public TeamManager TeamManager => _teamManager;

	public Team TraitorTeam => _traitorTeam;

	public Team InnocentTeam => _innocentTeam;

	public Team DetectiveTeam => _detectiveTeam;

	public Team SpectatorTeam => _spectatorTeam;

	public Team JesterTeam => _jesterTeam;

	public Team PsychopathTeam => _psychopathTeam;

	public Team LoneWolfTeam => _loneWolfTeam;

	public Team GlitchTeam => _glitchTeam;

	public Team ZombieTeam => _zombieTeam;

	public TriggerEvent RoundStartedEvent { get; set; }

	public TriggerEvent OneMinuteLeftEvent { get; set; }

	public TriggerEvent InnocentVictoryEvent { get; set; }

	public TriggerEvent TraitorVictoryEvent { get; set; }

	public TriggerEvent PsychopathVictoryEvent { get; set; }

	public TriggerEvent LoneWolfVictoryEvent { get; set; }

	public TriggerEvent TimeUpEvent { get; set; }

	public TriggerEvent BrainwashEvent { get; set; }

	public TriggerEvent ZombieVictoryEvent { get; set; }

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
		TeamManager.AddTeam(TraitorTeam);
		TeamManager.AddTeam(InnocentTeam);
		TeamManager.AddTeam(DetectiveTeam);
		TeamManager.AddTeam(SpectatorTeam);
		TeamManager.AddTeam(JesterTeam);
		TeamManager.AddTeam(PsychopathTeam);
		TeamManager.AddTeam(LoneWolfTeam);
		TeamManager.AddTeam(GlitchTeam);
		TeamManager.AddTeam(ZombieTeam);
		TeamManager.OnAssignedToTeam += OnAssignedToTeam;
		RoundStartedEvent = new TriggerEvent("RoundStarted", base.Relay, serverOnly: true);
		RoundStartedEvent.OnTriggeredWithValue += OnRoundStarted;
		OneMinuteLeftEvent = new TriggerEvent("OneMinuteLeft", base.Relay, serverOnly: true);
		OneMinuteLeftEvent.OnTriggered += OnOneMinuteLeft;
		InnocentVictoryEvent = new TriggerEvent("InnocentVictory", base.Relay, serverOnly: true);
		InnocentVictoryEvent.OnTriggered += OnInnocentVictory;
		TraitorVictoryEvent = new TriggerEvent("TraitorVictory", base.Relay, serverOnly: true);
		TraitorVictoryEvent.OnTriggered += OnTraitorVictory;
		PsychopathVictoryEvent = new TriggerEvent("PsychopathVictory", base.Relay, serverOnly: true);
		PsychopathVictoryEvent.OnTriggered += OnPsychopathVictory;
		LoneWolfVictoryEvent = new TriggerEvent("LoneWolfVictory", base.Relay, serverOnly: true);
		LoneWolfVictoryEvent.OnTriggered += OnLoneWolfVictory;
		TimeUpEvent = new TriggerEvent("TimeUp", base.Relay, serverOnly: true);
		TimeUpEvent.OnTriggered += OnTimeUp;
		ZombieVictoryEvent = new TriggerEvent("ZombieVictory", base.Relay, serverOnly: true);
		ZombieVictoryEvent.OnTriggered += OnZombieVictory;
		BrainwashEvent = new TriggerEvent("Brainwash", base.Relay);
		BrainwashEvent.OnTriggeredWithValue += OnBrainwash;
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
			MelonLogger.Warning("[FordTown] Roles Info menu unavailable: " + ex2.Message);
		}
		try
		{
			WinMusic.Initialize();
		}
		catch (Exception ex3)
		{
			MelonLogger.Warning("[FordTown] Win music unavailable: " + ex3.Message);
		}
		HypnotistController hypnotist = _hypnotist;
		hypnotist.OnBrainwashComplete = (Action<ushort>)Delegate.Combine(hypnotist.OnBrainwashComplete, (Action<ushort>)delegate(ushort targetId)
		{
			try
			{
				BrainwashEvent.TryInvoke(targetId.ToString());
			}
			catch
			{
			}
			Notifier.Send(new Notification
			{
				Title = "<color=#9b30ff>BRAINWASH COMPLETE!</color>",
				Message = "Their corpse has been turned into a Traitor.",
				ShowPopup = true,
				PopupLength = 4f,
				Type = NotificationType.INFORMATION
			});
		});
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
		InnocentVictoryEvent.UnregisterEvent();
		InnocentVictoryEvent = null;
		TraitorVictoryEvent.UnregisterEvent();
		TraitorVictoryEvent = null;
		PsychopathVictoryEvent.UnregisterEvent();
		PsychopathVictoryEvent = null;
		LoneWolfVictoryEvent.UnregisterEvent();
		LoneWolfVictoryEvent = null;
		TimeUpEvent.UnregisterEvent();
		TimeUpEvent = null;
		ZombieVictoryEvent.UnregisterEvent();
		ZombieVictoryEvent = null;
		BrainwashEvent.UnregisterEvent();
		BrainwashEvent = null;
		CorpseSpawnEvent.UnregisterEvent();
		CorpseSpawnEvent = null;
		_hypnotist.Cleanup();
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
		_brainwashGunGiven = false;
		_brainwashGunRetry = 0f;
		_hypnotistSmallID = 0;
		_roundLengthSeconds = 0f;
		_spawnedLoadoutItems.Clear();
		_lastKnownPositions.Clear();
		_recentKillers.Clear();
		_recentKillTime.Clear();
		_recentWeapons.Clear();
		_deadPlayers.Clear();
		_killedJesterTime = -999f;
		_localPrimaryBarcode = null;
		_oneStabKnife = null;
		OneStabState.LocalHolding = false;
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
			try
			{
				base.Metadata.TrySetMetadata("tift_onestab", OneStabKnifeEnabled ? "1" : "0");
			}
			catch
			{
			}
			try
			{
				base.Metadata.TrySetMetadata("tift_hypnotist", "0");
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
		_oneStabKnife = null;
		OneStabState.LocalHolding = false;
		ClearSpectatorEffects();
		_wristIndicator.Destroy();
		_roleLabels.ClearAll();
		_holsterHider.ClearAll();
		ZombieState.LocalIsZombie = false;
		CorpseManager.ClearAll();
		CorpseManager.DestroyUI();
		TraitorShopRadial.RemoveMenuItems();
		CustomWeapons.Reset();
		TraitorPoints.Clear();
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
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		if (!base.IsStarted)
		{
			return;
		}
		_elapsedTime += TimeReferences.DeltaTime;
		ZombieState.LocalIsZombie = _roundStarted && TeamManager.GetLocalTeam() == ZombieTeam;
		OneStabState.LocalHolding = _roundStarted && (Object)(object)_oneStabKnife != (Object)null && IsHeldByLocal(_oneStabKnife);
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
		_roleLabels.Update(this);
		if (_roundStarted)
		{
			_holsterHider.Update();
		}
		if (_roundStarted && TeamManager.GetLocalTeam() == TraitorTeam && IsLocalHypnotist())
		{
			if (!_brainwashGunGiven)
			{
				_brainwashGunRetry -= TimeReferences.DeltaTime;
				if (_brainwashGunRetry <= 0f)
				{
					_brainwashGunRetry = 1.5f;
					if (LocalHasBrainwashGun())
					{
						_brainwashGunGiven = true;
					}
					else if (RigData.HasPlayer)
					{
						GiveBrainwashGun();
					}
				}
			}
			_hypnotist.Update("JonLandon.BrainwashingGunTFT.Spawnable.BrainwashingGun");
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
		List<PlayerID> list = new List<PlayerID>(PlayerIDManager.PlayerIDs);
		list.Shuffle();
		int count = list.Count;
		int num = Math.Min(TraitorCount, Math.Max(1, count / 3));
		for (int i = 0; i < num; i++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			TeamManager.TryAssignTeam(list[0], TraitorTeam);
			list.RemoveAt(0);
		}
		if (DetectiveEnabled && list.Count >= 2)
		{
			TeamManager.TryAssignTeam(list[0], DetectiveTeam);
			list.RemoveAt(0);
		}
		if (count >= 4 && JesterEnabled && list.Count > 1)
		{
			TeamManager.TryAssignTeam(list[0], JesterTeam);
			list.RemoveAt(0);
		}
		if (count >= 4 && LoneWolfEnabled && list.Count > 1)
		{
			TeamManager.TryAssignTeam(list[0], LoneWolfTeam);
			list.RemoveAt(0);
		}
		if (count >= 4 && GlitchEnabled && list.Count > 1)
		{
			TeamManager.TryAssignTeam(list[0], GlitchTeam);
			list.RemoveAt(0);
		}
		if (count >= 4 && ZombieEnabled && list.Count > 1)
		{
			TeamManager.TryAssignTeam(list[0], ZombieTeam);
			list.RemoveAt(0);
		}
		_hypnotistSmallID = 0;
		if (count >= 4 && HypnotistEnabled && list.Count > 1)
		{
			PlayerID playerID = list[0];
			TeamManager.TryAssignTeam(playerID, TraitorTeam);
			_hypnotistSmallID = playerID.SmallID;
			list.RemoveAt(0);
			try
			{
				base.Metadata.TrySetMetadata("tift_hypnotist", _hypnotistSmallID.ToString());
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				base.Metadata.TrySetMetadata("tift_hypnotist", "0");
			}
			catch
			{
			}
		}
		foreach (PlayerID item in list)
		{
			TeamManager.TryAssignTeam(item, InnocentTeam);
		}
	}

	private void ShowPrepRoleText()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
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
		if (localTeam == TraitorTeam)
		{
			// Murderer text
			title = "You are the murderer";
			subtitle = "Kill everyone";
			footer1 = "Don't get caught";
			footer2 = "";
			titleColor = Color.red;
			subtitleColor = Color.red;
		}
		else if (localTeam == DetectiveTeam)
		{
			// Bystander with revolver
			title = "You are a bystander";
			subtitle = "with a secret weapon";
			footer1 = "There is a murderer on the loose";
			footer2 = "Find and kill him";
			titleColor = Color.blue;
			subtitleColor = new Color(0.5f, 0f, 0.5f);
		}
		else
		{
			// Bystander (normal)
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
			int points = TraitorPoints.Points;
			object obj = (_roundStarted ? TeamManager.GetLocalTeam() : null);
			if (!_wristTextInit || num != _lastWristSecond || points != _lastWristTP || obj != _lastWristTeam)
			{
				_wristTextInit = true;
				_lastWristSecond = num;
				_lastWristTP = points;
				_lastWristTeam = obj;
				_wristIndicator.SetText(GetWristText());
			}
			_wristIndicator.Update();
		}
	}

	private string GetWristText()
	{
		if (!_roundStarted)
		{
			return "GET READY\nRoles incoming...";
		}
		string text = TimeSpan.FromSeconds(Mathf.Max(_roundLengthSeconds - _elapsedTime, 0f)).ToString("mm\\:ss");
		Team localTeam = TeamManager.GetLocalTeam();
		string text2 = ((localTeam == TraitorTeam && IsLocalHypnotist()) ? $"<color={"#9b30ff"}>HYPNOTIST</color>\nTP: {TraitorPoints.Points}" : ((localTeam == TraitorTeam) ? $"<color={"#ff3333"}>TRAITOR</color>\nTP: {TraitorPoints.Points}" : ((localTeam == DetectiveTeam) ? "<color=#3399ff>DETECTIVE</color>" : ((localTeam == InnocentTeam) ? "<color=#33ff33>INNOCENT</color>" : ((localTeam == JesterTeam) ? "<color=#ff69b4>JESTER</color>" : ((localTeam == PsychopathTeam) ? "<color=#cc0000>PSYCHOPATH</color>" : ((localTeam == LoneWolfTeam) ? "<color=#ff8c00>LONE WOLF</color>" : ((localTeam == GlitchTeam) ? "<color=#00ffff>GLITCH</color>" : ((localTeam == ZombieTeam) ? "<color=#5ecc1a>ZOMBIE</color>" : ((localTeam != SpectatorTeam) ? "NO ROLE" : "<color=#aaaaaa>SPECTATING</color>"))))))))));
		return text2 + "\n" + text;
	}

	private void SpawnLoadout()
	{
		Team localTeam = TeamManager.GetLocalTeam();
		if (localTeam != SpectatorTeam && localTeam != ZombieTeam)
		{
			System.Random random = new System.Random();
			string item = Defaults.Sidearms[random.Next(Defaults.Sidearms.Length)];
			string item2 = (_localPrimaryBarcode = Defaults.Primaries[random.Next(Defaults.Primaries.Length)]);
			List<(string, bool)> queue = new List<(string, bool)>
			{
				(item, true),
				(item2, false)
			};
			SpawnLoadoutQueue(queue, 0);
		}
	}

	public void GiveTraitorKnife()
	{
		GiveKnife(useOneStabIfEnabled: true);
	}

	public void GiveZombieKnife()
	{
		GiveKnife(useOneStabIfEnabled: false);
	}

	private void GiveKnife(bool useOneStabIfEnabled)
	{
		bool oneStab = useOneStabIfEnabled && OneStabEnabledByHost();
		if (oneStab && !SpawnableExists("JonLandon.TFTknife.Spawnable.TFTknife"))
		{
			MelonLogger.Warning("[FordTown] One-stab knife 'JonLandon.TFTknife.Spawnable.TFTknife' is not a loaded spawnable — the mod isn't installed, or the barcode is the pallet barcode instead of the CRATE/spawnable barcode (should look like Author.Pallet.Spawnable.Name). Giving the default knife instead.");
			oneStab = false;
		}
		string barcode = (oneStab ? "JonLandon.TFTknife.Spawnable.TFTknife" : "c1534c5a-1fb8-477c-afbe-2a95436f6d62");
		SpawnLoadoutItem(barcode, preferLowSlot: true, null, delegate(GameObject go)
		{
			HolsterHider.RegisterLocalKnife(go);
			if (oneStab)
			{
				_oneStabKnife = go;
			}
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

	private void MakeOneStabKnifeUngrabbable()
	{
		GameObject oneStabKnife = _oneStabKnife;
		_oneStabKnife = null;
		OneStabState.LocalHolding = false;
		if ((Object)(object)oneStabKnife == (Object)null)
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
		try
		{
			foreach (Grip componentsInChild in oneStabKnife.GetComponentsInChildren<Grip>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null)
				{
					try
					{
						((Behaviour)componentsInChild).enabled = false;
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

	private void SpawnLoadoutQueue(List<(string barcode, bool low)> queue, int index)
	{
		if (index < queue.Count)
		{
			var (barcode, preferLowSlot) = queue[index];
			SpawnLoadoutItem(barcode, preferLowSlot, delegate
			{
				SpawnLoadoutQueue(queue, index + 1);
			});
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

	private void GiveBrainwashGun()
	{
		if (string.IsNullOrEmpty("JonLandon.BrainwashingGunTFT.Spawnable.BrainwashingGun"))
		{
			_brainwashGunGiven = true;
			MelonLogger.Warning("[FordTown] Hypnotist assigned but BrainwashGunBarcode is not set — no gun spawned.");
			Notifier.Send(new Notification
			{
				Title = "<color=#9b30ff>YOU ARE THE HYPNOTIST!</color>",
				Message = "Brainwash a dead Innocent back to life as a Traitor by aiming your gun at their corpse.",
				ShowPopup = true,
				PopupLength = 8f,
				Type = NotificationType.INFORMATION
			});
			return;
		}
		SpawnLoadoutItem("JonLandon.BrainwashingGunTFT.Spawnable.BrainwashingGun", preferLowSlot: false, null, delegate(GameObject go)
		{
			if (!((Object)(object)go == (Object)null))
			{
				_brainwashGunGiven = true;
				Notifier.Send(new Notification
				{
					Title = "<color=#9b30ff>YOU ARE THE HYPNOTIST!</color>",
					Message = "Aim your brainwashing gun at a dead Innocent's corpse for 10s to revive them as a Traitor.",
					ShowPopup = true,
					PopupLength = 8f,
					Type = NotificationType.INFORMATION
				});
			}
		});
	}

	private bool LocalHasBrainwashGun()
	{
		if (!RigData.HasPlayer)
		{
			return false;
		}
		try
		{
			foreach (Poolee componentsInChild in ((Component)RigData.Refs.RigManager).GetComponentsInChildren<Poolee>(true))
			{
				object obj;
				if (componentsInChild == null)
				{
					obj = null;
				}
				else
				{
					SpawnableCrate spawnableCrate = componentsInChild.SpawnableCrate;
					if (spawnableCrate == null)
					{
						obj = null;
					}
					else
					{
						Barcode barcode = ((Scannable)spawnableCrate).Barcode;
						obj = ((barcode != null) ? barcode.ID : null);
					}
				}
				if ((string?)obj == "JonLandon.BrainwashingGunTFT.Spawnable.BrainwashingGun")
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
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_043d: Unknown result type (might be due to invalid IL or missing references)
		//IL_054f: Unknown result type (might be due to invalid IL or missing references)
		FusionOverrides.ForceUpdateOverrides();
		if (player.IsMe && team != null && !_roundStarted)
		{
			_roundStarted = true;
		}
		if (KarmaManager.Enabled)
		{
			KarmaManager.EnsurePlayer(player);
		}
		if (team == PsychopathTeam || team == ZombieTeam)
		{
			try
			{
				CorpseManager.RemoveCorpseOf(player.SmallID);
			}
			catch
			{
			}
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
		if (team == DetectiveTeam && !player.IsMe)
		{
			player.TryGetDisplayName(out var name);
			Notifier.Send(new Notification
			{
				Title = "<color=#3399ff>DETECTIVE</color>",
				Message = name + " is the Detective!",
				ShowPopup = true,
				PopupLength = 5f,
				Type = NotificationType.INFORMATION
			});
		}
		if (!player.IsMe)
		{
			return;
		}
		if (team == TraitorTeam)
		{
			TraitorShopRadial.AddMenuItems(this);
		}
		else
		{
			TraitorShopRadial.RemoveMenuItems();
		}
		if (team == TraitorTeam)
		{
			if (_localDied)
			{
				_localDied = false;
				LocalHealth.MortalityOverride = true;
				LocalHealth.SetFullHealth();
				MelonCoroutines.Start(ReviveAtPositionRoutine(_hasLocalDeathPos ? _localDeathPos : Vector3.zero));
				Notifier.Send(new Notification
				{
					Title = "<color=#9b30ff>YOU'VE BEEN BRAINWASHED!</color>",
					Message = "The Hypnotist revived you — you are now a TRAITOR. Eliminate the Innocents!",
					ShowPopup = true,
					PopupLength = 8f,
					Type = NotificationType.INFORMATION
				});
			}
			else
			{
				Notifier.Send(new Notification
				{
					Title = "<color=#ff3333>YOU ARE A TRAITOR!</color>",
					Message = "Eliminate all Innocents. Open the Traitor Shop in the radial menu!",
					ShowPopup = true,
					PopupLength = 6f,
					Type = NotificationType.INFORMATION
				});
			}
		}
		else if (team == InnocentTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#33ff33>YOU ARE INNOCENT!</color>",
				Message = "Find and eliminate the Traitors!",
				ShowPopup = true,
				PopupLength = 6f,
				Type = NotificationType.INFORMATION
			});
		}
		else if (team == DetectiveTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#3399ff>YOU ARE THE DETECTIVE!</color>",
				Message = "Lead the Innocents and find the Traitors!",
				ShowPopup = true,
				PopupLength = 6f,
				Type = NotificationType.INFORMATION
			});
		}
		else if (team == JesterTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#ff69b4>YOU ARE THE JESTER!</color>",
				Message = "Trick an Innocent into killing you to become the Psychopath! If a Traitor kills you instead, you lose.",
				ShowPopup = true,
				PopupLength = 8f,
				Type = NotificationType.INFORMATION
			});
		}
		else if (team == PsychopathTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#cc0000>YOU ARE NOW THE PSYCHOPATH!</color>",
				Message = "Second chance! Kill everyone to win!",
				ShowPopup = true,
				PopupLength = 6f,
				Type = NotificationType.INFORMATION
			});
			LocalHealth.MortalityOverride = true;
			LocalHealth.VitalityOverride = 1.5f;
			try
			{
				GamemodeHelper.TeleportToSpawnPoint();
			}
			catch
			{
			}
			MelonCoroutines.Start(RevicePsychopathRoutine());
			if (!string.IsNullOrEmpty(_localPrimaryBarcode))
			{
				SpawnLoadoutItem(_localPrimaryBarcode);
			}
		}
		else if (team == LoneWolfTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#ff8c00>YOU ARE THE LONE WOLF!</color>",
				Message = "Kill everyone and be the last one standing to win!",
				ShowPopup = true,
				PopupLength = 6f,
				Type = NotificationType.INFORMATION
			});
		}
		else if (team == GlitchTeam)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#00ffff>YOU ARE THE GLITCH!</color>",
				Message = "You appear as a Traitor to Traitors — but you're secretly Innocent. Help Innocents win from the inside!",
				ShowPopup = true,
				PopupLength = 8f,
				Type = NotificationType.INFORMATION
			});
		}
		else if (team == ZombieTeam)
		{
			bool localDied = _localDied;
			if (localDied)
			{
				_localDied = false;
				LocalHealth.MortalityOverride = true;
				LocalHealth.SetFullHealth();
				MelonCoroutines.Start(ReviveAtPositionRoutine(_hasLocalDeathPos ? _localDeathPos : Vector3.zero));
			}
			Notifier.Send(new Notification
			{
				Title = "<color=#5ecc1a>YOU ARE A ZOMBIE!</color>",
				Message = (localDied ? "You've been infected! Stab others with your knife to turn them into Zombies too." : "Patient zero! Stab others with your knife to spread the infection. Turn everyone!"),
				ShowPopup = true,
				PopupLength = 8f,
				Type = NotificationType.INFORMATION
			});
			GiveZombieKnife();
		}
		else
		{
			if (team != SpectatorTeam || !_roundStarted)
			{
				return;
			}
			bool num = Time.time - _killedJesterTime <= 5f;
			_killedJesterTime = -999f;
			string text = (num ? "<color=#ff69b4>THE JESTER TRICKED YOU!</color>" : "YOU ARE NOW SPECTATING");
			string text2 = (num ? "You killed the Jester — now you pay for it. Spectate until the round ends." : "You can hear other dead players. Wait for the round to end.");
			Notifier.Send(new Notification
			{
				Title = text,
				Message = text2,
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
					return;
				}
				catch
				{
					return;
				}
			}
			MelonCoroutines.Start(MoveSpectatorToBodyRoutine(_localDeathPos));
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
		if (TeamManager.GetLocalTeam() != TraitorTeam)
		{
			return;
		}
		TraitorPoints.Reset();
		_statsPage.Refresh();
		List<string> list = new List<string>();
		foreach (byte player in TraitorTeam.Players)
		{
			PlayerID playerID = FindPlayerBySmallID(player);
			if (playerID != null && !playerID.IsMe && playerID.TryGetDisplayName(out var name))
			{
				list.Add(name);
			}
		}
		foreach (byte player2 in GlitchTeam.Players)
		{
			PlayerID playerID2 = FindPlayerBySmallID(player2);
			if (playerID2 != null && playerID2.TryGetDisplayName(out var name2))
			{
				list.Add(name2);
			}
		}
		string text = ((list.Count > 0) ? ("Your crew: " + string.Join(", ", list)) : "You are the only Traitor.");
		Notifier.Send(new Notification
		{
			Title = "<color=#ff3333>TRAITORS</color>",
			Message = text,
			ShowPopup = true,
			PopupLength = 7f,
			Type = NotificationType.INFORMATION
		});
		if (JesterTeam.PlayerCount <= 0)
		{
			return;
		}
		List<string> list2 = new List<string>();
		foreach (byte player3 in JesterTeam.Players)
		{
			PlayerID playerID3 = FindPlayerBySmallID(player3);
			if (playerID3 != null && playerID3.TryGetDisplayName(out var name3))
			{
				list2.Add(name3);
			}
		}
		if (list2.Count > 0)
		{
			Notifier.Send(new Notification
			{
				Title = "<color=#ff69b4>JESTER ALERT</color>",
				Message = "Jester: " + string.Join(", ", list2) + " — if an Innocent kills them, they become Psychopath!",
				ShowPopup = true,
				PopupLength = 6f,
				Type = NotificationType.WARNING
			});
		}
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
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
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
			string text;
			string text2;
			if (playerTeam == TraitorTeam)
			{
				text = "Traitor";
				text2 = "#ff3333";
			}
			else if (playerTeam == DetectiveTeam)
			{
				text = "Detective";
				text2 = "#3399ff";
			}
			else if (playerTeam == JesterTeam)
			{
				text = "Jester";
				text2 = "#ff69b4";
			}
			else if (playerTeam == PsychopathTeam)
			{
				text = "Psychopath";
				text2 = "#cc0000";
			}
			else if (playerTeam == LoneWolfTeam)
			{
				text = "Lone Wolf";
				text2 = "#ff8c00";
			}
			else if (playerTeam == GlitchTeam)
			{
				text = "Innocent";
				text2 = "#33ff33";
			}
			else if (playerTeam == ZombieTeam)
			{
				text = "Zombie";
				text2 = "#5ecc1a";
			}
			else if (playerTeam == SpectatorTeam)
			{
				text = "Spectator";
				text2 = "#aaaaaa";
			}
			else
			{
				text = "Innocent";
				text2 = "#33ff33";
			}
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
		if (!NetworkInfo.IsHost || !_roundStarted)
		{
			return;
		}
		if (JesterTeam.HasPlayer(player))
		{
			switch (ResolveJesterDeath(player))
			{
			case JesterDeath.BecamePsychopath:
				return;
			case JesterDeath.NoKillerYet:
				MelonCoroutines.Start(ResolveJesterDeathLater(player));
				return;
			}
		}
		FinishDeathResolution(player);
	}

	private JesterDeath ResolveJesterDeath(PlayerID player)
	{
		if (!_recentKillTime.TryGetValue(player.SmallID, out var value) || !(Time.time - value <= 4f) || !_recentKillers.TryGetValue(player.SmallID, out var value2))
		{
			return JesterDeath.NoKillerYet;
		}
		PlayerID playerID = FindPlayerBySmallID(value2);
		Team team = ((playerID != null) ? TeamManager.GetPlayerTeam(playerID) : null);
		if (team == null)
		{
			return JesterDeath.NoKillerYet;
		}
		_recentKillers.Remove(player.SmallID);
		_recentKillTime.Remove(player.SmallID);
		if (team == InnocentTeam || team == DetectiveTeam || team == GlitchTeam)
		{
			TeamManager.TryAssignTeam(player, PsychopathTeam);
			if (playerID != null && !SpectatorTeam.HasPlayer(playerID))
			{
				TeamManager.TryAssignTeam(playerID, SpectatorTeam);
			}
			CheckWinConditions();
			return JesterDeath.BecamePsychopath;
		}
		return JesterDeath.KilledByEnemy;
	}

	private IEnumerator ResolveJesterDeathLater(PlayerID player)
	{
		float deadline = Time.time + 2f;
		while (Time.time < deadline)
		{
			yield return (object)new WaitForSeconds(0.15f);
			if (player == null || !base.IsStarted || !NetworkInfo.IsHost || !JesterTeam.HasPlayer(player))
			{
				yield break;
			}
			JesterDeath jesterDeath = ResolveJesterDeath(player);
			if (jesterDeath == JesterDeath.BecamePsychopath)
			{
				yield break;
			}
			if (jesterDeath == JesterDeath.KilledByEnemy)
			{
				break;
			}
		}
		if (base.IsStarted && NetworkInfo.IsHost && JesterTeam.HasPlayer(player))
		{
			FinishDeathResolution(player);
		}
	}

	private void FinishDeathResolution(PlayerID player)
	{
		if (!ZombieTeam.HasPlayer(player) && _recentKillTime.TryGetValue(player.SmallID, out var value) && Time.time - value <= 4f && _recentKillers.TryGetValue(player.SmallID, out var value2))
		{
			PlayerID playerID = FindPlayerBySmallID(value2);
			if (playerID != null && ZombieTeam.HasPlayer(playerID))
			{
				_recentKillers.Remove(player.SmallID);
				_recentKillTime.Remove(player.SmallID);
				TeamManager.TryAssignTeam(player, ZombieTeam);
				CheckWinConditions();
				return;
			}
		}
		if (!SpectatorTeam.HasPlayer(player))
		{
			TeamManager.TryAssignTeam(player, SpectatorTeam);
		}
		CheckWinConditions(player);
	}

	private void OnPlayerKilled(PlayerID victim, PlayerID killer)
	{
		if (killer == null || !killer.IsMe || victim.IsMe || !_roundStarted)
		{
			return;
		}
		Team localTeam = TeamManager.GetLocalTeam();
		Team playerTeam = TeamManager.GetPlayerTeam(victim);
		if (playerTeam == SpectatorTeam)
		{
			return;
		}
		if ((Object)(object)_oneStabKnife != (Object)null && IsHeldByLocal(_oneStabKnife))
		{
			MakeOneStabKnifeUngrabbable();
		}
		if (playerTeam == JesterTeam && (localTeam == InnocentTeam || localTeam == DetectiveTeam || localTeam == GlitchTeam))
		{
			_killedJesterTime = Time.time;
		}
		bool flag = localTeam == TraitorTeam;
		bool flag2 = localTeam == GlitchTeam;
		bool flag3 = playerTeam == TraitorTeam;
		bool flag4 = playerTeam == GlitchTeam;
		bool flag5 = playerTeam == InnocentTeam || playerTeam == DetectiveTeam;
		if (flag && flag4)
		{
			return;
		}
		bool flag6 = (flag && !flag3) || (!flag && !flag2 && flag3) || (flag2 && flag3);
		if (flag6)
		{
			PointItemManager.RewardBits(50);
		}
		if (KarmaManager.Enabled)
		{
			if (flag6)
			{
				KarmaManager.AdjustKarma(PlayerIDManager.LocalID, 200);
			}
			else if (flag && flag3)
			{
				KarmaManager.AdjustKarma(PlayerIDManager.LocalID, -400);
			}
			else if (!flag && flag5)
			{
				KarmaManager.AdjustKarma(PlayerIDManager.LocalID, -300);
			}
		}
		if (flag && !flag3 && !flag4)
		{
			TraitorPoints.AddKillReward();
		}
		if (flag)
		{
			CustomWeapons.OnLocalKill();
		}
	}

	private static IEnumerator RevicePsychopathRoutine()
	{
		for (int i = 0; i < 10; i++)
		{
			try
			{
				if (RigData.HasPlayer)
				{
					LocalHealth.SetFullHealth();
					LocalRagdoll.ToggleRagdoll(ragdolled: false);
				}
			}
			catch
			{
			}
			yield return (object)new WaitForSeconds(0.15f);
		}
	}

	private static IEnumerator ReviveAtPositionRoutine(Vector3 pos)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 12; i++)
		{
			try
			{
				if (RigData.HasPlayer)
				{
					LocalHealth.SetFullHealth();
					LocalRagdoll.ToggleRagdoll(ragdolled: false);
					if (pos != Vector3.zero)
					{
						RigData.Refs.RigManager.TeleportToPosition(pos + Vector3.up * 0.1f);
					}
				}
			}
			catch
			{
			}
			yield return (object)new WaitForSeconds(0.12f);
		}
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
			int num = CountAlive(TraitorTeam, dyingPlayer);
			int num2 = CountAlive(InnocentTeam, dyingPlayer) + CountAlive(DetectiveTeam, dyingPlayer) + CountAlive(GlitchTeam, dyingPlayer);
			int num3 = CountAlive(PsychopathTeam, dyingPlayer);
			int num4 = CountAlive(LoneWolfTeam, dyingPlayer);
			int num5 = CountAlive(JesterTeam, dyingPlayer);
			int num6 = CountAlive(ZombieTeam, dyingPlayer);
			bool flag = num2 > 0 && num == 0 && num3 == 0 && num4 == 0 && num5 == 0 && num6 == 0;
			bool flag2 = num > 0 && num2 == 0 && num3 == 0 && num4 == 0 && num5 == 0 && num6 == 0;
			bool num7 = num3 > 0 && num2 == 0 && num == 0 && num4 == 0 && num5 == 0 && num6 == 0;
			bool flag3 = num4 > 0 && num2 == 0 && num == 0 && num3 == 0 && num5 == 0 && num6 == 0;
			bool flag4 = num6 > 0 && num2 == 0 && num == 0 && num3 == 0 && num4 == 0 && num5 == 0;
			if (num7)
			{
				_gameEnded = true;
				PsychopathVictoryEvent.TryInvoke();
				ScheduleStop(WinMusic.GetMaxDuration(WinSide.Jester, WinMusicEnabled));
			}
			else if (flag3)
			{
				_gameEnded = true;
				LoneWolfVictoryEvent.TryInvoke();
				ScheduleStop(WinMusic.GetMaxDuration(WinSide.LoneWolf, WinMusicEnabled));
			}
			else if (flag2)
			{
				_gameEnded = true;
				TraitorVictoryEvent.TryInvoke();
				ScheduleStop(WinMusic.GetMaxDuration(WinSide.Traitors, WinMusicEnabled));
			}
			else if (flag)
			{
				_gameEnded = true;
				InnocentVictoryEvent.TryInvoke();
				ScheduleStop(WinMusic.GetMaxDuration(WinSide.Innocents, WinMusicEnabled));
			}
			else if (flag4)
			{
				_gameEnded = true;
				ZombieVictoryEvent.TryInvoke();
				ScheduleStop(0f);
			}
			else if (num2 == 0 && num == 0 && num3 == 0 && num4 == 0 && num6 == 0)
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

	private bool OneStabEnabledByHost()
	{
		try
		{
			if (base.Metadata.TryGetMetadata("tift_onestab", out var value))
			{
				return value == "1";
			}
		}
		catch
		{
		}
		return OneStabKnifeEnabled;
	}

	private ushort GetHypnotistSmallID()
	{
		try
		{
			if (base.Metadata.TryGetMetadata("tift_hypnotist", out var value) && ushort.TryParse(value, out var result))
			{
				return result;
			}
		}
		catch
		{
		}
		return _hypnotistSmallID;
	}

	private bool IsLocalHypnotist()
	{
		PlayerID localID = PlayerIDManager.LocalID;
		if (localID != null && GetHypnotistSmallID() != 0)
		{
			return localID.SmallID == GetHypnotistSmallID();
		}
		return false;
	}

	private void OnBrainwash(string value)
	{
		if (!ushort.TryParse(value, out var result) || result == 0)
		{
			return;
		}
		try
		{
			CorpseManager.RemoveCorpseOf(result);
		}
		catch
		{
		}
		if (NetworkInfo.IsHost && _roundStarted)
		{
			PlayerID playerID = FindPlayerBySmallID(result);
			if (playerID != null && SpectatorTeam.HasPlayer(playerID))
			{
				TeamManager.TryAssignTeam(playerID, TraitorTeam);
				CheckWinConditions();
			}
		}
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
				bool wasInnocentSide = text == "Innocent" || text == "Detective";
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
			Title = "Trouble In FordTown",
			Message = "One minute left! Traitors, hurry up!",
			ShowPopup = true,
			PopupLength = 4f,
			Type = NotificationType.INFORMATION
		});
	}

	private void OnInnocentVictory()
	{
		WinMusic.PlayWin(WinSide.Innocents, SongsEnabledByHost());
		Team localTeam = TeamManager.GetLocalTeam();
		bool localWon = localTeam == InnocentTeam || localTeam == DetectiveTeam || localTeam == GlitchTeam;
		SendVictoryNotification("<color=#33ff33>INNOCENTS WIN!</color>", "All Traitors have been eliminated!", localWon);
	}

	private void OnTraitorVictory()
	{
		WinMusic.PlayWin(WinSide.Traitors, SongsEnabledByHost());
		bool localWon = TeamManager.GetLocalTeam() == TraitorTeam;
		SendVictoryNotification("<color=#ff3333>TRAITORS WIN!</color>", "All Innocents have been eliminated!", localWon);
	}

	private void OnPsychopathVictory()
	{
		WinMusic.PlayWin(WinSide.Jester, SongsEnabledByHost());
		bool localWon = TeamManager.GetLocalTeam() == PsychopathTeam;
		SendVictoryNotification("<color=#ff69b4>JESTER WINS!</color>", "The Jester got the last laugh — everyone else is dead!", localWon);
	}

	private void OnLoneWolfVictory()
	{
		WinMusic.PlayWin(WinSide.LoneWolf, SongsEnabledByHost());
		bool localWon = TeamManager.GetLocalTeam() == LoneWolfTeam;
		SendVictoryNotification("<color=#ff8c00>LONE WOLF WINS!</color>", "The Lone Wolf outlasted everyone!", localWon);
	}

	private void OnZombieVictory()
	{
		bool localWon = TeamManager.GetLocalTeam() == ZombieTeam;
		SendVictoryNotification("<color=#5ecc1a>ZOMBIES WIN!</color>", "The infection spread to everyone!", localWon);
	}

	private void OnTimeUp()
	{
		WinMusic.PlayWin(WinSide.Innocents, SongsEnabledByHost());
		Team localTeam = TeamManager.GetLocalTeam();
		bool localWon = localTeam == InnocentTeam || localTeam == DetectiveTeam || localTeam == GlitchTeam;
		SendVictoryNotification("<color=#33ff33>INNOCENTS WIN!</color>", "The Traitors ran out of time!", localWon);
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
		Team localTeam = TeamManager.GetLocalTeam();
		if (localTeam == SpectatorTeam)
		{
			return true;
		}
		if (localTeam == TraitorTeam)
		{
			if (!TraitorTeam.HasPlayer(id))
			{
				return GlitchTeam.HasPlayer(id);
			}
			return true;
		}
		if (localTeam == ZombieTeam)
		{
			return ZombieTeam.HasPlayer(id);
		}
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

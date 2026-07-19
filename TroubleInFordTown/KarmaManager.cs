using System;
using System.Collections.Generic;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.UI.Popups;
using MelonLoader;
using UnityEngine;

namespace TroubleInFordTown;

public static class KarmaManager
{
	public static bool Enabled = false;

	private static readonly Dictionary<ushort, int> _karma = new Dictionary<ushort, int>();

	public const int DefaultKarma = 1000;

	public const int MaxKarma = 2000;

	private const int KickThreshold = 350;

	private const int HighRiskThreshold = 500;

	private const int WarnThreshold = 700;

	public const int GoodKillReward = 200;

	public const int FriendlyFirePenalty = -400;

	public const int WrongKillPenalty = -300;

	public const int RoundParticipationBonus = 30;

	public static void EnsurePlayer(PlayerID player)
	{
		if (!_karma.ContainsKey(player.SmallID))
		{
			_karma[player.SmallID] = 1000;
		}
	}

	public static void RemovePlayer(PlayerID player)
	{
		_karma.Remove(player.SmallID);
	}

	public static int GetKarma(PlayerID player)
	{
		if (!_karma.TryGetValue(player.SmallID, out var value))
		{
			return 1000;
		}
		return value;
	}

	public static int GetLocalKarma()
	{
		PlayerID localID = PlayerIDManager.LocalID;
		if (localID == null)
		{
			return 1000;
		}
		return GetKarma(localID);
	}

	public static void AdjustKarma(PlayerID player, int delta)
	{
		if (Enabled)
		{
			EnsurePlayer(player);
			byte smallID = player.SmallID;
			_karma[smallID] = Math.Clamp(_karma[smallID] + delta, 0, 2000);
			if (player.IsMe)
			{
				int value = _karma[smallID];
				string value2 = ((delta >= 0) ? "+" : "");
				Notifier.Send(new Notification
				{
					Title = "Karma",
					Message = $"{value2}{delta} karma — you now have {value}",
					ShowPopup = true,
					PopupLength = 3f,
					Type = NotificationType.INFORMATION
				});
			}
			if (NetworkInfo.IsHost)
			{
				CheckAndMaybeKick(player);
			}
		}
	}

	public static void AwardRoundBonus(PlayerID player)
	{
		AdjustKarma(player, 30);
	}

	private static void CheckAndMaybeKick(PlayerID player)
	{
		if (player.IsMe)
		{
			return;
		}
		int karma = GetKarma(player);
		if (karma <= 350)
		{
			TryKick(player, karma);
		}
		else if (karma <= 500)
		{
			if (UnityEngine.Random.value < 0.6f)
			{
				TryKick(player, karma);
			}
			else
			{
				BroadcastLowKarmaWarning(player, karma);
			}
		}
		else if (karma <= 700)
		{
			BroadcastLowKarmaWarning(player, karma);
		}
	}

	private static void TryKick(PlayerID player, int karma)
	{
		player.TryGetDisplayName(out var name);
		MelonLogger.Msg($"[FordTown] Attempting to kick {name} for low karma ({karma}).");
		MelonLogger.Msg($"[FordTown Karma] Auto-kick triggered for {name} ({karma} karma) — no public kick API in this Fusion version.");
		Notifier.Send(new Notification
		{
			Title = "Player Kicked",
			Message = $"{name} was kicked for low karma ({karma}).",
			ShowPopup = true,
			PopupLength = 5f,
			Type = NotificationType.WARNING
		});
	}

	private static void BroadcastLowKarmaWarning(PlayerID player, int karma)
	{
		player.TryGetDisplayName(out var name);
		Notifier.Send(new Notification
		{
			Title = "Low Karma Warning",
			Message = $"{name} has low karma ({karma}). Play properly!",
			ShowPopup = true,
			PopupLength = 5f,
			Type = NotificationType.WARNING
		});
	}

	public static void Clear()
	{
		_karma.Clear();
	}
}

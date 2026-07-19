using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using LabFusion.Data;
using LabFusion.Entities;
using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
using UnityEngine;
using UnityEngine.UI;

namespace TroubleInFordTown;

public class RoleLabels
{
	private class Label
	{
		public GameObject Root;

		public TextMeshPro Text;
	}

	private readonly Dictionary<ushort, Label> _labels = new Dictionary<ushort, Label>();

	private static TMP_FontAsset _font;

	private readonly HashSet<ushort> _wanted = new HashSet<ushort>();

	private readonly List<ushort> _toRemove = new List<ushort>();

	private float _rebuildTimer;

	private const float RebuildInterval = 0.08f;

	private static readonly Color SpectatorGray = new Color(0.67f, 0.67f, 0.67f);

	private static TMP_FontAsset GetFont()
	{
		if ((Object)(object)_font != (Object)null)
		{
			return _font;
		}
		Il2CppArrayBase<TMP_FontAsset> val = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
		foreach (TMP_FontAsset item in val)
		{
			if (((Object)item).name.ToLower().Contains("arlon-medium"))
			{
				_font = item;
				break;
			}
		}
		if ((Object)(object)_font == (Object)null && val.Length > 0)
		{
			_font = val[0];
		}
		return _font;
	}

	public void Update(TroubleInFordTownGamemode gm)
	{
		if (gm == null || !gm.IsStarted || !RigData.HasPlayer)
		{
			ClearAll();
			return;
		}
		Transform headset = RigData.Refs.Headset;
		if ((Object)(object)headset == (Object)null)
		{
			ClearAll();
			return;
		}
		_rebuildTimer -= Time.deltaTime;
		if (_rebuildTimer > 0f)
		{
			return;
		}
		_rebuildTimer = 0.08f;
		Team localTeam = gm.TeamManager.GetLocalTeam();
		bool isSpectator = localTeam != null && localTeam == gm.SpectatorTeam;
		HashSet<ushort> wanted = _wanted;
		wanted.Clear();
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (playerID == null || playerID.IsMe)
			{
				continue;
			}
			Team playerTeam = gm.TeamManager.GetPlayerTeam(playerID);
			if (playerTeam == null)
			{
				continue;
			}
			// Spectators see SPECTATOR labels on other spectators
			if (isSpectator && playerTeam == gm.SpectatorTeam)
			{
				Transform head = GetHead(playerID);
				if (!((Object)(object)head == (Object)null))
				{
					wanted.Add(playerID.SmallID);
					Label label = EnsureLabel(playerID.SmallID);
					((TMP_Text)label.Text).text = "SPECTATOR";
					((Graphic)label.Text).color = SpectatorGray;
					Vector3 val = head.position + Vector3.up * 0.5f;
					Vector3 val2 = val - headset.position;
					if (val2.sqrMagnitude < 0.0001f)
					{
						val2 = Vector3.forward;
					}
					label.Root.transform.position = val;
					label.Root.transform.rotation = Quaternion.LookRotation(val2);
					if (!label.Root.activeSelf)
					{
						label.Root.SetActive(true);
					}
				}
			}
			// Future Feature 10: Murderer red outline visible to spectators
			// if (isSpectator && playerTeam == gm.MurdererTeam) { ... show red outline ... }
		}
		if (_labels.Count <= 0)
		{
			return;
		}
		_toRemove.Clear();
		foreach (KeyValuePair<ushort, Label> label2 in _labels)
		{
			if (!wanted.Contains(label2.Key))
			{
				_toRemove.Add(label2.Key);
			}
		}
		foreach (ushort item in _toRemove)
		{
			if ((Object)(object)_labels[item].Root != (Object)null)
			{
				Object.Destroy((Object)(object)_labels[item].Root);
			}
			_labels.Remove(item);
		}
	}

	private static Transform GetHead(PlayerID pid)
	{
		try
		{
			if (NetworkPlayerManager.TryGetPlayer(pid.SmallID, out var player))
			{
				return player.RigRefs?.Head;
			}
		}
		catch
		{
		}
		return null;
	}

	private Label EnsureLabel(ushort smallId)
	{
		if (_labels.TryGetValue(smallId, out var value) && (Object)(object)value.Root != (Object)null)
		{
			return value;
		}
		GameObject val = new GameObject($"MurderLab Role Label {smallId}");
		Object.DontDestroyOnLoad((Object)(object)val);
		((Object)val).hideFlags = (HideFlags)32;
		TextMeshPro val2 = val.AddComponent<TextMeshPro>();
		TMP_FontAsset font = GetFont();
		if ((Object)(object)font != (Object)null)
		{
			((TMP_Text)val2).font = font;
		}
		((TMP_Text)val2).fontSize = 1.4f;
		((TMP_Text)val2).fontStyle = (FontStyles)1;
		((TMP_Text)val2).alignment = (TextAlignmentOptions)514;
		((TMP_Text)val2).enableWordWrapping = false;
		((TMP_Text)val2).rectTransform.sizeDelta = new Vector2(2.5f, 0.7f);
		Label label = new Label
		{
			Root = val,
			Text = val2
		};
		_labels[smallId] = label;
		return label;
	}

	public void ClearAll()
	{
		foreach (KeyValuePair<ushort, Label> label in _labels)
		{
			if ((Object)(object)label.Value.Root != (Object)null)
			{
				Object.Destroy((Object)(object)label.Value.Root);
			}
		}
		_labels.Clear();
	}
}

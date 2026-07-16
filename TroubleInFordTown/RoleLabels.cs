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

	private static readonly Color DetectiveBlue = new Color(0.2f, 0.6f, 1f);

	private static readonly Color TraitorRed = new Color(1f, 0.2f, 0.2f);

	private static readonly Color JesterPink = new Color(1f, 0.41f, 0.71f);

	private static readonly Color InnocentGreen = new Color(0.2f, 1f, 0.2f);

	private static readonly Color PsychopathRed = new Color(0.8f, 0f, 0f);

	private static readonly Color LoneWolfOrange = new Color(1f, 0.55f, 0f);

	private static readonly Color GlitchCyan = new Color(0f, 1f, 1f);

	private static readonly Color ZombieLime = new Color(0.37f, 0.8f, 0.1f);

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
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
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
		bool flag = localTeam == gm.TraitorTeam;
		bool flag2 = localTeam == gm.SpectatorTeam;
		bool flag3 = localTeam == gm.ZombieTeam;
		HashSet<ushort> wanted = _wanted;
		wanted.Clear();
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (playerID == null || playerID.IsMe)
			{
				continue;
			}
			Team playerTeam = gm.TeamManager.GetPlayerTeam(playerID);
			if (playerTeam == null || playerTeam == gm.SpectatorTeam)
			{
				continue;
			}
			string text;
			Color color;
			if (flag2)
			{
				if (playerTeam == gm.TraitorTeam)
				{
					text = "TRAITOR";
					color = TraitorRed;
				}
				else if (playerTeam == gm.DetectiveTeam)
				{
					text = "DETECTIVE";
					color = DetectiveBlue;
				}
				else if (playerTeam == gm.JesterTeam)
				{
					text = "JESTER";
					color = JesterPink;
				}
				else if (playerTeam == gm.PsychopathTeam)
				{
					text = "PSYCHOPATH";
					color = PsychopathRed;
				}
				else if (playerTeam == gm.LoneWolfTeam)
				{
					text = "LONE WOLF";
					color = LoneWolfOrange;
				}
				else if (playerTeam == gm.GlitchTeam)
				{
					text = "GLITCH";
					color = GlitchCyan;
				}
				else if (playerTeam == gm.ZombieTeam)
				{
					text = "ZOMBIE";
					color = ZombieLime;
				}
				else
				{
					text = "INNOCENT";
					color = InnocentGreen;
				}
			}
			else if (flag3 && playerTeam == gm.ZombieTeam)
			{
				text = "ZOMBIE";
				color = ZombieLime;
			}
			else if (playerTeam == gm.DetectiveTeam)
			{
				text = "DETECTIVE";
				color = DetectiveBlue;
			}
			else if (flag && playerTeam == gm.JesterTeam)
			{
				text = "JESTER";
				color = JesterPink;
			}
			else
			{
				if (!flag || (playerTeam != gm.TraitorTeam && playerTeam != gm.GlitchTeam))
				{
					continue;
				}
				text = "TRAITOR";
				color = TraitorRed;
			}
			Transform head = GetHead(playerID);
			if (!((Object)(object)head == (Object)null))
			{
				wanted.Add(playerID.SmallID);
				Label label = EnsureLabel(playerID.SmallID);
				((TMP_Text)label.Text).text = text;
				((Graphic)label.Text).color = color;
				Vector3 val = head.position + Vector3.up * 0.5f;
				Vector3 val2 = val - headset.position;
				if (((Vector3)(ref val2)).sqrMagnitude < 0.0001f)
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
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		if (_labels.TryGetValue(smallId, out var value) && (Object)(object)value.Root != (Object)null)
		{
			return value;
		}
		GameObject val = new GameObject($"TIFT Role Label {smallId}");
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

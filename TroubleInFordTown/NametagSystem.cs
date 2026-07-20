using System;
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

public class NametagSystem
{
	private struct NametagData
	{
		public string RpName;
		public Color Color;
	}

	private static readonly string[] RPNames = new string[]
	{
		"Frank", "Maria", "Mr. Johnson", "Alice", "Bob", "Carol", "Dave",
		"Eve", "Jimmy", "Laura", "Mike", "Nina", "Oscar", "Patty",
		"Quinn", "Ray", "Sarah", "Tom", "Uma", "Victor", "Wendy",
		"Xander", "Yvonne", "Zack", "Dr. Green", "Sgt. Baker", "Prof. Lee",
		"Mia", "Leo", "Zoe", "Jack", "Rose", "Max", "Ruby"
	};

	private class Label
	{
		public GameObject Root;
		public TextMeshPro Text;
	}

	private readonly Dictionary<ushort, NametagData> _nametags = new Dictionary<ushort, NametagData>();
	private readonly Dictionary<ushort, Label> _labels = new Dictionary<ushort, Label>();

	private static TMP_FontAsset _font;

	private float _updateTimer;
	private const float UpdateInterval = 0.08f;

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

	private static Color GenerateRandomColor()
	{
		float h = UnityEngine.Random.Range(0f, 1f);
		float s = UnityEngine.Random.Range(0.5f, 0.8f);
		float v = UnityEngine.Random.Range(0.6f, 0.9f);
		return Color.HSVToRGB(h, s, v);
	}

	/// <summary>
	/// Assigns random RP names and colors to all current players.
	/// Deterministic across clients: sorted by SmallID, shuffled name list.
	/// </summary>
	public void OnRoundStart()
	{
		_nametags.Clear();

		List<PlayerID> players = new List<PlayerID>(PlayerIDManager.PlayerIDs);
		if (players.Count == 0)
		{
			return;
		}

		// Sort by SmallID for consistent assignment across clients
		players.Sort((a, b) => a.SmallID.CompareTo(b.SmallID));

		// Fisher-Yates shuffle the name list
		List<string> availableNames = new List<string>(RPNames);
		System.Random rng = new System.Random();
		for (int i = availableNames.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			string tmp = availableNames[i];
			availableNames[i] = availableNames[j];
			availableNames[j] = tmp;
		}

		int nameIndex = 0;
		foreach (PlayerID player in players)
		{
			if (player == null)
			{
				continue;
			}
			string name = availableNames[nameIndex % availableNames.Count];
			nameIndex++;
			Color color = GenerateRandomColor();
			_nametags[player.SmallID] = new NametagData
			{
				RpName = name,
				Color = color
			};
		}
	}

	/// <summary>
	/// Clears all nametag data and destroys label GameObjects.
	/// </summary>
	public void OnRoundEnd()
	{
		_nametags.Clear();
		ClearAllLabels();
	}

	/// <summary>
	/// Returns the RP name assigned to the given player.
	/// </summary>
	public string GetName(ushort smallId)
	{
		if (_nametags.TryGetValue(smallId, out var data))
		{
			return data.RpName;
		}
		return "???";
	}

	/// <summary>
	/// Returns the color assigned to the given player.
	/// </summary>
	public Color GetColor(ushort smallId)
	{
		if (_nametags.TryGetValue(smallId, out var data))
		{
			return data.Color;
		}
		return Color.white;
	}

	/// <summary>
	/// Returns the local player's assigned RP name.
	/// </summary>
	public string GetLocalRPName()
	{
		PlayerID local = PlayerIDManager.LocalID;
		if (local != null && _nametags.TryGetValue(local.SmallID, out var data))
		{
			return data.RpName;
		}
		return "Player";
	}

	/// <summary>
	/// Updates nametag billboards each frame.
	/// Shows nametags within ~2.7m range, or at all distances for spectators.
	/// </summary>
	public void Update(TroubleInFordTownGamemode gm)
	{
		if (gm == null || !gm.IsStarted || !RigData.HasPlayer)
		{
			ClearAllLabels();
			return;
		}
		Transform headset = RigData.Refs.Headset;
		if ((Object)(object)headset == (Object)null)
		{
			ClearAllLabels();
			return;
		}
		_updateTimer -= Time.deltaTime;
		if (_updateTimer > 0f)
		{
			return;
		}
		_updateTimer = UpdateInterval;

		Team localTeam = gm.TeamManager.GetLocalTeam();
		bool isSpectator = localTeam != null && localTeam == gm.SpectatorTeam;
		Vector3 headPos = headset.position;
		float maxDistSqr = 2.7f * 2.7f;

		HashSet<ushort> wanted = new HashSet<ushort>();

		foreach (KeyValuePair<ushort, NametagData> kvp in _nametags)
		{
			ushort smallId = kvp.Key;
			NametagData data = kvp.Value;

			Transform head = GetHead(smallId);
			if ((Object)(object)head == (Object)null)
			{
				continue;
			}
			Vector3 labelPos = head.position + Vector3.up * 0.5f;
			bool show = isSpectator || (labelPos - headPos).sqrMagnitude < maxDistSqr;

			if (show)
			{
				wanted.Add(smallId);
				Label label = EnsureLabel(smallId);
				((TMP_Text)label.Text).text = data.RpName;
				((Graphic)label.Text).color = data.Color;
				label.Root.transform.position = labelPos;
				Vector3 dir = labelPos - headPos;
				if (dir.sqrMagnitude < 0.0001f)
				{
					dir = Vector3.forward;
				}
				label.Root.transform.rotation = Quaternion.LookRotation(dir);
				if (!label.Root.activeSelf)
				{
					label.Root.SetActive(true);
				}
			}
		}

		// Hide labels for players not in wanted range
		foreach (KeyValuePair<ushort, Label> kvp in _labels)
		{
			if (!wanted.Contains(kvp.Key) && kvp.Value.Root.activeSelf)
			{
				kvp.Value.Root.SetActive(false);
			}
		}
	}

	private static Transform GetHead(ushort smallId)
	{
		try
		{
			if (NetworkPlayerManager.TryGetPlayer((byte)smallId, out var player))
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
		GameObject val = new GameObject($"MurderLab Nametag {smallId}");
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

	public void ClearAllLabels()
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

using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using LabFusion.Data;
using UnityEngine;

namespace TroubleInFordTown;

public class CorpseInspectorUI
{
	private GameObject _root;

	private TextMeshPro _text;

	private CorpseProxy _showing;

	private static TMP_FontAsset _font;

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

	private bool TryCreate()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_root != (Object)null)
		{
			return true;
		}
		if (!RigData.HasPlayer)
		{
			return false;
		}
		_root = new GameObject("TIFT Corpse Inspector");
		Object.DontDestroyOnLoad((Object)(object)_root);
		((Object)_root).hideFlags = (HideFlags)32;
		_root.SetActive(false);
		_text = _root.AddComponent<TextMeshPro>();
		((TMP_Text)_text).fontSize = 0.55f;
		((TMP_Text)_text).alignment = (TextAlignmentOptions)514;
		((TMP_Text)_text).rectTransform.sizeDelta = new Vector2(1.4f, 0.6f);
		TMP_FontAsset font = GetFont();
		if ((Object)(object)font != (Object)null)
		{
			((TMP_Text)_text).font = font;
		}
		return true;
	}

	public void ShowFor(CorpseProxy corpse)
	{
		_showing = corpse;
	}

	public void Hide()
	{
		_showing = null;
		if ((Object)(object)_root != (Object)null)
		{
			_root.SetActive(false);
		}
	}

	public void Update()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		if (!TryCreate())
		{
			return;
		}
		if (_showing == null || !_showing.IsValid)
		{
			_root.SetActive(false);
			return;
		}
		Transform headset = RigData.Refs.Headset;
		if ((Object)(object)headset == (Object)null)
		{
			_root.SetActive(false);
			return;
		}
		Vector3 val = _showing.Position + Vector3.up * 0.7f;
		Vector3 val2 = headset.position - val;
		if (val2.sqrMagnitude < 0.001f)
		{
			val2 = Vector3.forward;
		}
		_root.transform.position = val;
		_root.transform.rotation = Quaternion.LookRotation(-val2.normalized);
		float num = Time.realtimeSinceStartup - _showing.TimeOfDeath;
		string text = ((num < 60f) ? $"{Mathf.FloorToInt(num)}s ago" : $"{Mathf.FloorToInt(num / 60f)}m {Mathf.FloorToInt(num % 60f)}s ago");
		string text2 = (string.IsNullOrEmpty(_showing.WeaponName) ? "<size=85%><color=#aaaaaa>Weapon: Unknown</color></size>" : ("<size=85%>Weapon: <b>" + _showing.WeaponName + "</b></size>"));
		((TMP_Text)_text).text = $"<b>{_showing.PlayerName}</b>\n<color={_showing.RoleColor}>{_showing.RoleName}</color>\n" + text2 + "\n<size=80%><color=#cccccc>" + text + "</color></size>";
		if (!_root.activeSelf)
		{
			_root.SetActive(true);
		}
	}

	public void DestroyUI()
	{
		if ((Object)(object)_root != (Object)null)
		{
			Object.Destroy((Object)(object)_root);
		}
		_root = null;
		_text = null;
		_showing = null;
	}
}

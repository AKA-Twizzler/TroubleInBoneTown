using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Marrow;
using Il2CppTMPro;
using LabFusion.Data;
using UnityEngine;

namespace TroubleInFordTown;

public class WristIndicator
{
	private GameObject _root;

	private TextMeshPro _text;

	private static TMP_FontAsset _font;

	public bool IsCreated => (Object)(object)_root != (Object)null;

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

	public bool TryCreate()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		if (IsCreated)
		{
			return true;
		}
		if (!RigData.HasPlayer || (Object)(object)RigData.Refs.LeftHand == (Object)null)
		{
			return false;
		}
		_root = new GameObject("TIFT Wrist Indicator");
		Object.DontDestroyOnLoad((Object)(object)_root);
		((Object)_root).hideFlags = (HideFlags)32;
		_text = _root.AddComponent<TextMeshPro>();
		TMP_FontAsset font = GetFont();
		if ((Object)(object)font != (Object)null)
		{
			((TMP_Text)_text).font = font;
		}
		((TMP_Text)_text).fontSize = 0.21f;
		((TMP_Text)_text).alignment = (TextAlignmentOptions)514;
		((TMP_Text)_text).rectTransform.sizeDelta = new Vector2(0.18f, 0.08f);
		((TMP_Text)_text).text = "";
		return true;
	}

	public void SetText(string value)
	{
		if ((Object)(object)_text != (Object)null)
		{
			((TMP_Text)_text).text = value;
		}
	}

	public void Update()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if (!IsCreated || !RigData.HasPlayer)
		{
			return;
		}
		Hand leftHand = RigData.Refs.LeftHand;
		Transform headset = RigData.Refs.Headset;
		if (!((Object)(object)leftHand == (Object)null) && !((Object)(object)headset == (Object)null))
		{
			Vector3 val = ((Component)leftHand).transform.position + Vector3.up * 0.12f;
			Vector3 val2 = val - headset.position;
			float magnitude = val2.magnitude;
			bool flag = false;
			try
			{
				flag = (Object)(object)leftHand.m_CurrentAttachedGO != (Object)null;
			}
			catch
			{
			}
			bool flag2 = !flag && magnitude < 1.2f && magnitude > 0.05f && Vector3.Dot(headset.forward, val2 / magnitude) > 0.55f;
			if (_root.activeSelf != flag2)
			{
				_root.SetActive(flag2);
			}
			if (flag2)
			{
				_root.transform.position = val;
				_root.transform.rotation = Quaternion.LookRotation(val2);
			}
		}
	}

	public void Destroy()
	{
		if ((Object)(object)_root != (Object)null)
		{
			Object.Destroy((Object)(object)_root);
		}
		_root = null;
		_text = null;
	}
}

using System;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Data;
using UnityEngine;

namespace TroubleInFordTown;

public class HypnotistController
{
	public const float BrainwashSeconds = 9.5f;

	public Action<ushort> OnBrainwashComplete;

	private bool _charging;

	private ushort _targetId;

	private float _charge;

	private float _graceTimer;

	private GameObject _beamObj;

	private LineRenderer _beam;

	private AudioSource _audio;

	public static AudioClip ChargeClip;

	public void Update(string gunBarcode)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (!RigData.HasPlayer)
		{
			StopCharge();
			return;
		}
		Transform val = FindHeldGun(gunBarcode);
		if ((Object)(object)val == (Object)null)
		{
			StopCharge();
			return;
		}
		Vector3 position = val.position;
		Vector3 forward = val.forward;
		if (CorpseManager.TryGetInnocentCorpseAlongRay(position, forward, 30f, out var ownerSmallId, out var corpsePos) && ownerSmallId != 0)
		{
			if (_charging && _targetId == ownerSmallId)
			{
				_charge += Time.deltaTime;
			}
			else
			{
				_charging = true;
				_targetId = ownerSmallId;
				_charge = 0f;
				PlayChargeOnce(corpsePos);
			}
			_graceTimer = 0.6f;
			ShowBeam(position, corpsePos);
			if (_charge >= 9.5f)
			{
				ushort targetId = _targetId;
				StopCharge();
				OnBrainwashComplete?.Invoke(targetId);
			}
		}
		else if (_charging && _graceTimer > 0f)
		{
			_graceTimer -= Time.deltaTime;
			if ((Object)(object)_beamObj != (Object)null && _beamObj.activeSelf)
			{
				_beamObj.SetActive(false);
			}
		}
		else
		{
			StopCharge();
		}
	}

	public void Cleanup()
	{
		StopCharge();
		if ((Object)(object)_beamObj != (Object)null)
		{
			Object.Destroy((Object)(object)_beamObj);
			_beamObj = null;
			_beam = null;
		}
		if ((Object)(object)_audio != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)_audio).gameObject);
			_audio = null;
		}
	}

	private static Transform FindHeldGun(string gunBarcode)
	{
		try
		{
			PhysicsRig physicsRig = RigData.Refs.RigManager.physicsRig;
			Hand[] array = (Hand[])(object)new Hand[2] { physicsRig.rightHand, physicsRig.leftHand };
			foreach (Hand obj in array)
			{
				GameObject val = ((obj != null) ? obj.m_CurrentAttachedGO : null);
				if ((Object)(object)val == (Object)null)
				{
					continue;
				}
				Gun componentInChildren = ((Component)val.transform.root).GetComponentInChildren<Gun>(true);
				Transform result = (((Object)(object)componentInChildren != (Object)null && (Object)(object)componentInChildren.firePointTransform != (Object)null) ? componentInChildren.firePointTransform : (((Object)(object)componentInChildren != (Object)null) ? ((Component)componentInChildren).transform : val.transform));
				bool flag = false;
				if (!string.IsNullOrEmpty(gunBarcode))
				{
					Poolee val2 = Poolee.Cache.Get(val);
					if ((Object)(object)val2 == (Object)null)
					{
						val2 = val.GetComponentInParent<Poolee>();
					}
					object obj2;
					if (val2 == null)
					{
						obj2 = null;
					}
					else
					{
						SpawnableCrate spawnableCrate = val2.SpawnableCrate;
						if (spawnableCrate == null)
						{
							obj2 = null;
						}
						else
						{
							Barcode barcode = ((Scannable)spawnableCrate).Barcode;
							obj2 = ((barcode != null) ? barcode.ID : null);
						}
					}
					if ((string?)obj2 == gunBarcode)
					{
						flag = true;
					}
				}
				if (flag || (Object)(object)componentInChildren != (Object)null)
				{
					return result;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private void ShowBeam(Vector3 from, Vector3 to)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		if ((Object)(object)_beamObj == (Object)null)
		{
			_beamObj = new GameObject("TIFT Brainwash Beam");
			Object.DontDestroyOnLoad((Object)(object)_beamObj);
			_beam = _beamObj.AddComponent<LineRenderer>();
			((Renderer)_beam).material = new Material(Shader.Find("Sprites/Default"));
			_beam.widthMultiplier = 0.03f;
			_beam.numCapVertices = 4;
		}
		float num = Mathf.Clamp01(_charge / 9.5f);
		Color val = Color.Lerp(new Color(0.6f, 0f, 1f), new Color(1f, 0.2f, 0.6f), num);
		_beam.startColor = val;
		_beam.endColor = val;
		_beam.SetPosition(0, from);
		_beam.SetPosition(1, to);
		if (!_beamObj.activeSelf)
		{
			_beamObj.SetActive(true);
		}
	}

	private void PlayChargeOnce(Vector3 position)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		if (!((Object)(object)ChargeClip == (Object)null))
		{
			if ((Object)(object)_audio == (Object)null)
			{
				GameObject val = new GameObject("TIFT Brainwash Audio");
				Object.DontDestroyOnLoad((Object)(object)val);
				_audio = val.AddComponent<AudioSource>();
				_audio.loop = false;
				_audio.spatialBlend = 1f;
				_audio.minDistance = 2f;
				_audio.maxDistance = 25f;
				_audio.clip = ChargeClip;
				_audio.volume = 1f;
			}
			((Component)_audio).transform.position = position;
			_audio.Stop();
			_audio.Play();
		}
	}

	private void StopCharge()
	{
		_charging = false;
		_charge = 0f;
		_targetId = 0;
		if ((Object)(object)_beamObj != (Object)null && _beamObj.activeSelf)
		{
			_beamObj.SetActive(false);
		}
		if ((Object)(object)_audio != (Object)null && _audio.isPlaying)
		{
			_audio.Stop();
		}
	}
}

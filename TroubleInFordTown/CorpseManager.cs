using System;
using System.Collections.Generic;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSystem;
using LabFusion.Data;
using LabFusion.Player;
using LabFusion.Representation;
using UnityEngine;

namespace TroubleInFordTown;

public static class CorpseManager
{
	private static readonly List<CorpseProxy> _corpses = new List<CorpseProxy>();

	private static readonly CorpseInspectorUI _inspectorUI = new CorpseInspectorUI();

	private static readonly Dictionary<ushort, float> _recentlyRemoved = new Dictionary<ushort, float>();

	private const float GrabRadius = 1.2f;

	private const float SuppressSeconds = 2f;

	public static void RegisterDeath(string playerName, string roleName, string roleColor, Vector3 deathPosition, string weaponName = null, string avatarBarcode = null, ushort ownerSmallId = 0, bool wasInnocentSide = false)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		SpawnRagdollCorpse(deathPosition, avatarBarcode, delegate(RigManager rig)
		{
			if (ownerSmallId != 0 && _recentlyRemoved.TryGetValue(ownerSmallId, out var value) && Time.realtimeSinceStartup - value < 2f)
			{
				try
				{
					if ((Object)(object)rig != (Object)null && (Object)(object)((Component)rig).gameObject != (Object)null)
					{
						Object.Destroy((Object)(object)((Component)rig).gameObject);
					}
					return;
				}
				catch
				{
					return;
				}
			}
			_corpses.Add(new CorpseProxy
			{
				Rig = rig,
				PlayerName = playerName,
				RoleName = roleName,
				RoleColor = roleColor,
				WeaponName = weaponName,
				TimeOfDeath = Time.realtimeSinceStartup,
				OwnerSmallID = ownerSmallId,
				WasInnocentSide = wasInnocentSide
			});
		});
	}

	public static bool TryGetInnocentCorpseAlongRay(Vector3 origin, Vector3 direction, float maxDistance, out ushort ownerSmallId, out Vector3 corpsePos)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		ownerSmallId = 0;
		corpsePos = Vector3.zero;
		direction = ((Vector3)(ref direction)).normalized;
		float num = float.MaxValue;
		CorpseProxy corpseProxy = null;
		foreach (CorpseProxy corpse in _corpses)
		{
			if (corpse == null || !corpse.IsValid || !corpse.WasInnocentSide)
			{
				continue;
			}
			Vector3 val = corpse.Position - origin;
			float num2 = Vector3.Dot(val, direction);
			if (!(num2 < 0f) && !(num2 > maxDistance))
			{
				Vector3 val2 = val - direction * num2;
				float magnitude = ((Vector3)(ref val2)).magnitude;
				if (!(magnitude > 1f) && magnitude < num)
				{
					num = magnitude;
					corpseProxy = corpse;
				}
			}
		}
		if (corpseProxy != null)
		{
			ownerSmallId = corpseProxy.OwnerSmallID;
			corpsePos = corpseProxy.Position;
			return true;
		}
		return false;
	}

	public static void RemoveCorpseOf(ushort ownerSmallId)
	{
		for (int num = _corpses.Count - 1; num >= 0; num--)
		{
			if (_corpses[num] != null && _corpses[num].OwnerSmallID == ownerSmallId)
			{
				_corpses[num].Destroy();
				_corpses.RemoveAt(num);
			}
		}
		if (ownerSmallId != 0)
		{
			_recentlyRemoved[ownerSmallId] = Time.realtimeSinceStartup;
		}
	}

	private static void SpawnRagdollCorpse(Vector3 position, string avatarBarcode, Action<RigManager> onSpawned)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		PlayerRepUtilities.CreateNewRig(delegate(RigManager rig)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Expected O, but got Unknown
			try
			{
				((Component)rig).transform.position = position;
				try
				{
					foreach (InventorySlotReceiver componentsInChild in ((Component)rig).GetComponentsInChildren<InventorySlotReceiver>(true))
					{
						((Behaviour)componentsInChild).enabled = false;
					}
				}
				catch
				{
				}
				if (!string.IsNullOrEmpty(avatarBarcode))
				{
					try
					{
						rig.SwapAvatarCrate(new Barcode(avatarBarcode), false, (Action<bool>)null);
					}
					catch
					{
					}
				}
				try
				{
					ControllerRig controllerRig = rig.ControllerRig;
					BaseController[] array = (BaseController[])(object)new BaseController[2]
					{
						(controllerRig != null) ? controllerRig.leftController : null,
						(controllerRig != null) ? controllerRig.rightController : null
					};
					foreach (BaseController obj3 in array)
					{
						Haptor val = ((obj3 != null) ? obj3.haptor : null);
						if (!((Object)(object)val == (Object)null))
						{
							val.hapticsAllowed = false;
							val.device_Controller = null;
							((Behaviour)val).enabled = false;
						}
					}
					foreach (Haptor componentsInChild2 in ((Component)rig).GetComponentsInChildren<Haptor>(true))
					{
						if (!((Object)(object)componentsInChild2 == (Object)null))
						{
							componentsInChild2.hapticsAllowed = false;
							componentsInChild2.device_Controller = null;
							((Behaviour)componentsInChild2).enabled = false;
						}
					}
				}
				catch
				{
				}
				try
				{
					rig.physicsRig.ShutdownRig();
					rig.physicsRig.RagdollRig();
				}
				catch
				{
				}
				try
				{
					ControllerRig controllerRig2 = rig.ControllerRig;
					BaseController[] array = (BaseController[])(object)new BaseController[2]
					{
						(controllerRig2 != null) ? controllerRig2.leftController : null,
						(controllerRig2 != null) ? controllerRig2.rightController : null
					};
					foreach (BaseController val2 in array)
					{
						if (!((Object)(object)val2 == (Object)null))
						{
							try
							{
								((Behaviour)val2).enabled = false;
							}
							catch
							{
							}
							try
							{
								((Component)val2).gameObject.SetActive(false);
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
				try
				{
					Hand[] array2 = (Hand[])(object)new Hand[2]
					{
						rig.physicsRig.leftHand,
						rig.physicsRig.rightHand
					};
					foreach (Hand val3 in array2)
					{
						if (!((Object)(object)val3 == (Object)null))
						{
							try
							{
								HandPoseAnimator animator = val3.Animator;
								if ((Object)(object)animator != (Object)null)
								{
									((Behaviour)animator).enabled = false;
								}
							}
							catch
							{
							}
							try
							{
								((Behaviour)val3).enabled = false;
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
				((Object)((Component)rig).gameObject).name = "[TIFT Corpse]";
				onSpawned?.Invoke(rig);
			}
			catch
			{
			}
		});
	}

	public static void Update()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		if (!RigData.HasPlayer)
		{
			return;
		}
		Hand leftHand = RigData.Refs.LeftHand;
		Transform val = ((leftHand != null) ? ((Component)leftHand).transform : null);
		Hand rightHand = RigData.Refs.RightHand;
		Transform val2 = ((rightHand != null) ? ((Component)rightHand).transform : null);
		Transform headset = RigData.Refs.Headset;
		Vector3 val3 = (((Object)(object)headset != (Object)null) ? headset.position : Vector3.zero);
		Vector3 val4 = (((Object)(object)val != (Object)null) ? val.position : val3);
		Vector3 val5 = (((Object)(object)val2 != (Object)null) ? val2.position : val3);
		CorpseProxy corpseProxy = null;
		float num = 1.2f;
		foreach (CorpseProxy corpse in _corpses)
		{
			if (corpse.IsValid)
			{
				Vector3 position = corpse.Position;
				float num2 = Mathf.Min(Vector3.Distance(val4, position), Vector3.Distance(val5, position));
				if (num2 < num)
				{
					num = num2;
					corpseProxy = corpse;
				}
			}
		}
		if (corpseProxy != null)
		{
			_inspectorUI.ShowFor(corpseProxy);
		}
		else
		{
			_inspectorUI.Hide();
		}
		_inspectorUI.Update();
	}

	public static void ClearAll()
	{
		_inspectorUI.Hide();
		try
		{
			LocalPlayer.ReleaseGrips();
		}
		catch
		{
		}
		foreach (CorpseProxy corpse in _corpses)
		{
			corpse.Destroy();
		}
		_corpses.Clear();
	}

	public static void DestroyUI()
	{
		_inspectorUI.DestroyUI();
	}
}

using System;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Marrow.Data;
using LabFusion.Data;
using LabFusion.Marrow.Pool;
using LabFusion.Player;
using LabFusion.RPC;
using LabFusion.UI.Popups;
using UnityEngine;

namespace TroubleInFordTown;

public static class CustomWeapons
{
	public enum WeaponType
	{
		GoldenPistol,
		ShadowKnife
	}

	private struct TrackedWeapon
	{
		public ushort EntityId;

		public WeaponType Type;
	}

	private static TrackedWeapon? _active;

	private const string PistolBarcode = "c1534c5a-fcfc-4f43-8fb0-d29531393131";

	private const string KnifeBarcode = "c1534c5a-1fb8-477c-afbe-2a95436f6d62";

	public const int GoldenPistolCost = 3;

	public const int ShadowKnifeCost = 2;

	public static bool GoldenPistolActive
	{
		get
		{
			if (_active.HasValue)
			{
				return _active.Value.Type == WeaponType.GoldenPistol;
			}
			return false;
		}
	}

	public static void BuyGoldenPistol(TroubleInFordTownGamemode gamemode)
	{
		if (!TraitorPoints.TrySpend(3))
		{
			ShowNotEnoughTP(3);
		}
		else
		{
			SpawnCustomWeapon("c1534c5a-fcfc-4f43-8fb0-d29531393131", WeaponType.GoldenPistol, ApplyGoldMaterial, gamemode);
		}
	}

	public static void BuyShadowKnife(TroubleInFordTownGamemode gamemode)
	{
		if (!TraitorPoints.TrySpend(2))
		{
			ShowNotEnoughTP(2);
		}
		else
		{
			SpawnCustomWeapon("c1534c5a-1fb8-477c-afbe-2a95436f6d62", WeaponType.ShadowKnife, ApplyShadowMaterial, gamemode);
		}
	}

	private static void SpawnCustomWeapon(string barcode, WeaponType type, Action<GameObject> materializer, TroubleInFordTownGamemode gamemode)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (!RigData.HasPlayer)
		{
			return;
		}
		Spawnable spawnable = LocalAssetSpawner.CreateSpawnable(barcode);
		Transform headset = RigData.Refs.Headset;
		Vector3 position = headset.position + headset.forward * 0.5f;
		NetworkAssetSpawner.Spawn(new NetworkAssetSpawner.SpawnRequestInfo
		{
			Spawnable = spawnable,
			Position = position,
			Rotation = Quaternion.identity,
			SpawnEffect = true,
			SpawnCallback = delegate(NetworkAssetSpawner.SpawnCallbackInfo info)
			{
				_active = new TrackedWeapon
				{
					EntityId = info.Entity.ID,
					Type = type
				};
				try
				{
					materializer(info.Spawned);
				}
				catch
				{
				}
				try
				{
					BoostDamage(info.Spawned);
				}
				catch
				{
				}
				gamemode.TryHolster(info.Spawned);
			}
		});
	}

	private static void BoostDamage(GameObject item)
	{
		if ((Object)(object)item == (Object)null)
		{
			return;
		}
		foreach (MonoBehaviour componentsInChild in item.GetComponentsInChildren<MonoBehaviour>(true))
		{
			if ((Object)(object)componentsInChild == (Object)null)
			{
				continue;
			}
			try
			{
				FieldInfo field = ((object)componentsInChild).GetType().GetField("damage", BindingFlags.Instance | BindingFlags.Public);
				if (field != null && field.FieldType == typeof(float))
				{
					field.SetValue(componentsInChild, 99999f);
				}
			}
			catch
			{
			}
		}
	}

	public static void OnLocalKill()
	{
		if (_active.HasValue)
		{
			TrackedWeapon value = _active.Value;
			_active = null;
			try
			{
				LocalPlayer.ReleaseGrips();
			}
			catch
			{
			}
			try
			{
				NetworkAssetSpawner.Despawn(new NetworkAssetSpawner.DespawnRequestInfo
				{
					EntityID = value.EntityId,
					DespawnEffect = true
				});
			}
			catch
			{
			}
			string text = ((value.Type == WeaponType.GoldenPistol) ? "Golden Pistol" : "Shadow Knife");
			Notifier.Send(new Notification
			{
				Title = text,
				Message = "Your one-use weapon has disappeared.",
				ShowPopup = true,
				PopupLength = 3f,
				Type = NotificationType.INFORMATION
			});
		}
	}

	public static void Reset()
	{
		_active = null;
	}

	private static void ApplyGoldMaterial(GameObject item)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		Color baseColor = default(Color);
		((Color)(ref baseColor))._002Ector(1f, 0.78f, 0.1f);
		Color emissiveColor = default(Color);
		((Color)(ref emissiveColor))._002Ector(0.4f, 0.28f, 0f);
		TintAllRenderers(item, baseColor, emissiveColor);
	}

	private static void ApplyShadowMaterial(GameObject item)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		Color baseColor = default(Color);
		((Color)(ref baseColor))._002Ector(0.06f, 0f, 0.12f);
		Color emissiveColor = default(Color);
		((Color)(ref emissiveColor))._002Ector(0.25f, 0f, 0.45f);
		TintAllRenderers(item, baseColor, emissiveColor);
	}

	private static void TintAllRenderers(GameObject item, Color baseColor, Color emissiveColor)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)item == (Object)null)
		{
			return;
		}
		foreach (MeshRenderer componentsInChild in item.GetComponentsInChildren<MeshRenderer>(true))
		{
			TintRenderer((Renderer)(object)componentsInChild, baseColor, emissiveColor);
		}
		foreach (SkinnedMeshRenderer componentsInChild2 in item.GetComponentsInChildren<SkinnedMeshRenderer>(true))
		{
			TintRenderer((Renderer)(object)componentsInChild2, baseColor, emissiveColor);
		}
	}

	private static void TintRenderer(Renderer rend, Color baseColor, Color emissiveColor)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		Il2CppReferenceArray<Material> materials = rend.materials;
		for (int i = 0; i < ((Il2CppArrayBase<Material>)(object)materials).Length; i++)
		{
			Material val = ((Il2CppArrayBase<Material>)(object)materials)[i];
			if (!((Object)(object)val == (Object)null))
			{
				if (val.HasProperty("_BaseColor"))
				{
					val.SetColor("_BaseColor", baseColor);
				}
				if (val.HasProperty("_Color"))
				{
					val.SetColor("_Color", baseColor);
				}
				if (val.HasProperty("_MainColor"))
				{
					val.SetColor("_MainColor", baseColor);
				}
				if (val.HasProperty("_TintColor"))
				{
					val.SetColor("_TintColor", baseColor);
				}
				if (val.HasProperty("_AlbedoColor"))
				{
					val.SetColor("_AlbedoColor", baseColor);
				}
				if (val.HasProperty("_EmissiveColor"))
				{
					val.SetColor("_EmissiveColor", emissiveColor);
				}
				if (val.HasProperty("_EmissionColor"))
				{
					val.SetColor("_EmissionColor", emissiveColor);
				}
				if (val.HasProperty("_Emission"))
				{
					val.SetColor("_Emission", emissiveColor);
				}
				val.EnableKeyword("_EMISSION");
				val.EnableKeyword("_EMISSIVE_COLOR_MAP");
			}
		}
		rend.materials = materials;
	}

	private static void ShowNotEnoughTP(int cost)
	{
		Notifier.Send(new Notification
		{
			Title = "Not Enough Traitor Points",
			Message = $"You need {cost} TP but only have {TraitorPoints.Points}.",
			ShowPopup = true,
			PopupLength = 3f,
			Type = NotificationType.WARNING
		});
	}
}

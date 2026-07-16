using Il2CppSLZ.Marrow;
using UnityEngine;

namespace TroubleInFordTown;

public class CorpseProxy
{
	public RigManager Rig;

	public string PlayerName;

	public string RoleName;

	public string RoleColor;

	public string WeaponName;

	public float TimeOfDeath;

	public ushort OwnerSmallID;

	public bool WasInnocentSide;

	public bool IsValid
	{
		get
		{
			if ((Object)(object)Rig != (Object)null)
			{
				return (Object)(object)((Component)Rig).gameObject != (Object)null;
			}
			return false;
		}
	}

	public Vector3 Position
	{
		get
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (IsValid)
				{
					PhysicsRig physicsRig = Rig.physicsRig;
					if ((Object)(object)((physicsRig != null) ? ((Rig)physicsRig).m_chest : null) != (Object)null)
					{
						return ((Rig)Rig.physicsRig).m_chest.position;
					}
				}
			}
			catch
			{
			}
			return Vector3.zero;
		}
	}

	public void Destroy()
	{
		try
		{
			if ((Object)(object)Rig != (Object)null && (Object)(object)((Component)Rig).gameObject != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)Rig).gameObject);
			}
		}
		catch
		{
		}
		Rig = null;
	}
}

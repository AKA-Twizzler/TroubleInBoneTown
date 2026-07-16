using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.Combat;
using LabFusion.Utilities;
using UnityEngine;

namespace TroubleInFordTown;

[HarmonyPatch(typeof(PlayerDamageReceiver), "ReceiveAttack")]
public static class ZombieBiteDamagePatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	public static void Prefix(PlayerDamageReceiver __instance, ref Attack attack)
	{
		if (!ZombieState.LocalIsZombie && !OneStabState.LocalHolding)
		{
			return;
		}
		try
		{
			TriggerRefProxy proxy = attack.proxy;
			if (!((Object)(object)proxy == (Object)null) && !((Object)(object)proxy.root == (Object)null))
			{
				RigManager val = RigManager.Cache.Get(proxy.root);
				if ((Object)(object)val != (Object)null && val.IsLocalPlayer())
				{
					attack.damage = 999999f;
				}
			}
		}
		catch
		{
		}
	}
}

using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Combat;

namespace TroubleInFordTown;

/// <summary>
/// Stub: placeholder for one-shot murder damage patch.
/// When implemented, this will make the Murderer's knife a one-hit kill
/// by prefixing PlayerDamageReceiver.ReceiveAttack with high damage.
/// </summary>
[HarmonyPatch(typeof(PlayerDamageReceiver), "ReceiveAttack")]
public static class MurdererDamagePatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	public static void Prefix(PlayerDamageReceiver __instance, ref Attack attack)
	{
		// TODO: Check if attacker is the Murderer, set attack.damage = 999999f
	}
}

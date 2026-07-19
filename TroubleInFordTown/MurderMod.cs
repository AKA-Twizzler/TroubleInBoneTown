using System;
using System.Reflection;
using LabFusion.SDK.Gamemodes;
using MelonLoader;

namespace TroubleInFordTown;

public class MurderMod : MelonMod
{
	private bool _registered;

	private bool _patched;

	public override void OnInitializeMelon()
	{
		ApplyPatches();
		TryRegisterGamemode();
	}

	public override void OnLateInitializeMelon()
	{
		ApplyPatches();
		TryRegisterGamemode();
	}

	private void ApplyPatches()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			((MelonBase)this).HarmonyInstance.CreateClassProcessor(typeof(MurdererDamagePatch)).Patch();
			_patched = true;
		}
		catch (Exception ex)
		{
			((MelonBase)this).LoggerInstance.Warning("Murderer damage patch could not be applied on this build (Murderer may not one-shot, everything else still works): " + ex.Message);
		}
	}

	private void TryRegisterGamemode()
	{
		if (_registered)
		{
			return;
		}
		try
		{
			GamemodeRegistration.LoadGamemodes(Assembly.GetExecutingAssembly());
			_registered = true;
			((MelonBase)this).LoggerInstance.Msg("MurderLab registered! Select it from the Fusion menu's Gamemodes tab.");
		}
		catch (Exception ex)
		{
			((MelonBase)this).LoggerInstance.Warning("Gamemode registration deferred/failed (will retry): " + ex.Message);
		}
	}
}

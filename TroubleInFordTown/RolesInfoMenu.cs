using BoneLib.BoneMenu;
using UnityEngine;

namespace TroubleInFordTown;

public class RolesInfoMenu
{
	private Page _page;

	private Page _settingsPage;

	private FunctionElement _karmaDisplay;

	public void Register()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		_settingsPage = Page.Root.CreatePage("TFT Settings", new Color(0.85f, 0.85f, 0.2f));
		_settingsPage.CreateBool("Mute Win Music (local)", new Color(0.9f, 0.5f, 0.2f), WinMusic.LocallyMuted, delegate(bool v)
		{
			WinMusic.LocallyMuted = v;
		});
		_karmaDisplay = _settingsPage.CreateFunction("Your Karma: —", new Color(0.4f, 0.8f, 1f), RefreshKarma);
		Menu.OnPageOpened += OnPageOpened;
		_page = Page.Root.CreatePage("Roles Info", Color.white, 7);
		AddRole("INNOCENT", new Color(0.2f, 1f, 0.2f), "Find and kill all the", "Traitors to win.");
		AddRole("TRAITOR", new Color(1f, 0.2f, 0.2f), "Secretly eliminate", "everyone else to win.", "Buy gear in the", "Traitor Shop.");
		AddRole("DETECTIVE", new Color(0.2f, 0.6f, 1f), "Publicly known to all.", "Leads the Innocents in", "hunting the Traitors.");
		AddRole("JESTER", new Color(1f, 0.41f, 0.71f), "Trick an Innocent into", "killing you to become", "the Psychopath. If a", "Traitor kills you, you lose.");
		AddRole("PSYCHOPATH", new Color(0.8f, 0f, 0f), "A second chance for the", "Jester after being killed", "by an Innocent. Kill", "everyone to win.");
		AddRole("LONE WOLF", new Color(1f, 0.55f, 0f), "A lone killer. Be the", "last one standing", "to win.");
		AddRole("GLITCH", new Color(0f, 1f, 1f), "Appears as a Traitor to", "the Traitors, but is", "secretly Innocent and", "wins with the Innocents.");
		AddRole("HYPNOTIST", new Color(0.61f, 0.19f, 1f), "A Traitor with a", "brainwashing gun. Aim it", "at a dead Innocent's", "corpse to revive them", "as a Traitor.");
		AddRole("ZOMBIE", new Color(0.37f, 0.8f, 0.1f), "Stab others with your", "knife to turn them into", "Zombies too. Infect", "everyone to win.");
		AddRole("SPECTATOR", new Color(0.67f, 0.67f, 0.67f), "You've been eliminated.", "Watch and wait for the", "round to end.");
	}

	private void AddRole(string name, Color color, params string[] lines)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		Page page = _page.CreatePage(name, color);
		foreach (string name2 in lines)
		{
			page.CreateFunction(name2, Color.white, delegate
			{
			});
		}
	}

	private void OnPageOpened(Page page)
	{
		if (page == _settingsPage)
		{
			RefreshKarma();
		}
	}

	private void RefreshKarma()
	{
		if (_karmaDisplay != null)
		{
			string elementName = (KarmaManager.Enabled ? $"Your Karma: {KarmaManager.GetLocalKarma()}" : "Your Karma: (Karma disabled)");
			_karmaDisplay.ElementName = elementName;
		}
	}

	public void Unregister()
	{
		Menu.OnPageOpened -= OnPageOpened;
		if (_page != null)
		{
			Page.Root.RemovePage(_page);
			_page = null;
		}
		if (_settingsPage != null)
		{
			Page.Root.RemovePage(_settingsPage);
			_settingsPage = null;
		}
	}
}

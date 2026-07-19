using BoneLib.BoneMenu;
using UnityEngine;

namespace TroubleInFordTown;

public class RolesInfoMenu
{
	private Page _page;

	private Page _settingsPage;

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
		_settingsPage = Page.Root.CreatePage("ML Settings", new Color(0.3f, 0.0f, 0.05f));
		_settingsPage.CreateBool("Mute Win Music (local)", new Color(0.9f, 0.5f, 0.2f), WinMusic.LocallyMuted, delegate(bool v)
		{
			WinMusic.LocallyMuted = v;
		});
		// More ML settings can be added here (game variant, etc.)
		Menu.OnPageOpened += OnPageOpened;
		_page = Page.Root.CreatePage("MurderLab Roles", Color.white);
		AddRole("Normal Bystander", new Color(0.05f, 0.46f, 0.92f), "No weapon. Collect loot.", "Stay alive.");
		AddRole("Bystander with Gun", new Color(0.41f, 0.28f, 0.85f), "You have a revolver.", "Use it wisely. Don't", "shoot bystanders.");
		AddRole("Murderer", new Color(0.71f, 0.05f, 0.10f), "Kill everyone. Don't", "get caught.");
		AddRole("Spectator", new Color(0.67f, 0.67f, 0.67f), "You've been eliminated.", "Watch and wait.");
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

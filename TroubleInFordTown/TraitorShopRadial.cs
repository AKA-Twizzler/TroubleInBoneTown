using System;
using Il2CppSLZ.Bonelab;
using Il2CppSystem;
using LabFusion.Data;
using LabFusion.UI.Popups;

namespace TroubleInFordTown;

public static class TraitorShopRadial
{
	private const int HealthCost = 1;

	private const int KnifeCost = 2;

	private static TroubleInFordTownGamemode _gamemode;

	private static bool _added;

	public static void AddMenuItems(TroubleInFordTownGamemode gamemode)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		RemoveMenuItems();
		_gamemode = gamemode;
		try
		{
			PopUpMenuView popUpMenu = UIRig.Instance.popUpMenu;
			Page homePage = popUpMenu.radialPageView.m_HomePage;
			homePage.items.Add(new PageItem($"Health Boost [{1} TP]", (Directions)5, Action.op_Implicit((Action)delegate
			{
				popUpMenu.Deactivate();
				BuyHealth();
			})));
			homePage.items.Add(new PageItem($"Knife [{2} TP]", (Directions)6, Action.op_Implicit((Action)delegate
			{
				popUpMenu.Deactivate();
				BuyKnife();
			})));
			popUpMenu.radialPageView.Render(homePage);
			_added = true;
		}
		catch
		{
		}
	}

	public static void RemoveMenuItems()
	{
		if (!_added)
		{
			return;
		}
		try
		{
			PopUpMenuView popUpMenu = UIRig.Instance.popUpMenu;
			Page homePage = popUpMenu.radialPageView.m_HomePage;
			homePage.items.RemoveAll(Predicate<PageItem>.op_Implicit((Func<PageItem, bool>)((PageItem i) => i.name != null && (i.name.StartsWith("Golden Pistol") || i.name.StartsWith("Shadow Knife") || i.name.StartsWith("Knife") || i.name.StartsWith("Health Boost")))));
			popUpMenu.radialPageView.Render(homePage);
		}
		catch
		{
		}
		_added = false;
		_gamemode = null;
	}

	private static void BuyHealth()
	{
		if (!TraitorPoints.TrySpend(1))
		{
			Notifier.Send(new Notification
			{
				Title = "Not Enough Traitor Points",
				Message = $"You need {1} TP but only have {TraitorPoints.Points}.",
				ShowPopup = true,
				PopupLength = 2f,
				Type = NotificationType.WARNING
			});
			return;
		}
		try
		{
			if (RigData.HasPlayer)
			{
				RigData.Refs.RigManager.health.SetFullHealth();
			}
		}
		catch
		{
		}
		Notifier.Send(new Notification
		{
			Title = "Health Boost!",
			Message = "Your health has been restored.",
			ShowPopup = true,
			PopupLength = 3f,
			Type = NotificationType.INFORMATION
		});
	}

	private static void BuyKnife()
	{
		if (!TraitorPoints.TrySpend(2))
		{
			Notifier.Send(new Notification
			{
				Title = "Not Enough Traitor Points",
				Message = $"You need {2} TP but only have {TraitorPoints.Points}.",
				ShowPopup = true,
				PopupLength = 2f,
				Type = NotificationType.WARNING
			});
		}
		else
		{
			try
			{
				_gamemode?.GiveTraitorKnife();
			}
			catch
			{
			}
			Notifier.Send(new Notification
			{
				Title = "Knife Purchased",
				Message = "A knife has been holstered. Others can't see it on your body.",
				ShowPopup = true,
				PopupLength = 3f,
				Type = NotificationType.INFORMATION
			});
		}
	}
}

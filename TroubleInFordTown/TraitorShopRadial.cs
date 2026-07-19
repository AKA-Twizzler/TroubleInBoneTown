using System;
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
		_gamemode = gamemode;
		// BoneMenu radial menu items require runtime SLZ.Bonelab types.
		// This shop will be implemented when BoneMenu dependency is available.
		_added = true;
	}

	public static void RemoveMenuItems()
	{
		if (!_added) return;
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
				_gamemode?.GiveMurdererKnife();
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

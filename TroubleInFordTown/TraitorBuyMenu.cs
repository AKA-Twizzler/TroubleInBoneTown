using BoneLib.BoneMenu;
using LabFusion.UI.Popups;
using UnityEngine;

namespace TroubleInFordTown;

public class TraitorBuyMenu
{
	private Page _shopPage;

	private TroubleInFordTownGamemode _gamemode;

	private FunctionElement _tpHeader;

	private FunctionElement _pistolBtn;

	private FunctionElement _knifeBtn;

	private FunctionElement _healthBtn;

	private FunctionElement _statusLabel;

	public void Register(TroubleInFordTownGamemode gamemode)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		_gamemode = gamemode;
		_shopPage = Page.Root.CreatePage("Traitor Shop", new Color(0.85f, 0.1f, 0.1f));
		_statusLabel = _shopPage.CreateFunction("Traitors only — in-round", Color.gray, delegate
		{
		});
		TraitorPoints.OnPointsChanged += Refresh;
		Menu.OnPageOpened += OnPageOpened;
	}

	private void OnPageOpened(Page page)
	{
		if (page == _shopPage)
		{
			Refresh();
		}
	}

	public void Unregister()
	{
		TraitorPoints.OnPointsChanged -= Refresh;
		Menu.OnPageOpened -= OnPageOpened;
		if (_shopPage != null)
		{
			Page.Root.RemovePage(_shopPage);
			_shopPage = null;
		}
		_gamemode = null;
	}

	public void Refresh()
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		if (_shopPage == null || _gamemode == null)
		{
			return;
		}
		_shopPage.RemoveAll();
		_tpHeader = null;
		_pistolBtn = null;
		_knifeBtn = null;
		_healthBtn = null;
		_statusLabel = null;
		if (!_gamemode.IsStarted || _gamemode.TeamManager.GetLocalTeam() != _gamemode.TraitorTeam)
		{
			_statusLabel = _shopPage.CreateFunction("Traitors only — in-round", Color.gray, delegate
			{
			});
			return;
		}
		int points = TraitorPoints.Points;
		_tpHeader = _shopPage.CreateFunction($"Traitor Points: {points}", Color.yellow, delegate
		{
			Refresh();
		});
		Color color = (Color)((points >= 1) ? new Color(0.2f, 0.9f, 0.3f) : Color.gray);
		_healthBtn = _shopPage.CreateFunction($"Health Boost  [{1} TP]  — restore full health", color, delegate
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
			}
			else
			{
				Notifier.Send(new Notification
				{
					Title = "Health Boost!",
					Message = "Your health has been restored.",
					ShowPopup = true,
					PopupLength = 3f,
					Type = NotificationType.INFORMATION
				});
				Refresh();
			}
		});
	}
}

using System;

namespace TroubleInFordTown;

public static class TraitorPoints
{
	public static int Points { get; private set; }

	public static event Action OnPointsChanged;

	public static void Reset()
	{
		Points = 2;
		TraitorPoints.OnPointsChanged?.Invoke();
	}

	public static void Clear()
	{
		Points = 0;
		TraitorPoints.OnPointsChanged?.Invoke();
	}

	public static void AddKillReward()
	{
		Points++;
		TraitorPoints.OnPointsChanged?.Invoke();
	}

	public static bool TrySpend(int cost)
	{
		if (Points < cost)
		{
			return false;
		}
		Points -= cost;
		TraitorPoints.OnPointsChanged?.Invoke();
		return true;
	}
}

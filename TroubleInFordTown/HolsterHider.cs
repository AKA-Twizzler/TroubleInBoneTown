using System.Collections.Generic;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using UnityEngine;

namespace TroubleInFordTown;

public class HolsterHider
{
	private const string KnifeBarcode = "c1534c5a-1fb8-477c-afbe-2a95436f6d62";

	private const string OneStabKnifeBarcode = "JonLandon.TFTknife.Spawnable.TFTknife";

	private readonly Dictionary<GameObject, List<Renderer>> _hiddenProps = new Dictionary<GameObject, List<Renderer>>();

	private readonly Dictionary<int, bool> _knifeCache = new Dictionary<int, bool>();

	private static readonly List<GameObject> _localKnives = new List<GameObject>();

	private readonly HashSet<GameObject> _found = new HashSet<GameObject>();

	private readonly HashSet<GameObject> _evaluate = new HashSet<GameObject>();

	private readonly List<GameObject> _dead = new List<GameObject>();

	private float _timer;

	private const float RefreshInterval = 0.3f;

	public static void RegisterLocalKnife(GameObject go)
	{
		if ((Object)(object)go != (Object)null)
		{
			_localKnives.Add(go);
		}
	}

	public void Update()
	{
		foreach (KeyValuePair<GameObject, List<Renderer>> hiddenProp in _hiddenProps)
		{
			List<Renderer> value = hiddenProp.Value;
			for (int i = 0; i < value.Count; i++)
			{
				Renderer val = value[i];
				try
				{
					if ((Object)(object)val != (Object)null && !val.forceRenderingOff)
					{
						val.forceRenderingOff = true;
					}
				}
				catch
				{
				}
			}
		}
		_timer -= Time.deltaTime;
		if (_timer > 0f)
		{
			return;
		}
		_timer = 0.3f;
		_localKnives.RemoveAll((GameObject g) => (Object)(object)g == (Object)null);
		_found.Clear();
		try
		{
			foreach (WeaponSlot item in Object.FindObjectsOfType<WeaponSlot>())
			{
				if (!((Object)(object)item == (Object)null) && IsKnife(item))
				{
					Poolee componentInParent = ((Component)item).gameObject.GetComponentInParent<Poolee>();
					GameObject val2 = (((Object)(object)componentInParent != (Object)null) ? ((Component)componentInParent).gameObject : ((Component)((Component)item).transform.root).gameObject);
					if ((Object)(object)val2 != (Object)null && !IsLocalKnife(val2))
					{
						_found.Add(val2);
					}
				}
			}
		}
		catch
		{
		}
		_evaluate.Clear();
		foreach (GameObject item2 in _found)
		{
			_evaluate.Add(item2);
		}
		foreach (GameObject key in _hiddenProps.Keys)
		{
			_evaluate.Add(key);
		}
		foreach (GameObject item3 in _evaluate)
		{
			if ((Object)(object)item3 == (Object)null)
			{
				DropHidden(item3);
			}
			else if (IsHeld(item3))
			{
				Reveal(item3);
			}
			else
			{
				Conceal(item3);
			}
		}
		_dead.Clear();
		foreach (GameObject key2 in _hiddenProps.Keys)
		{
			if ((Object)(object)key2 == (Object)null)
			{
				_dead.Add(key2);
			}
		}
		foreach (GameObject item4 in _dead)
		{
			DropHidden(item4);
		}
	}

	private void Conceal(GameObject prop)
	{
		if (_hiddenProps.ContainsKey(prop))
		{
			return;
		}
		List<Renderer> list = new List<Renderer>();
		try
		{
			foreach (Renderer componentsInChild in prop.GetComponentsInChildren<Renderer>(true))
			{
				if (!((Object)(object)componentsInChild == (Object)null))
				{
					componentsInChild.forceRenderingOff = true;
					list.Add(componentsInChild);
				}
			}
		}
		catch
		{
		}
		_hiddenProps[prop] = list;
	}

	private void Reveal(GameObject prop)
	{
		if (!_hiddenProps.TryGetValue(prop, out var value))
		{
			return;
		}
		foreach (Renderer item in value)
		{
			try
			{
				if ((Object)(object)item != (Object)null)
				{
					item.forceRenderingOff = false;
				}
			}
			catch
			{
			}
		}
		_hiddenProps.Remove(prop);
	}

	private void DropHidden(GameObject prop)
	{
		_hiddenProps.Remove(prop);
	}

	private static bool IsHeld(GameObject propGO)
	{
		try
		{
			foreach (Grip componentsInChild in propGO.GetComponentsInChildren<Grip>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && componentsInChild.attachedHands != null && componentsInChild.attachedHands.Count > 0)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsLocalKnife(GameObject propGO)
	{
		foreach (GameObject localKnife in _localKnives)
		{
			if ((Object)(object)localKnife == (Object)null)
			{
				continue;
			}
			if ((Object)(object)localKnife == (Object)(object)propGO)
			{
				return true;
			}
			try
			{
				if (localKnife.transform.IsChildOf(propGO.transform) || propGO.transform.IsChildOf(localKnife.transform))
				{
					return true;
				}
			}
			catch
			{
			}
		}
		return false;
	}

	private bool IsKnife(WeaponSlot weapon)
	{
		int instanceID = ((Object)weapon).GetInstanceID();
		if (_knifeCache.TryGetValue(instanceID, out var value))
		{
			return value;
		}
		try
		{
			GameObject gameObject = ((Component)weapon).gameObject;
			Poolee val = Poolee.Cache.Get(gameObject);
			if ((Object)(object)val == (Object)null)
			{
				val = gameObject.GetComponentInParent<Poolee>();
			}
			object obj;
			if (val == null)
			{
				obj = null;
			}
			else
			{
				SpawnableCrate spawnableCrate = val.SpawnableCrate;
				if (spawnableCrate == null)
				{
					obj = null;
				}
				else
				{
					Barcode barcode = ((Scannable)spawnableCrate).Barcode;
					obj = ((barcode != null) ? barcode.ID : null);
				}
			}
			string text = (string)obj;
			if (!string.IsNullOrEmpty(text))
			{
				bool flag = text == "c1534c5a-1fb8-477c-afbe-2a95436f6d62" || text == "JonLandon.TFTknife.Spawnable.TFTknife";
				_knifeCache[instanceID] = flag;
				return flag;
			}
		}
		catch
		{
		}
		return false;
	}

	public void ClearAll()
	{
		foreach (KeyValuePair<GameObject, List<Renderer>> hiddenProp in _hiddenProps)
		{
			foreach (Renderer item in hiddenProp.Value)
			{
				try
				{
					if ((Object)(object)item != (Object)null)
					{
						item.forceRenderingOff = false;
					}
				}
				catch
				{
				}
			}
		}
		_hiddenProps.Clear();
		_knifeCache.Clear();
		_localKnives.Clear();
	}
}

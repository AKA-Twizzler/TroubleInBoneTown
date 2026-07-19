using UnityEngine;

namespace TroubleInFordTown;

public class LootPickup : MonoBehaviour
{
	public int SpawnIndex { get; set; }

	private Light _pointLight;

	private float _phase;

	private void Awake()
	{
		// Add green point light for visibility
		GameObject lightGo = new GameObject("LootGlow");
		lightGo.transform.SetParent(base.transform, false);
		lightGo.transform.localPosition = Vector3.zero;
		_pointLight = lightGo.AddComponent<Light>();
		_pointLight.type = LightType.Point;
		_pointLight.color = Color.green;
		_pointLight.range = 3f;
		_pointLight.intensity = 1.2f;

		// Tint any existing renderers green with emissive glow
		TintGreen();
	}

	private void Update()
	{
		// Gentle pulsing effect on the light
		_phase += Time.deltaTime * 1.5f;
		if (_pointLight != null)
		{
			_pointLight.intensity = 1f + Mathf.Sin(_phase) * 0.3f;
		}
	}

	private void TintGreen()
	{
		Color baseColor = new Color(0f, 0.85f, 0.15f);
		Color emissiveColor = new Color(0f, 0.45f, 0.05f);

		// Tint MeshRenderers
		MeshRenderer[] meshes = GetComponentsInChildren<MeshRenderer>(true);
		foreach (MeshRenderer rend in meshes)
		{
			TintRenderer(rend, baseColor, emissiveColor);
		}

		// Tint SkinnedMeshRenderers
		SkinnedMeshRenderer[] skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
		foreach (SkinnedMeshRenderer rend in skinned)
		{
			TintRenderer(rend, baseColor, emissiveColor);
		}
	}

	private static void TintRenderer(Renderer rend, Color baseColor, Color emissiveColor)
	{
		if (rend == null) return;
		try
		{
			Material[] materials = rend.materials;
			for (int i = 0; i < materials.Length; i++)
			{
				Material mat = materials[i];
				if (mat == null) continue;

				if (mat.HasProperty("_BaseColor"))
					mat.SetColor("_BaseColor", baseColor);
				if (mat.HasProperty("_Color"))
					mat.SetColor("_Color", baseColor);
				if (mat.HasProperty("_MainColor"))
					mat.SetColor("_MainColor", baseColor);
				if (mat.HasProperty("_TintColor"))
					mat.SetColor("_TintColor", baseColor);
				if (mat.HasProperty("_AlbedoColor"))
					mat.SetColor("_AlbedoColor", baseColor);
				if (mat.HasProperty("_EmissiveColor"))
					mat.SetColor("_EmissiveColor", emissiveColor);
				if (mat.HasProperty("_EmissionColor"))
					mat.SetColor("_EmissionColor", emissiveColor);
				if (mat.HasProperty("_Emission"))
					mat.SetColor("_Emission", emissiveColor);

				mat.EnableKeyword("_EMISSION");
				mat.EnableKeyword("_EMISSIVE_COLOR_MAP");
			}
			rend.materials = materials;
		}
		catch
		{
		}
	}
}

using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using LabFusion.Data;
using MelonLoader;
using UnityEngine;

namespace TroubleInFordTown;

/// <summary>
/// Full-screen black overlay for the MurderLab prep phase.
/// Shows role-specific text and plays role audio when available.
/// Audio files to be extracted from GMod workshop ID 187073946.
/// </summary>
public class BlackScreenController
{
	private GameObject _root;
	private GameObject _background;
	private TextMeshPro _titleText;
	private TextMeshPro _subtitleText;
	private TextMeshPro _footerText1;
	private TextMeshPro _footerText2;

	// Audio source for role-specific sound effects.
	// Will use extracted WAV files from GMod workshop ID 187073946.
	private AudioSource _audioSource;

	private static TMP_FontAsset _font;

	public bool IsCreated => (Object)(object)_root != (Object)null;

	public bool IsShown => IsCreated && _root.activeSelf;

	private static TMP_FontAsset GetFont()
	{
		if ((Object)(object)_font != (Object)null)
		{
			return _font;
		}
		Il2CppArrayBase<TMP_FontAsset> val = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
		foreach (TMP_FontAsset item in val)
		{
			if (((Object)item).name.ToLower().Contains("arlon-medium"))
			{
				_font = item;
				break;
			}
		}
		if ((Object)(object)_font == (Object)null && val.Length > 0)
		{
			_font = val[0];
		}
		return _font;
	}

	public void Create()
	{
		if (IsCreated)
		{
			return;
		}
		try
		{
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			_root = new GameObject("ML Prep Overlay");
			Object.DontDestroyOnLoad((Object)(object)_root);
			((Object)_root).hideFlags = (HideFlags)61;

			// Black background quad
			_background = GameObject.CreatePrimitive((PrimitiveType)9); // PrimitiveType.Quad = 9
			_background.name = "ML Prep BG";
			_background.transform.SetParent(_root.transform, false);
			_background.transform.localScale = new Vector3(20f, 20f, 1f);
			_background.transform.localPosition = new Vector3(0f, 0f, 2f);

			// Make it black (use Sprites/Default for alpha support)
			Renderer renderer = _background.GetComponent<Renderer>();
			Material mat = new Material(Shader.Find("Sprites/Default"));
			mat.color = Color.black;
			renderer.material = mat;

			// Role title (big, top)
			_titleText = CreateText("ML Prep Title", new Vector3(0f, 1.5f, 1.8f), 1.0f);

			// Role subtitle (medium, below title)
			_subtitleText = CreateText("ML Prep Subtitle", new Vector3(0f, 0.5f, 1.8f), 0.6f);

			// Footer lines (small, bottom)
			_footerText1 = CreateText("ML Prep Footer1", new Vector3(0f, -0.3f, 1.8f), 0.4f);
			_footerText2 = CreateText("ML Prep Footer2", new Vector3(0f, -0.8f, 1.8f), 0.4f);

			// Audio source for role-specific SFX.
			// Audio files will be loaded from embedded resources once extracted from GMod workshop ID 187073946.
			_audioSource = _root.AddComponent<AudioSource>();
			_audioSource.spatialBlend = 0f; // 2D sound (full-screen)
			_audioSource.playOnAwake = false;
			_audioSource.volume = 1f;

			_root.SetActive(false);
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("ML Prep: BlackScreenController.Create failed: " + ex.Message);
		}
	}

	private TextMeshPro CreateText(string name, Vector3 localPos, float fontSize)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		GameObject val = new GameObject(name);
		val.transform.SetParent(_root.transform, false);
		val.transform.localPosition = localPos;

		TextMeshPro val2 = val.AddComponent<TextMeshPro>();
		TMP_FontAsset font = GetFont();
		if ((Object)(object)font != (Object)null)
		{
			((TMP_Text)val2).font = font;
		}
		((TMP_Text)val2).fontSize = fontSize;
		((TMP_Text)val2).alignment = (TextAlignmentOptions)4; // Center
		((TMP_Text)val2).text = "";

		return val2;
	}

	public void Show()
	{
		if (!IsCreated)
		{
			return;
		}
		if (!RigData.HasPlayer)
		{
			return;
		}
		_root.SetActive(true);
		UpdatePosition();
		SetAlpha(1f);
	}

	public void Hide()
	{
		if ((Object)(object)_root != (Object)null)
		{
			_root.SetActive(false);
		}
	}

	/// <summary>
	/// Display role-specific text on the black screen.
	/// </summary>
	public void DisplayRoleText(string title, string subtitle, string footer1, string footer2,
		Color titleColor, Color subtitleColor)
	{
		try
		{
			if ((Object)(object)_titleText != (Object)null)
			{
				((TMP_Text)_titleText).text = title;
				((TMP_Text)_titleText).color = titleColor;
			}
			if ((Object)(object)_subtitleText != (Object)null)
			{
				((TMP_Text)_subtitleText).text = subtitle;
				((TMP_Text)_subtitleText).color = subtitleColor;
			}
			if ((Object)(object)_footerText1 != (Object)null)
			{
				((TMP_Text)_footerText1).text = footer1;
				((TMP_Text)_footerText1).color = titleColor;
			}
			if ((Object)(object)_footerText2 != (Object)null)
			{
				((TMP_Text)_footerText2).text = footer2;
				((TMP_Text)_footerText2).color = titleColor;
			}

			// PLACEHOLDER: Role-specific audio not yet available.
			// Audio files need extraction from GMod workshop ID 187073946.
			// Once extracted as WAV, load via WavLoader and play via _audioSource.
			//   - Murderer: Scream / menacing sound
			//   - Bystanders: Tense ambient / heartbeat SFX
			MelonLogger.Warning("ML Prep: Role audio not yet available (GMod workshop ID 187073946)");
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("ML Prep: DisplayRoleText failed: " + ex.Message);
		}
	}

	/// <summary>
	/// Start fading the black screen out over the given duration.
	/// Call Update() each frame while fading.
	/// Returns false if already fading or no overlay is shown.
	/// </summary>
	public bool StartFadeOut(float duration)
	{
		if (!IsShown)
		{
			return false;
		}
		_fadeDuration = duration;
		_fadeTimer = duration;
		_isFading = true;
		_fadeComplete = false;
		return true;
	}

	/// <summary>
	/// Call every frame while the overlay is active.
	/// Returns true while fading is in progress.
	/// </summary>
	public bool Update()
	{
		if (!IsCreated || !_root.activeSelf)
		{
			return false;
		}
		try
		{
			UpdatePosition();
			if (_isFading)
			{
				_fadeTimer -= Time.deltaTime;
				if (_fadeTimer <= 0f)
				{
					_isFading = false;
					_fadeComplete = true;
					Hide();
					return false;
				}
				SetAlpha(_fadeTimer / _fadeDuration);
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public bool IsFading => _isFading;

	public bool IsFadeComplete => _fadeComplete;

	private float _fadeDuration = 1f;
	private float _fadeTimer;
	private bool _isFading;
	private bool _fadeComplete;

	private void UpdatePosition()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (!RigData.HasPlayer)
		{
			return;
		}
		Transform headset = RigData.Refs.Headset;
		if ((Object)(object)headset == (Object)null)
		{
			return;
		}
		// Place overlay 2m in front of the player, facing them
		_root.transform.position = headset.position + headset.forward * 2f;
		_root.transform.rotation = Quaternion.LookRotation(-headset.forward);
	}

	private void SetAlpha(float alpha)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_background != (Object)null)
		{
			Renderer renderer = _background.GetComponent<Renderer>();
			if ((Object)(object)renderer != (Object)null)
			{
				Color color = renderer.material.color;
				color.a = Mathf.Clamp01(alpha);
				renderer.material.color = color;
			}
		}
		SetTextMeshAlpha(_titleText, alpha);
		SetTextMeshAlpha(_subtitleText, alpha);
		SetTextMeshAlpha(_footerText1, alpha);
		SetTextMeshAlpha(_footerText2, alpha);
	}

	private static void SetTextMeshAlpha(TextMeshPro tmp, float alpha)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)tmp == (Object)null)
		{
			return;
		}
		Color color = ((TMP_Text)tmp).color;
		color.a = Mathf.Clamp01(alpha);
		((TMP_Text)tmp).color = color;
	}

	public void Destroy()
	{
		if ((Object)(object)_root != (Object)null)
		{
			Object.Destroy((Object)(object)_root);
		}
		_root = null;
		_background = null;
		_titleText = null;
		_subtitleText = null;
		_footerText1 = null;
		_footerText2 = null;
		_audioSource = null;
	}
}

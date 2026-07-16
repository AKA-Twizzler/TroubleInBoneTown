using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Il2CppSLZ.Marrow.Audio;
using MelonLoader;
using UnityEngine;
using UnityEngine.Audio;

namespace TroubleInFordTown;

public static class WinMusic
{
	private static readonly List<AudioClip> _innocentLines = new List<AudioClip>();

	private static readonly List<AudioClip> _innocentSongs = new List<AudioClip>();

	private static readonly List<AudioClip> _traitorLines = new List<AudioClip>();

	private static readonly List<AudioClip> _traitorSongs = new List<AudioClip>();

	private static readonly List<AudioClip> _jesterLines = new List<AudioClip>();

	private static readonly List<AudioClip> _jesterSongs = new List<AudioClip>();

	private static readonly List<AudioClip> _loneWolfSongs = new List<AudioClip>();

	private static AudioSource _voiceSource;

	private static AudioSource _musicSource;

	private static bool _initialized;

	private static object _pendingSongRoutine;

	public static bool LocallyMuted = false;

	private static readonly List<AudioClip> _noLines = new List<AudioClip>();

	public static void Initialize()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		try
		{
			GameObject val = new GameObject("TIFT Win Music");
			Object.DontDestroyOnLoad((Object)val);
			((Object)val).hideFlags = (HideFlags)61;
			_voiceSource = val.AddComponent<AudioSource>();
			_voiceSource.spatialBlend = 0f;
			_voiceSource.volume = 1f;
			_voiceSource.playOnAwake = false;
			_musicSource = val.AddComponent<AudioSource>();
			_musicSource.spatialBlend = 0f;
			_musicSource.volume = 1f;
			_musicSource.playOnAwake = false;
			LoadInto("innocents_song1.wav", _innocentSongs);
			LoadInto("innocents_song2.wav", _innocentSongs);
			LoadInto("traitors_song1.wav", _traitorSongs);
			LoadInto("traitors_song2.wav", _traitorSongs);
			LoadInto("jester_song1.wav", _jesterSongs);
			LoadInto("jester_song2.wav", _jesterSongs);
			LoadInto("lonewolf_song1.wav", _loneWolfSongs);
			AudioClip val2 = LoadEmbedded("brainwash_charge.wav");
			if ((Object)(object)val2 != (Object)null)
			{
				HypnotistController.ChargeClip = val2;
			}
			LoadInto("innocents_line1.wav", _innocentLines);
			LoadInto("innocents_line2.wav", _innocentLines);
			LoadInto("traitors_line1.wav", _traitorLines);
			LoadInto("traitors_line2.wav", _traitorLines);
			LoadInto("jester_line1.wav", _jesterLines);
			LoadInto("jester_line2.wav", _jesterLines);
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("WinMusic init failed: " + ex.Message);
		}
	}

	public static void PlayWin(WinSide side, bool playSong)
	{
		if ((Object)(object)_voiceSource == (Object)null || (Object)(object)_musicSource == (Object)null)
		{
			return;
		}
		AudioClip val = RandomClip(LinesFor(side));
		AudioClip val2 = RandomClip(SongsFor(side));
		bool flag = playSong && !LocallyMuted;
		try
		{
			if (_pendingSongRoutine != null)
			{
				MelonCoroutines.Stop(_pendingSongRoutine);
				_pendingSongRoutine = null;
			}
			_voiceSource.Stop();
			_musicSource.Stop();
			ApplyMixerGroups();
			if ((Object)(object)val != (Object)null)
			{
				_voiceSource.PlayOneShot(val);
				if ((Object)(object)val2 != (Object)null && flag)
				{
					_pendingSongRoutine = MelonCoroutines.Start(PlayAfter(val.length, val2));
				}
			}
			else if ((Object)(object)val2 != (Object)null && flag)
			{
				_musicSource.clip = val2;
				_musicSource.Play();
			}
		}
		catch
		{
		}
	}

	private static void ApplyMixerGroups()
	{
		try
		{
			AudioMixerGroup hardInteraction = Audio3dManager.hardInteraction;
			AudioMixerGroup nonDiegeticMusic = Audio3dManager.nonDiegeticMusic;
			if ((Object)(object)hardInteraction != (Object)null)
			{
				_voiceSource.outputAudioMixerGroup = hardInteraction;
			}
			if ((Object)(object)nonDiegeticMusic != (Object)null)
			{
				_musicSource.outputAudioMixerGroup = nonDiegeticMusic;
			}
		}
		catch
		{
		}
	}

	public static float GetMaxDuration(WinSide side, bool includeSong)
	{
		float num = MaxLength(LinesFor(side));
		float num2 = (includeSong ? MaxLength(SongsFor(side)) : 0f);
		if (num <= 0f && num2 <= 0f)
		{
			return 0f;
		}
		return num + num2 + 1.5f;
	}

	public static void Stop()
	{
		try
		{
			if (_pendingSongRoutine != null)
			{
				MelonCoroutines.Stop(_pendingSongRoutine);
				_pendingSongRoutine = null;
			}
			if ((Object)(object)_voiceSource != (Object)null && _voiceSource.isPlaying)
			{
				_voiceSource.Stop();
			}
			if ((Object)(object)_musicSource != (Object)null && _musicSource.isPlaying)
			{
				_musicSource.Stop();
			}
		}
		catch
		{
		}
	}

	private static IEnumerator PlayAfter(float delay, AudioClip song)
	{
		yield return (object)new WaitForSeconds(delay);
		if ((Object)(object)_musicSource != (Object)null && (Object)(object)song != (Object)null)
		{
			_musicSource.clip = song;
			_musicSource.Play();
		}
		_pendingSongRoutine = null;
	}

	private static List<AudioClip> LinesFor(WinSide side)
	{
		return side switch
		{
			WinSide.Traitors => _traitorLines, 
			WinSide.Jester => _jesterLines, 
			WinSide.LoneWolf => _noLines, 
			_ => _innocentLines, 
		};
	}

	private static List<AudioClip> SongsFor(WinSide side)
	{
		return side switch
		{
			WinSide.Traitors => _traitorSongs, 
			WinSide.Jester => _jesterSongs, 
			WinSide.LoneWolf => _loneWolfSongs, 
			_ => _innocentSongs, 
		};
	}

	private static AudioClip RandomClip(List<AudioClip> clips)
	{
		if (clips == null || clips.Count == 0)
		{
			return null;
		}
		return clips[Random.Range(0, clips.Count)];
	}

	private static float MaxLength(List<AudioClip> clips)
	{
		float num = 0f;
		if (clips != null)
		{
			foreach (AudioClip clip in clips)
			{
				if ((Object)(object)clip != (Object)null && clip.length > num)
				{
					num = clip.length;
				}
			}
		}
		return num;
	}

	private static void LoadInto(string fileName, List<AudioClip> target)
	{
		AudioClip val = LoadEmbedded(fileName);
		if ((Object)(object)val != (Object)null)
		{
			target.Add(val);
		}
	}

	private static AudioClip LoadEmbedded(string fileName)
	{
		try
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			string text = null;
			string[] manifestResourceNames = executingAssembly.GetManifestResourceNames();
			foreach (string text2 in manifestResourceNames)
			{
				if (text2.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
				{
					text = text2;
					break;
				}
			}
			if (text == null)
			{
				MelonLogger.Warning("WinMusic: embedded resource '" + fileName + "' not found");
				return null;
			}
			using Stream stream = executingAssembly.GetManifestResourceStream(text);
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			AudioClip val = WavLoader.Load(memoryStream.ToArray(), Path.GetFileNameWithoutExtension(fileName));
			if ((Object)(object)val != (Object)null)
			{
				MelonLogger.Msg($"WinMusic: loaded {fileName} ({val.length:0.0}s)");
			}
			return val;
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("WinMusic: failed to load " + fileName + ": " + ex.Message);
			return null;
		}
	}
}

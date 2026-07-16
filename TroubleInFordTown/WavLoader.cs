using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace TroubleInFordTown;

public static class WavLoader
{
	public static AudioClip Load(byte[] data, string name)
	{
		try
		{
			return Parse(data, name);
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("WinMusic: failed to decode WAV " + name + ": " + ex.Message);
			return null;
		}
	}

	private static AudioClip Parse(byte[] data, string name)
	{
		if (data.Length < 12 || data[0] != 82 || data[1] != 73 || data[2] != 70 || data[3] != 70 || data[8] != 87 || data[9] != 65 || data[10] != 86 || data[11] != 69)
		{
			MelonLogger.Warning("WinMusic: " + name + " is not a valid WAV file");
			return null;
		}
		int num = 2;
		int num2 = 44100;
		int num3 = 16;
		int num4 = 1;
		int num5 = -1;
		int num6 = 0;
		int num7 = 12;
		while (num7 + 8 <= data.Length)
		{
			string text = $"{(char)data[num7]}{(char)data[num7 + 1]}{(char)data[num7 + 2]}{(char)data[num7 + 3]}";
			int num8 = BitConverter.ToInt32(data, num7 + 4);
			int num9 = num7 + 8;
			if (text == "fmt ")
			{
				num4 = BitConverter.ToInt16(data, num9);
				num = BitConverter.ToInt16(data, num9 + 2);
				num2 = BitConverter.ToInt32(data, num9 + 4);
				num3 = BitConverter.ToInt16(data, num9 + 14);
			}
			else if (text == "data")
			{
				num5 = num9;
				num6 = num8;
			}
			num7 = num9 + num8 + (num8 & 1);
		}
		if (num5 < 0 || num <= 0)
		{
			MelonLogger.Warning("WinMusic: " + name + " missing data/fmt chunk");
			return null;
		}
		if (num5 + num6 > data.Length)
		{
			num6 = data.Length - num5;
		}
		int num10 = num3 / 8;
		int num11 = num6 / num10;
		float[] array = new float[num11];
		for (int i = 0; i < num11; i++)
		{
			int num12 = num5 + i * num10;
			switch (num3)
			{
			case 8:
				array[i] = (float)(data[num12] - 128) / 128f;
				break;
			case 16:
				array[i] = (float)BitConverter.ToInt16(data, num12) / 32768f;
				break;
			case 24:
			{
				int num13 = data[num12] | (data[num12 + 1] << 8) | ((sbyte)data[num12 + 2] << 16);
				array[i] = (float)num13 / 8388608f;
				break;
			}
			case 32:
				array[i] = ((num4 == 3) ? BitConverter.ToSingle(data, num12) : ((float)BitConverter.ToInt32(data, num12) / 2.1474836E+09f));
				break;
			default:
				return null;
			}
		}
		AudioClip val = AudioClip.Create(name, num11 / num, num, num2, false);
		Il2CppStructArray<float> val2 = new Il2CppStructArray<float>((long)num11);
		for (int j = 0; j < num11; j++)
		{
			((Il2CppArrayBase<float>)(object)val2)[j] = array[j];
		}
		val.SetData(val2, 0);
		((Object)val).hideFlags = (HideFlags)32;
		return val;
	}
}

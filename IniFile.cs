using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;

namespace TotalMixKeyControl
{
	internal static class IniFile
	{
		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);

		public static string ReadString(string path, string section, string key, string defaultValue)
		{
			var sb = new StringBuilder(1024);
			GetPrivateProfileString(section, key, defaultValue, sb, sb.Capacity, path);
			return sb.ToString();
		}

		public static int ReadInt(string path, string section, string key, int defaultValue)
		{
			var s = ReadString(path, section, key, defaultValue.ToString(CultureInfo.InvariantCulture));
			return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
		}

		public static float ReadFloat(string path, string section, string key, float defaultValue)
		{
			var s = ReadString(path, section, key, defaultValue.ToString(CultureInfo.InvariantCulture));
			return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
		}

		public static void WriteString(string path, string section, string key, string value)
		{
			if (!WritePrivateProfileString(section, key, value, path))
			{
				int err = Marshal.GetLastWin32Error();
				throw new InvalidOperationException($"Failed to write INI value (Win32 {err}) to '{path}'");
			}
		}
	}
}

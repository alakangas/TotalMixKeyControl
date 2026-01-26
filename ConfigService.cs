using System.Globalization;

namespace TotalMixKeyControl
{
	internal static class ConfigService
	{
		private const string SectionOsc = "OSC";
		private const string SectionVolume = "Volume";
		private const string SectionOsd = "OSD";
		private const string SectionSettings = "Settings";
		private const string SectionHotkeys = "Hotkeys";

		private const string KeyPort = "Port";
		private const string KeyOutPort = "OutPort";
		private const string KeyIp = "IP";
		private const string KeyAddress = "Address";

		private const string KeyVolumeStep = "VolumeStep";

		private const string KeyHideTrayIcon = "HideTrayIcon";
		private const string KeyOsdDisplayTime = "DisplayTime";
		private const string KeyOsdEnabled = "Enabled";
		private const string KeyOsdPosition = "Position";
		private const string KeyOsdMarginPreset = "MarginPreset";

		private const string KeyVolumeUpHotkey = "VolumeUpHotkey";
		private const string KeyVolumeDownHotkey = "VolumeDownHotkey";
		private const string KeyVolumeMuteHotkey = "VolumeMuteHotkey";

		public static AppConfig Load(string path)
		{
			var config = new AppConfig
			{
				OscIp = IniFile.ReadString(path, SectionOsc, KeyIp, "127.0.0.1"),
				OscSendPort = IniFile.ReadInt(path, SectionOsc, KeyPort, 7001),
				OscReceivePort = IniFile.ReadInt(path, SectionOsc, KeyOutPort, 9001),
				OscAddress = IniFile.ReadString(path, SectionOsc, KeyAddress, "/1/volume4"),
				VolumeStep = IniFile.ReadFloat(path, SectionVolume, KeyVolumeStep, 0.01f),
				HideTrayIcon = IniFile.ReadInt(path, SectionSettings, KeyHideTrayIcon, 0),
				OsdDisplayTimeMs = IniFile.ReadInt(path, SectionOsd, KeyOsdDisplayTime, 1900),
				OsdEnabled = IniFile.ReadInt(path, SectionOsd, KeyOsdEnabled, 1) != 0,
				OsdPosition = IniFile.ReadString(path, SectionOsd, KeyOsdPosition, "BottomCenter"),
				OsdMarginPreset = IniFile.ReadString(path, SectionOsd, KeyOsdMarginPreset, "Small"),
				VolumeUpHotkey = IniFile.ReadString(path, SectionHotkeys, KeyVolumeUpHotkey, string.Empty),
				VolumeDownHotkey = IniFile.ReadString(path, SectionHotkeys, KeyVolumeDownHotkey, string.Empty),
				VolumeMuteHotkey = IniFile.ReadString(path, SectionHotkeys, KeyVolumeMuteHotkey, string.Empty)
			};

			return config;
		}

		public static void Save(string path, AppConfig config)
		{
			IniFile.WriteString(path, SectionOsc, KeyIp, config.OscIp);
			IniFile.WriteString(path, SectionOsc, KeyPort, config.OscSendPort.ToString(CultureInfo.InvariantCulture));
			IniFile.WriteString(path, SectionOsc, KeyOutPort, config.OscReceivePort.ToString(CultureInfo.InvariantCulture));
			IniFile.WriteString(path, SectionOsc, KeyAddress, config.OscAddress);
			IniFile.WriteString(path, SectionHotkeys, KeyVolumeUpHotkey, config.VolumeUpHotkey);
			IniFile.WriteString(path, SectionHotkeys, KeyVolumeDownHotkey, config.VolumeDownHotkey);
			IniFile.WriteString(path, SectionHotkeys, KeyVolumeMuteHotkey, config.VolumeMuteHotkey);
			IniFile.WriteString(path, SectionVolume, KeyVolumeStep, config.VolumeStep.ToString(CultureInfo.InvariantCulture));
			IniFile.WriteString(path, SectionOsd, KeyOsdEnabled, config.OsdEnabled ? "1" : "0");
			IniFile.WriteString(path, SectionOsd, KeyOsdPosition, config.OsdPosition);
			IniFile.WriteString(path, SectionOsd, KeyOsdMarginPreset, config.OsdMarginPreset);
			IniFile.WriteString(path, SectionOsd, KeyOsdDisplayTime, config.OsdDisplayTimeMs.ToString(CultureInfo.InvariantCulture));
			IniFile.WriteString(path, SectionSettings, KeyHideTrayIcon, config.HideTrayIcon.ToString(CultureInfo.InvariantCulture));
		}
	}
}

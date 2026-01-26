namespace TotalMixKeyControl
{
	internal sealed class AppConfig
	{
		public string OscIp { get; set; } = "127.0.0.1";
		public int OscSendPort { get; set; } = 7001;
		public int OscReceivePort { get; set; } = 9001;
		public string OscAddress { get; set; } = "/1/volume4";
		public float VolumeStep { get; set; } = 0.01f;
		public int OsdDisplayTimeMs { get; set; } = 1900;
		public bool OsdEnabled { get; set; } = true;
		public string OsdPosition { get; set; } = "BottomCenter";
		public string OsdMarginPreset { get; set; } = "Small";
		public int HideTrayIcon { get; set; } = 0;
		public string VolumeUpHotkey { get; set; } = string.Empty;
		public string VolumeDownHotkey { get; set; } = string.Empty;
		public string VolumeMuteHotkey { get; set; } = string.Empty;
	}
}

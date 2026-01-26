using System.ComponentModel;
using System.Text.RegularExpressions;

namespace TotalMixKeyControl
{
	public partial class MainForm : Form
	{
		private readonly string _configPath;
		private readonly NotifyIcon _notifyIcon;
		private readonly ContextMenuStrip _contextMenuStrip;
		private readonly OscClient _oscClient;
		private readonly OscReceiver _oscReceiver;
		private readonly HotkeyManager _hotkeyManager;
		private readonly OsdForm _osdForm;

		// Config values
		private string _oscIp = "127.0.0.1";
		private int _oscSendPort = 7001;
		private int _oscReceivePort = 9001;
		private string _oscAddress = "/1/volume4";
		private float _volumeStep = 0.01f;
		private int _osdDisplayTimeMs = 1900;
		private bool _osdEnabled = true;
		private string _osdPosition = "BottomCenter";
		private string _osdMarginPreset = "Small";

		// INI sections/keys
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
		private const string KeyMaxValue = "MaxValue";

		private const string KeyHideTrayIcon = "HideTrayIcon";
		private const string KeyOsdDisplayTime = "DisplayTime";
		private const string KeyOsdEnabled = "Enabled";
		private const string KeyOsdPosition = "Position";
		private const string KeyOsdMarginPreset = "MarginPreset";

		private const string KeyVolumeUpHotkey = "VolumeUpHotkey";
		private const string KeyVolumeDownHotkey = "VolumeDownHotkey";
		private const string KeyVolumeMuteHotkey = "VolumeMuteHotkey";

		// Private fields
		private readonly System.Windows.Forms.Timer _renderTimer;
		private const int RenderCoalesceMs = 25;
		private const float VolumeMax = 1.0f;
		private float _volume;
		private float _volumeReceivedValue;
		private string _volumeReceivedString = string.Empty;
		private float _volumeBeforeMute;
		private bool _mute;
		private int _hideTrayIcon;
		private bool _oscBusOutputInitialized;
		private volatile bool _contextMenuOpen;

		public MainForm(string configPath)
		{
			_configPath = configPath;
			ShowInTaskbar = false;
			WindowState = FormWindowState.Minimized;
			FormBorderStyle = FormBorderStyle.FixedToolWindow;
			Opacity = 0;

			_oscClient = new OscClient();
			_oscReceiver = new OscReceiver();
			_hotkeyManager = new HotkeyManager(this);
			_osdForm = new OsdForm();
			_renderTimer = new System.Windows.Forms.Timer();
			_renderTimer.Tick += (_, _) => RenderTimer_Tick();

			_contextMenuStrip = BuildMenu();
			_notifyIcon = new NotifyIcon
			{
				Text = "TotalMix Key Control",
				Visible = true,
				ContextMenuStrip = _contextMenuStrip,
				Icon = SystemIcons.Application
			};
			_notifyIcon.DoubleClick += (_, _) => ShowSetup();
			// Show context menu on left-click as well for convenience
			_notifyIcon.MouseUp += (_, mouseEventArgs) =>
			{
				try
				{
					if (mouseEventArgs.Button == MouseButtons.Left) _contextMenuStrip.Show(MousePosition);
				}
				catch (Exception exception)
				{
					Console.WriteLine($"Failed to show context menu: {exception.Message}");
				}
			};

			// Try to load custom icon from output directory (copied at build)
			try
			{
				var iconPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");
				if (File.Exists(iconPath))
				{
					var notifyIcon = new Icon(iconPath);
					_notifyIcon.Icon = notifyIcon;
					Icon = notifyIcon;
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Failed to load custom icon: {exception.Message}");
			}

			if (!File.Exists(_configPath))
			{
				try
				{
					ShowSetup(true);
				}
				catch (Exception exception)
				{
					Console.WriteLine($"Failed to show initial setup: {exception.Message}");
				}
			}

			LoadConfigAndInit();
		}

		private ContextMenuStrip BuildMenu()
		{
			var contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Opening += (_, _) => _contextMenuOpen = true;
			contextMenuStrip.Closed += (_, _) => _contextMenuOpen = false;
			var setupMenuItem = new ToolStripMenuItem("Setup", null, (_, _) => ShowSetup());
			var exitMenuItem = new ToolStripMenuItem("Exit", null, (_, _) => Close());
			contextMenuStrip.Items.Add(setupMenuItem);
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(exitMenuItem);
			return contextMenuStrip;
		}

		private void LoadConfigAndInit()
		{
			try
			{
				// Read OSC
				_oscIp = IniFile.ReadString(_configPath, SectionOsc, KeyIp, _oscIp);
				_oscSendPort = IniFile.ReadInt(_configPath, SectionOsc, KeyPort, _oscSendPort);
				_oscReceivePort = IniFile.ReadInt(_configPath, SectionOsc, KeyOutPort, _oscReceivePort);
				_oscAddress = IniFile.ReadString(_configPath, SectionOsc, KeyAddress, _oscAddress);

				// Volume
				_volumeStep = IniFile.ReadFloat(_configPath, SectionVolume, KeyVolumeStep, _volumeStep);

				// Settings
				_hideTrayIcon = IniFile.ReadInt(_configPath, SectionSettings, KeyHideTrayIcon, _hideTrayIcon);
				_notifyIcon.Visible = _hideTrayIcon == 0;

				// OSD
				_osdDisplayTimeMs = IniFile.ReadInt(_configPath, SectionOsd, KeyOsdDisplayTime, _osdDisplayTimeMs);
				_osdEnabled = IniFile.ReadInt(_configPath, SectionOsd, KeyOsdEnabled, _osdEnabled ? 1 : 0) != 0;
				_osdPosition = IniFile.ReadString(_configPath, SectionOsd, KeyOsdPosition, _osdPosition);
				_osdMarginPreset = IniFile.ReadString(_configPath, SectionOsd, KeyOsdMarginPreset, _osdMarginPreset);

				_osdForm.ConfigureLayout(_osdPosition, _osdMarginPreset);

				// Connect OSC
				_oscClient.Configure(_oscIp, _oscSendPort);
				StartOscReceiver();
				InitializeOscBusOutput();

				// Hotkeys
				RegisterHotkeysFromIni();
			}
			catch (Exception exception)
			{
				MessageBox.Show($"Failed to initialize: {exception.Message}", "TotalMix Key Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void RegisterHotkeysFromIni()
		{
			_hotkeyManager.UnregisterAll();

			var upString = IniFile.ReadString(_configPath, SectionHotkeys, KeyVolumeUpHotkey, string.Empty);
			var downString = IniFile.ReadString(_configPath, SectionHotkeys, KeyVolumeDownHotkey, string.Empty);
			var muteString = IniFile.ReadString(_configPath, SectionHotkeys, KeyVolumeMuteHotkey, string.Empty);

			if (HotkeyParser.TryParse(upString, out var parsedUpHotkey))
			{
				_hotkeyManager.Register(parsedUpHotkey.Modifiers, parsedUpHotkey.Key, VolumeUp);
			}
			if (HotkeyParser.TryParse(downString, out var parsedDownHotkey))
			{
				_hotkeyManager.Register(parsedDownHotkey.Modifiers, parsedDownHotkey.Key, VolumeDown);
			}
			if (HotkeyParser.TryParse(muteString, out var parsedMuteHotkey))
			{
				_hotkeyManager.Register(parsedMuteHotkey.Modifiers, parsedMuteHotkey.Key, VolumeMute);
			}
		}

		private void VolumeUp()
		{
			if (_mute)
			{
				_mute = false;
				_volume = _volumeBeforeMute;
			}
			_volume = Math.Min(_volume + _volumeStep, VolumeMax);
			SendOscVolume(_volume);
		}

		private void VolumeDown()
		{
			if (_mute)
			{
				_mute = false;
				_volume = _volumeBeforeMute;
			}
			_volume = Math.Max(_volume - _volumeStep, 0f);
			SendOscVolume(_volume);
		}

		private void VolumeMute()
		{
			if (!_mute)
			{
				_mute = true;
				_volumeBeforeMute = _volume;
				_volume = 0f;
			}
			else
			{
				_mute = false;
				_volume = _volumeBeforeMute;
			}

			SendOscVolume(_volume);
		}

		private void InitializeOscBusOutput()
		{
			if (_oscBusOutputInitialized)
			{
				return;
			}

			var pageMatch = MyRegex().Match(_oscAddress);
			if (!pageMatch.Success)
			{
				return;
			}
			var page = pageMatch.Groups["page"].Value;
			var busOut = $"/{page}/busOutput";
			try
			{
				_oscClient.SendFloat(busOut, 1f);
				_oscBusOutputInitialized = true;
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Failed to initialize OSC bus output: {exception.Message}");
			}
		}

		private void StartOscReceiver()
		{
			try
			{
				_oscReceiver.Stop();
				_oscReceiver.StringMessageReceived -= OnOscReceivedString;
				_oscReceiver.FloatMessageReceived -= OnOscReceivedFloat;
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Failed to stop OSC receiver: {exception.Message}");
			}

			try
			{
				_oscReceiver.StringMessageReceived += OnOscReceivedString;
				_oscReceiver.FloatMessageReceived += OnOscReceivedFloat;
				_oscReceiver.Start(_oscReceivePort);
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Failed to start OSC receiver: {exception.Message}");
			}
		}

		private void OnOscReceivedString(string address, string? receivedString)
		{
			var volumeReceivedAddress = _oscAddress + "Val";
			if (!string.Equals(address, volumeReceivedAddress, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (!_osdEnabled)
			{
				return;
			}

			_volumeReceivedString = receivedString ?? string.Empty;
			ScheduleRender();
		}

		private void OnOscReceivedFloat(string address, float receivedValue)
		{
			if (!string.Equals(address, _oscAddress, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			_volumeReceivedValue = Math.Max(0f, Math.Min(1f, receivedValue));
			_volume = _volumeReceivedValue;

			if (!_osdEnabled)
			{
				return;
			}

			// Skip OSD while context menu is open to prevent UI deadlock
			if (_contextMenuOpen)
			{
				return;
			}

			if (InvokeRequired)
			{
				try
				{
					BeginInvoke(() =>
					{
						// Double-check menu state in UI thread context
						if (_contextMenuOpen)
						{
							return;
						}
						_osdForm.ShowBarWithText(_volumeReceivedValue, _volumeReceivedString, _osdDisplayTimeMs);
					});
				}
				catch (Exception exception)
				{
					Console.WriteLine($"Failed to invoke OSD update: {exception.Message}");
				}
			}
			else
			{
				_osdForm.ShowBarWithText(_volumeReceivedValue, _volumeReceivedString, _osdDisplayTimeMs);
			}
		}

		private void ScheduleRender()
		{
			if (InvokeRequired)
			{
				try
				{
					BeginInvoke(ScheduleRender);
				}
				catch (Exception exception)
				{
					Console.WriteLine($"Failed to invoke ScheduleRender: {exception.Message}");
				}
				return;
			}

			if (_renderTimer.Enabled)
			{
				return;
			}
			_renderTimer.Interval = RenderCoalesceMs;
			_renderTimer.Start();
		}

		private void RenderTimer_Tick()
		{
			_renderTimer.Stop();
			if (!_osdEnabled)
			{
				return;
			}
			// Skip OSD while context menu is open
			if (_contextMenuOpen)
			{
				return;
			}
			_osdForm.ShowBarWithText(_volumeReceivedValue, _volumeReceivedString, _osdDisplayTimeMs);
		}

		private void SendOscVolume(float value)
		{
			try
			{
				_oscClient.SendFloat(_oscAddress, value);
				_volumeReceivedValue = Math.Max(0f, Math.Min(1f, value));

				if (!_osdEnabled)
				{
					return;
				}

				// Skip OSD while context menu is open
				if (_contextMenuOpen)
				{
					return;
				}

				if (InvokeRequired)
				{
					try
					{
						BeginInvoke(() =>
						{
							if (_contextMenuOpen) return;
							_osdForm.ShowBarWithText(_volumeReceivedValue, _volumeReceivedString, _osdDisplayTimeMs);
						});
					}
					catch (Exception exception)
					{
						Console.WriteLine($"Failed to invoke OSD update: {exception.Message}");
					}
				}
				else
				{
					_osdForm.ShowBarWithText(_volumeReceivedValue, _volumeReceivedString, _osdDisplayTimeMs);
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Failed to send OSC message: {exception.Message}");
			}
		}

		private void ShowSetup(bool firstRun = false)
		{
			using var setupForm = new SetupForm(
				_oscIp, _oscSendPort, _oscReceivePort, _oscAddress,
				IniFile.ReadString(_configPath, SectionHotkeys, KeyVolumeUpHotkey, string.Empty),
				IniFile.ReadString(_configPath, SectionHotkeys, KeyVolumeDownHotkey, string.Empty),
				IniFile.ReadString(_configPath, SectionHotkeys, KeyVolumeMuteHotkey, string.Empty),
				_osdEnabled, _osdPosition, _osdMarginPreset,
				_osdDisplayTimeMs,
				_volumeStep,
				firstRun
			);

			if (setupForm.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			// Save to INI
			_oscIp = setupForm.OscIp;
			_oscSendPort = setupForm.OscPort;
			_oscReceivePort = setupForm.OscOutPort;
			_oscAddress = setupForm.OscAddress;
			_volumeStep = setupForm.VolumeStepValue;
			_osdEnabled = setupForm.OsdEnabled;
			_osdPosition = setupForm.OsdPosition;
			_osdMarginPreset = setupForm.OsdMarginPreset;
			_osdDisplayTimeMs = setupForm.OsdDisplayTimeMs;

			IniFile.WriteString(_configPath, SectionOsc, KeyIp, _oscIp);
			IniFile.WriteString(_configPath, SectionOsc, KeyPort, _oscSendPort.ToString());
			IniFile.WriteString(_configPath, SectionOsc, KeyOutPort, _oscReceivePort.ToString());
			IniFile.WriteString(_configPath, SectionOsc, KeyAddress, _oscAddress);
			IniFile.WriteString(_configPath, SectionHotkeys, KeyVolumeUpHotkey, setupForm.VolUpHotkey);
			IniFile.WriteString(_configPath, SectionHotkeys, KeyVolumeDownHotkey, setupForm.VolDownHotkey);
			IniFile.WriteString(_configPath, SectionHotkeys, KeyVolumeMuteHotkey, setupForm.VolMuteHotkey);
			IniFile.WriteString(_configPath, SectionVolume, KeyVolumeStep, _volumeStep.ToString(System.Globalization.CultureInfo.InvariantCulture));
			IniFile.WriteString(_configPath, SectionOsd, KeyOsdEnabled, _osdEnabled ? "1" : "0");
			IniFile.WriteString(_configPath, SectionOsd, KeyOsdPosition, _osdPosition);
			IniFile.WriteString(_configPath, SectionOsd, KeyOsdMarginPreset, _osdMarginPreset);
			IniFile.WriteString(_configPath, SectionOsd, KeyOsdDisplayTime, _osdDisplayTimeMs.ToString());
			IniFile.WriteString(_configPath, SectionSettings, KeyHideTrayIcon, _hideTrayIcon.ToString(System.Globalization.CultureInfo.InvariantCulture));

			// Reconnect + rebind
			_oscClient.Configure(_oscIp, _oscSendPort);
			_osdForm.ConfigureLayout(_osdPosition, _osdMarginPreset);
			if (firstRun)
			{
				StartOscReceiver();
				_oscBusOutputInitialized = false;
				InitializeOscBusOutput();
			}
			RegisterHotkeysFromIni();
		}

		protected override void OnClosing(CancelEventArgs eventArgs)
		{
			try
			{
				_hotkeyManager.UnregisterAll();
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Failed to unregister hotkeys: {exception.Message}");
			}
			base.OnClosing(eventArgs);
		}

		protected override void WndProc(ref Message message)
		{
			if (message.Msg == HotkeyManager.WM_HOTKEY)
			{
				_hotkeyManager.ProcessHotkey(message);
			}
			base.WndProc(ref message);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_notifyIcon.Dispose();
				_contextMenuStrip.Dispose();
				_oscClient.Dispose();
				_oscReceiver.Dispose();
				_osdForm.Dispose();
			}
			base.Dispose(disposing);
		}

        [GeneratedRegex("^/(?<page>\\d+)/", RegexOptions.CultureInvariant)]
        private static partial Regex MyRegex();
    }
}

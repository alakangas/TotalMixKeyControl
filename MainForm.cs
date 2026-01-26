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
		private string _volumeUpHotkey = string.Empty;
		private string _volumeDownHotkey = string.Empty;
		private string _volumeMuteHotkey = string.Empty;

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
					Log.Error("Failed to show context menu.", exception);
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
				Log.Error("Failed to load custom icon.", exception);
			}

			if (!File.Exists(_configPath))
			{
				try
				{
					ShowSetup(true);
				}
				catch (Exception exception)
				{
					Log.Error("Failed to show initial setup.", exception);
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
				var config = ConfigService.Load(_configPath);
				_oscIp = config.OscIp;
				_oscSendPort = config.OscSendPort;
				_oscReceivePort = config.OscReceivePort;
				_oscAddress = config.OscAddress;
				_volumeStep = config.VolumeStep;
				_hideTrayIcon = config.HideTrayIcon;
				_notifyIcon.Visible = _hideTrayIcon == 0;
				_osdDisplayTimeMs = config.OsdDisplayTimeMs;
				_osdEnabled = config.OsdEnabled;
				_osdPosition = config.OsdPosition;
				_osdMarginPreset = config.OsdMarginPreset;
				_volumeUpHotkey = config.VolumeUpHotkey;
				_volumeDownHotkey = config.VolumeDownHotkey;
				_volumeMuteHotkey = config.VolumeMuteHotkey;

				_osdForm.ConfigureLayout(_osdPosition, _osdMarginPreset);

				// Connect OSC
				_oscClient.Configure(_oscIp, _oscSendPort);
				StartOscReceiver();
				InitializeOscBusOutput();

				// Hotkeys
				RegisterHotkeys();
			}
			catch (Exception exception)
			{
				MessageBox.Show($"Failed to initialize: {exception.Message}", "TotalMix Key Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void RegisterHotkeys()
		{
			_hotkeyManager.UnregisterAll();

			if (HotkeyParser.TryParse(_volumeUpHotkey, out var parsedUpHotkey))
			{
				_hotkeyManager.Register(parsedUpHotkey.Modifiers, parsedUpHotkey.Key, VolumeUp);
			}
			if (HotkeyParser.TryParse(_volumeDownHotkey, out var parsedDownHotkey))
			{
				_hotkeyManager.Register(parsedDownHotkey.Modifiers, parsedDownHotkey.Key, VolumeDown);
			}
			if (HotkeyParser.TryParse(_volumeMuteHotkey, out var parsedMuteHotkey))
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
				Log.Error("Failed to initialize OSC bus output.", exception);
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
				Log.Error("Failed to stop OSC receiver.", exception);
			}

			try
			{
				_oscReceiver.StringMessageReceived += OnOscReceivedString;
				_oscReceiver.FloatMessageReceived += OnOscReceivedFloat;
				_oscReceiver.Start(_oscReceivePort);
			}
			catch (Exception exception)
			{
				Log.Error("Failed to start OSC receiver.", exception);
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

			ShowOsdIfEnabled();
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
					Log.Error("Failed to invoke ScheduleRender.", exception);
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
			ShowOsdIfEnabled();
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

				ShowOsdIfEnabled();
			}
			catch (Exception exception)
			{
				Log.Error("Failed to send OSC message.", exception);
			}
		}

		private void ShowOsdIfEnabled()
		{
			if (!_osdEnabled || _contextMenuOpen)
			{
				return;
			}

			if (InvokeRequired)
			{
				try
				{
					BeginInvoke(ShowOsdIfEnabled);
				}
				catch (Exception exception)
				{
					Log.Error("Failed to invoke OSD update.", exception);
				}
				return;
			}

			if (_contextMenuOpen)
			{
				return;
			}

			_osdForm.ShowBarWithText(_volumeReceivedValue, _volumeReceivedString, _osdDisplayTimeMs);
		}

		private void ShowSetup(bool firstRun = false)
		{
			using var setupForm = new SetupForm(
				_oscIp, _oscSendPort, _oscReceivePort, _oscAddress,
				_volumeUpHotkey,
				_volumeDownHotkey,
				_volumeMuteHotkey,
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
			_volumeUpHotkey = setupForm.VolUpHotkey;
			_volumeDownHotkey = setupForm.VolDownHotkey;
			_volumeMuteHotkey = setupForm.VolMuteHotkey;

			ConfigService.Save(_configPath, new AppConfig
			{
				OscIp = _oscIp,
				OscSendPort = _oscSendPort,
				OscReceivePort = _oscReceivePort,
				OscAddress = _oscAddress,
				VolumeStep = _volumeStep,
				OsdEnabled = _osdEnabled,
				OsdPosition = _osdPosition,
				OsdMarginPreset = _osdMarginPreset,
				OsdDisplayTimeMs = _osdDisplayTimeMs,
				HideTrayIcon = _hideTrayIcon,
				VolumeUpHotkey = _volumeUpHotkey,
				VolumeDownHotkey = _volumeDownHotkey,
				VolumeMuteHotkey = _volumeMuteHotkey
			});

			// Reconnect + rebind
			_oscClient.Configure(_oscIp, _oscSendPort);
			_osdForm.ConfigureLayout(_osdPosition, _osdMarginPreset);
			if (firstRun)
			{
				StartOscReceiver();
				_oscBusOutputInitialized = false;
				InitializeOscBusOutput();
			}
			RegisterHotkeys();
		}

		protected override void OnClosing(CancelEventArgs eventArgs)
		{
			try
			{
				_hotkeyManager.UnregisterAll();
			}
			catch (Exception exception)
			{
				Log.Error("Failed to unregister hotkeys.", exception);
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

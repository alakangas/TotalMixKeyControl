using System;
using System.Drawing;
using System.Windows.Forms;

namespace TotalMixKeyControl
{
    internal class SetupForm : Form
    {
        private TextBox _ipBox = null!;
    private NumericUpDown _portBox = null!;      // incoming to TotalMix (we send to this)
    private NumericUpDown _outPortBox = null!;   // outgoing from TotalMix (we receive from this)
        private TextBox _addrBox = null!;

        private TextBox _hkUp = null!;
        private TextBox _hkDown = null!;
        private TextBox _hkMute = null!;

    // OSD controls
    private CheckBox _osdEnabled = null!;
    private ComboBox _osdPosition = null!;
    private ComboBox _osdMarginPreset = null!;
    private NumericUpDown _osdDisplayTime = null!; // ms

    // Volume controls
    private ComboBox _volStepSpeed = null!; // 1-4 factor over 0.01f

    // Startup
    private CheckBox _runOnStartup = null!;

        public string OscIp => _ipBox.Text.Trim();
    public int OscPort => (int)_portBox.Value;
    public int OscOutPort => (int)_outPortBox.Value;
        public string OscAddress => _addrBox.Text.Trim();

        public string VolUpHotkey => _hkUp.Text.Trim();
        public string VolDownHotkey => _hkDown.Text.Trim();
        public string VolMuteHotkey => _hkMute.Text.Trim();

        public float VolumeStepValue
        {
            get
            {
                int factor = 1;
                if (int.TryParse(_volStepSpeed.SelectedItem?.ToString(), out var f) && f >= 1 && f <= 4)
                    factor = f;
                return 0.01f * factor;
            }
        }

        public bool OsdEnabled => _osdEnabled.Checked;
        public string OsdPosition => _osdPosition.SelectedItem?.ToString() ?? "BottomCenter";
        public string OsdMarginPreset => _osdMarginPreset.SelectedItem?.ToString() ?? "Small";
        public int OsdDisplayTimeMs => (int)_osdDisplayTime.Value;
        public bool RunOnStartup => _runOnStartup.Checked;

        public SetupForm(string ip, int port, int outPort, string address,
            string hkUp, string hkDown, string hkMute,
            bool osdEnabled, string osdPosition, string osdMarginPreset,
            int osdDisplayTimeMs,
            float volumeStep,
            bool runOnStartup,
            bool firstRun = false)
        {
            Text = "TotalMix Key Control Setup";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(480, 660);

            var lblTitle = new Label { Text = "TotalMix Key Control Setup", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(130, 15), Size = new Size(220, 20) };

            var lblUp = MakeLabel("Volume Up Hotkey", 30, 70);
            _hkUp = MakeTextBox(220, 70, 170, 20, hkUp);

            var lblDown = MakeLabel("Volume Down Hotkey", 30, 110);
            _hkDown = MakeTextBox(220, 110, 170, 20, hkDown);

            var lblMute = MakeLabel("Volume Mute Hotkey", 30, 150);
            _hkMute = MakeTextBox(220, 150, 170, 20, hkMute);

            var lblIp = MakeLabel("TotalMix FX OSC IP", 30, 190);
            _ipBox = MakeTextBox(220, 190, 200, 20, ip);

            var lblPort = MakeLabel("TotalMix FX OSC Port (incoming)", 30, 230);
            _portBox = MakeNumeric(220, 230, 200, 20, 1, 65535, port);

            var lblOutPort = MakeLabel("TotalMix FX OSC Port (outgoing)", 30, 270);
            _outPortBox = MakeNumeric(220, 270, 200, 20, 1, 65535, outPort);

            // Volume Step Speed
            var lblStep = MakeLabel("Volume Step Speed", 30, 350);
            _volStepSpeed = MakeCombo(220, 350, 200, 22);
            _volStepSpeed.Items.AddRange(new object[] { "1", "2", "3", "4" });
            // infer selected from current volumeStep (0.01f * factor)
            var factor = Math.Max(1, Math.Min(4, (int)Math.Round(volumeStep / 0.01f)));
            _volStepSpeed.SelectedItem = factor.ToString();

            // OSD group
            var grpOsd = new GroupBox { Text = "OSD", Location = new Point(20, 390), Size = new Size(440, 160) };
            _osdEnabled = new CheckBox { Text = "Enable OSD", Location = new Point(16, 28), Size = new Size(160, 22), Checked = osdEnabled };
            var lblPos = MakeLabel("Position", 16, 62, 140, 20);
            _osdPosition = MakeCombo(160, 60, 240, 22);
            _osdPosition.Items.AddRange(new object[] { "TopLeft", "TopCenter", "TopRight", "BottomLeft", "BottomCenter", "BottomRight" });
            _osdPosition.SelectedItem = (object?)(Array.Exists(new[]{"TopLeft","TopCenter","TopRight","BottomLeft","BottomCenter","BottomRight"}, x => string.Equals(x, osdPosition, StringComparison.OrdinalIgnoreCase)) ? osdPosition : "BottomCenter");
            var lblMargin = MakeLabel("Position Margin", 16, 96, 140, 20);
            _osdMarginPreset = MakeCombo(160, 94, 240, 22);
            _osdMarginPreset.Items.AddRange(new object[] { "None", "Small", "Medium", "Large" });
            _osdMarginPreset.SelectedItem = (object?)(Array.Exists(new[]{"None","Small","Medium","Large"}, x => string.Equals(x, osdMarginPreset, StringComparison.OrdinalIgnoreCase)) ? osdMarginPreset : "Small");
            var lblTime = MakeLabel("Display Time (ms)", 16, 128, 140, 20);
            _osdDisplayTime = MakeNumeric(160, 126, 120, 22, 500, 20000, Math.Max(500, Math.Min(20000, osdDisplayTimeMs <= 0 ? 2500 : osdDisplayTimeMs)));
            _osdDisplayTime.Increment = 100;
            grpOsd.Controls.AddRange(new Control[] { _osdEnabled, lblPos, _osdPosition, lblMargin, _osdMarginPreset, lblTime, _osdDisplayTime });

            var lblAddr = MakeLabel("OSC Address", 30, 310);
            _addrBox = MakeTextBox(220, 310, 200, 20, address);

            _runOnStartup = new CheckBox { Text = "Run on Windows startup", Location = new Point(30, 562), Size = new Size(200, 22), Checked = runOnStartup };

            var btnOk = new Button { Text = "OK", Location = new Point(272, 610), Size = new Size(110, 30) };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(62, 610), Size = new Size(100, 30) };

            btnOk.Click += (_, __) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            // First-run: app can't run without config; remove cancel/close affordances
            if (firstRun)
            {
                btnCancel.Enabled = false;
                btnCancel.Visible = false;
                ControlBox = false; // hide window close button (X)

                // Center the OK button when Cancel is hidden
                var centeredX = (ClientSize.Width - btnOk.Width) / 2;
                btnOk.Location = new Point(centeredX, btnOk.Location.Y);
            }
            else
            {
                // Center the pair (Cancel, OK) with a consistent gap when both are visible
                int gap = 16;
                int total = btnCancel.Width + gap + btnOk.Width;
                int startX = (ClientSize.Width - total) / 2;
                btnCancel.Location = new Point(startX, btnCancel.Location.Y);
                btnOk.Location = new Point(startX + btnCancel.Width + gap, btnOk.Location.Y);
            }

            Controls.AddRange(new Control[]
            {
                lblTitle,
                lblUp, _hkUp,
                lblDown, _hkDown,
                lblMute, _hkMute,
                lblIp, _ipBox,
                lblPort, _portBox,
                lblOutPort, _outPortBox,
                lblAddr, _addrBox,
                lblStep, _volStepSpeed,
                grpOsd,
                _runOnStartup,
                btnOk, btnCancel
            });

            // Wire hotkey capture after controls exist, so we can chain focus
            WireHotkeyCapture(_hkUp, _hkDown);
            WireHotkeyCapture(_hkDown, _hkMute);
            WireHotkeyCapture(_hkMute, null);

            // Focus first hotkey after the form is shown (handle is created)
            Shown += (_, __) =>
            {
                try
                {
                    _hkUp.Focus();
                }
                catch { }
            };
        }

        private static void WireHotkeyCapture(TextBox tb, TextBox? next)
        {
            tb.ReadOnly = true;
            const string Waiting = "Waiting for input";

            // Show placeholder when focused and empty
            tb.Enter += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Tag = "placeholder";
                    tb.ForeColor = SystemColors.GrayText;
                    tb.Text = Waiting;
                }
            };

            // Remove placeholder on leave
            tb.Leave += (s, e) =>
            {
                if (Equals(tb.Tag, "placeholder"))
                {
                    tb.Tag = null;
                    tb.ForeColor = SystemColors.WindowText;
                    tb.Clear();
                }
            };

            tb.KeyDown += (s, e) =>
            {
                e.SuppressKeyPress = true;
                var parts = new System.Collections.Generic.List<string>();
                if (e.Control) parts.Add("Ctrl");
                if (e.Alt) parts.Add("Alt");
                if (e.Shift) parts.Add("Shift");
                // Windows key capture is tricky; ignoring here intentionally to avoid conflicts

                var key = e.KeyCode;
                if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu)
                {
                    tb.Text = string.Join("+", parts);
                    return;
                }
                parts.Add(key.ToString());
                tb.Tag = null;
                tb.ForeColor = SystemColors.WindowText;
                tb.Text = string.Join("+", parts);

                // Move focus to next field if provided
                next?.Focus();
            };
        }

        private static Label MakeLabel(string text, int x, int y, int width = 180, int height = 20)
        {
            return new Label { Text = text, Location = new Point(x, y), Size = new Size(width, height) };
        }

        private static TextBox MakeTextBox(int x, int y, int width, int height, string text)
        {
            return new TextBox { Location = new Point(x, y), Size = new Size(width, height), Text = text };
        }

        private static NumericUpDown MakeNumeric(int x, int y, int width, int height, int min, int max, int value)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                Minimum = min,
                Maximum = max,
                Value = value
            };
        }

        private static ComboBox MakeCombo(int x, int y, int width, int height)
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(x, y),
                Size = new Size(width, height)
            };
        }
    }
}

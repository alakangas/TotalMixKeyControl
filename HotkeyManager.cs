using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TotalMixKeyControl
{
	internal class HotkeyManager
	{
		public const int WM_HOTKEY = 0x0312;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		[Flags]
		public enum Modifiers : uint
		{
			None = 0x0000,
			Alt = 0x0001,
			Control = 0x0002,
			Shift = 0x0004,
			Win = 0x0008,
		}

		private readonly Form _window;
		private int _nextId = 1;
		private readonly Dictionary<int, Action> _actions = new();

		public HotkeyManager(Form host)
		{
			_window = host;
			_window.HandleCreated += (_, __) => { };
			_window.HandleDestroyed += (_, __) => UnregisterAll();
		}

		public bool Register(Modifiers modifiers, Keys key, Action action)
		{
			if (key == Keys.None || action == null)
				return false;
			int id = _nextId++;
			if (!RegisterHotKey(_window.Handle, id, (uint)modifiers, (uint)key))
				return false;
			_actions[id] = action;
			return true;
		}

		public void UnregisterAll()
		{
			foreach (var id in new List<int>(_actions.Keys))
			{
				try { UnregisterHotKey(_window.Handle, id); } catch { }
				_actions.Remove(id);
			}
		}

		public void ProcessHotkey(Message m)
		{
			if (m.Msg != WM_HOTKEY) return;
			int id = m.WParam.ToInt32();
			if (_actions.TryGetValue(id, out var action))
			{
				try { action(); } catch { }
			}
		}
	}
}

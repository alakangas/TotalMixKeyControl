using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TotalMixKeyControl
{
	internal static class HotkeyParser
	{
		public struct ParsedHotkey
		{
			public HotkeyManager.Modifiers Modifiers;
			public Keys Key;
		}

		private static readonly Dictionary<string, Keys> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
		{
			{"PgUp", Keys.PageUp},
			{"PgDn", Keys.PageDown},
			{"NumpadAdd", Keys.Add},
			{"NumpadSub", Keys.Subtract},
			{"NumpadSubtract", Keys.Subtract},
			{"NumpadEnter", Keys.Enter},
			{"NumpadMult", Keys.Multiply},
			{"NumpadDiv", Keys.Divide},
			{"Esc", Keys.Escape},
			{"Win", Keys.LWin},
		};

		public static bool TryParse(string? text, out ParsedHotkey result)
		{
			result = new ParsedHotkey { Modifiers = HotkeyManager.Modifiers.None, Key = Keys.None };
			if (string.IsNullOrWhiteSpace(text)) return false;
			text = text.Trim();

			// AHK-style prefix ^ ! + #
			if (text.Any(c => c == '^' || c == '!' || c == '+' || c == '#'))
			{
				var mods = HotkeyManager.Modifiers.None;
				int i = 0;
				while (i < text.Length && (text[i] == '^' || text[i] == '!' || text[i] == '+' || text[i] == '#'))
				{
					mods |= text[i] switch
					{
						'^' => HotkeyManager.Modifiers.Control,
						'!' => HotkeyManager.Modifiers.Alt,
						'+' => HotkeyManager.Modifiers.Shift,
						'#' => HotkeyManager.Modifiers.Win,
						_ => HotkeyManager.Modifiers.None
					};
					i++;
				}
				var keyToken = text[i..].Trim();
				if (TryParseKey(keyToken, out var key))
				{
					result = new ParsedHotkey { Modifiers = mods, Key = key };
					return true;
				}
				return false;
			}

			// Textual format: Ctrl+Alt+K, etc.
			var parts = text.Split(new[] {'+'}, StringSplitOptions.RemoveEmptyEntries)
							.Select(p => p.Trim()).ToArray();
			if (parts.Length == 0) return false;

			var m = HotkeyManager.Modifiers.None;
			Keys k = Keys.None;

			foreach (var p in parts)
			{
				if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase))
					m |= HotkeyManager.Modifiers.Control;
				else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase))
					m |= HotkeyManager.Modifiers.Alt;
				else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase))
					m |= HotkeyManager.Modifiers.Shift;
				else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase) || p.Equals("Windows", StringComparison.OrdinalIgnoreCase))
					m |= HotkeyManager.Modifiers.Win;
				else
				{
					if (!TryParseKey(p, out k))
						return false;
				}
			}

			if (k == Keys.None) return false;
			result = new ParsedHotkey { Modifiers = m, Key = k };
			return true;
		}

		private static bool TryParseKey(string token, out Keys key)
		{
			// Aliases first
			if (KeyAliases.TryGetValue(token, out key)) return true;

			// Single letter/number
			if (token.Length == 1)
			{
				char c = char.ToUpperInvariant(token[0]);
				if (c >= 'A' && c <= 'Z') { key = (Keys)c; return true; }
				if (c >= '0' && c <= '9') { key = (Keys)c; return true; }
			}

			// Try enum parse for F-keys etc.
			if (Enum.TryParse<Keys>(token, true, out key))
				return true;

			return false;
		}
	}
}

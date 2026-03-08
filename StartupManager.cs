using Microsoft.Win32;

namespace TotalMixKeyControl
{
	internal static class StartupManager
	{
		private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private const string AppName = "TotalMixKeyControl";

		public static bool IsEnabled()
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
				return key?.GetValue(AppName) != null;
			}
			catch (Exception ex)
			{
				Log.Error("Failed to read startup registry key.", ex);
				return false;
			}
		}

		public static void SetEnabled(bool enabled)
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
				if (key == null)
					return;

				if (enabled)
				{
					var exePath = Environment.ProcessPath;
					if (!string.IsNullOrEmpty(exePath))
						key.SetValue(AppName, $"\"{exePath}\"");
				}
				else
				{
					key.DeleteValue(AppName, false);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Failed to update startup registry key.", ex);
			}
		}
	}
}

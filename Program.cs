using System;
using System.IO;
using System.Windows.Forms;
using Velopack;

namespace TotalMixKeyControl
{
	internal static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			VelopackApp.Build().Run();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var appDataDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"TotalMixKeyControl");
			Directory.CreateDirectory(appDataDir);

			var configArg = (args is { Length: > 0 }) ? args[0] : "config.ini";
			var configPath = Path.IsPathRooted(configArg)
				? configArg
				: Path.Combine(appDataDir, configArg);

			Application.Run(new MainForm(configPath));
		}
	}
}

using System;
using System.IO;
using System.Windows.Forms;

namespace TotalMixKeyControl
{
	internal static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			var baseDir = AppContext.BaseDirectory;
			var configArg = (args != null && args.Length > 0) ? args[0] : "config.ini";
			var configPath = Path.IsPathRooted(configArg) ? configArg : Path.Combine(baseDir, configArg);
			Application.Run(new MainForm(configPath));
		}
	}
}

using System.Diagnostics;

namespace TotalMixKeyControl
{
	internal static class Log
	{
		public static void Info(string message)
		{
			Trace.TraceInformation(message);
			Debug.WriteLine(message);
		}

		public static void Error(string message)
		{
			Trace.TraceError(message);
			Debug.WriteLine(message);
		}

		public static void Error(string message, Exception exception)
		{
			Trace.TraceError($"{message} Exception: {exception}");
			Debug.WriteLine($"{message} Exception: {exception}");
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace RedditRetweeter
{
	public class Logger : ILogger
	{
		private readonly List<string> Logging = new List<string>();
		private readonly string LogPrefix = $"[{DateTime.Now}]: ";

		public void Message (string message)
		{
			WriteToConsole(message);
		}

		public void Info(string message)
		{
			var msg = LogPrefix + message;
			WriteToConsole(msg);
			Logging.Add(msg);
		}

		private void WriteToConsole(string message)
		{
			Console.WriteLine(message);
		}

		public IEnumerable<string> GetLogs()
		{
			return Logging;
		}
	}
}

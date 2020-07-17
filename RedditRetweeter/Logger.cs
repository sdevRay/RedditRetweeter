using System;
using System.Collections.Generic;
using System.Text;

namespace RedditRetweeter
{
	public class Logger : ILogger
	{
		private readonly List<string> Logging = new List<string>();

		public void Message (string message)
		{
			WriteToConsole(message);
		}

		public void Info(string message)
		{
			WriteToConsole(message);
			var msg = $"[{DateTime.Now}]: {message}"; 
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

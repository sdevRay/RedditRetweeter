using System.Collections.Generic;

namespace RedditRetweeter
{
	public interface ILogger
	{
		IEnumerable<string> GetLogs();
		void Message(string message);
		void Info(string message);
	}
}

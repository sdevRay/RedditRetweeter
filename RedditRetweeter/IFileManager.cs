using System.Collections.Generic;

namespace RedditRetweeter
{
	public interface IFileManager
	{
		T ReadFile<T>(string filePath);
		void SaveFile<T>(IEnumerable<T> details, string filePath, bool initial = false);
	}
}

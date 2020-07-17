using System.Collections.Generic;

namespace RedditRetweeter
{
	public interface IFileManager
	{
		void Delete(string filePath);
		T ReadFile<T>(string filePath);
		void SaveFile<T>(IEnumerable<T> details, string filePath, bool initial = false);
	}
}

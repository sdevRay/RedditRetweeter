using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RedditRetweeter
{
	public class FileManager : IFileManager
	{
		private readonly ILogger _logger;

		public FileManager(ILogger logger)
		{
			_logger = logger;
		}

		public T ReadFile<T>(string filePath) 
		{
			try
			{
				var serializer = new JsonSerializer();
				using var sr = new StreamReader(filePath);
				using var jsonTextReader = new JsonTextReader(sr);
				return serializer.Deserialize<T>(jsonTextReader);
			}
			catch (FileNotFoundException ex)
			{
				throw ex;
			}
		}

		public void SaveFile<T>(IEnumerable<T> details, string filePath, bool initial = false)
		{
			if (initial)
			{
				if (File.Exists(filePath))
					File.Delete(filePath);
			}

			using StreamWriter file = new StreamWriter(filePath, false);
			var serializer = new JsonSerializer();
			serializer.Serialize(file, details);
		}

		public void Delete(string filePath)
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}
	}
}

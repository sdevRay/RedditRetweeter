namespace RedditRetweeter
{
	class Program
	{
		static void Main(string[] args)
		{
			var logger = new Logger();
			new Startup(new FileManager(logger), logger);
		}
	}
}

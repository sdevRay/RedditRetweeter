using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace RedditRetweeter
{
	public class Startup
	{
		private static Timer aTimer;
		private RedditFetcher _reddit;
		private TwitterPoster _twitter;
		private const string _filePath = @"reddit_post.json";
		private const string _trendFilePath = @"trends.json";
		private int _limit;
		private SubredditNames _sub;
		private TimeFrame _timeframe;
		private int _interval;
		private readonly IFileManager _fileManager;
		private readonly ILogger _logger;
		private const int MINUTE_MILLISECOND_CONVERSION = 60000;
		private bool _appenTrend = false;

		public Startup(IFileManager fileManager, ILogger logger)
		{
			_fileManager = fileManager;
			_logger = logger;

			_logger.Message("Reddit Retweeter v0.5");
			_logger.Message("Pulls Reddit posts then Tweets them on interval\n");

			GetUserInput();
			Initialize();
		}

		private void GetUserInput()
		{
			try
			{
				SelectPurge();
				_sub = SelectSubreddit();
				_limit = SelectFetchLimit();
				_timeframe = SelectTimeFrame();
				_interval = SelectTimeInterval();
				SelectAppendTrend();
			}
			catch (FormatException ex)
			{
				_logger.Message(ex.Message + "Retrying user input\n");
				GetUserInput();
			}
		}

		private void SelectPurge()
		{
			_logger.Message("Purge Twitter timeline? [Y/N]");
			var line = Console.ReadLine();
			if (line[0] == 'y' || line[0] == 'Y')
			{
				PurgeTwitterTimeline();
				Exit();
			}
		}

		private void PurgeTwitterTimeline() // Run this to erase timeline and start fresh
		{
			_twitter = new TwitterPoster(_logger, _fileManager, _trendFilePath, _appenTrend);
			_twitter.BulkDelete();
		}

		private void SelectAppendTrend()
		{
			_logger.Message("Append trending hashtags to Twitter post? [Y/N]");
			var line = Console.ReadLine();
			if (line[0] == 'y' || line[0] == 'Y')
				_appenTrend = true;
			else if (line[0] == 'n' || line[0] == 'N')
				_appenTrend = false;
			else
				SelectAppendTrend();
		}

		private SubredditNames SelectSubreddit()
		{
			_logger.Message("Select Subreddit");
			var jsNames = Enum.GetNames(typeof(SubredditNames));
			var count = 0;
			foreach (var js in jsNames)
			{
				_logger.Message(count + " " + js);
				count++;
			}
			int sub = Convert.ToInt32(Console.ReadLine());
			if (sub > (count - 1) || sub < 0)
			{
				_logger.Message("Incorrect selection\n");
				SelectSubreddit();
			}

			return (SubredditNames)sub;
		}
		private TimeFrame SelectTimeFrame()
		{
			_logger.Message("How far back to collect posts from Reddit?");
			var tfNames = Enum.GetNames(typeof(TimeFrame));
			var count = 0;
			foreach (var tf in tfNames)
			{
				_logger.Message(count + " " + tf);
				count++;
			}
			int timeframe = Convert.ToInt32(Console.ReadLine());
			if (timeframe >= count || timeframe < 0)
			{
				_logger.Message("Incorrect selection\n");
				SelectTimeFrame();
			}

			return (TimeFrame)timeframe;
		}

		private int SelectFetchLimit()
		{
			_logger.Message("How many posts to pull from Reddit [1 - 100]");
			int limit = Convert.ToInt32(Console.ReadLine());
			if (limit > 100 || limit < 1)
			{
				_logger.Message("Must be [1 - 100] posts\n");
				SelectFetchLimit();
			}

			return limit;
		}

		private int SelectTimeInterval()
		{
			_logger.Message("Time interval in minutes to post to Twitter? (First post will be immediate)");
			int interval = Convert.ToInt32(Console.ReadLine());
			if (interval > 720 || interval < 1)
			{
				_logger.Message("Incorrect selection, must be between [1 - 720 mins]\n");
				SelectTimeInterval();
			}

			return interval * MINUTE_MILLISECOND_CONVERSION;
		}

		private void Initialize()
		{
			try
			{
				_reddit = new RedditFetcher(_logger);
				_twitter = new TwitterPoster(_logger, _fileManager, _trendFilePath, _appenTrend);

				var posts = _reddit.GetTopPosts(_reddit.GetSubreddit(_sub), _timeframe, _limit);
				_fileManager.SaveFile(posts, _filePath, true);
				_logger.Message("\nSuccessfully setup configuration\n");
			}
			catch (Exception ex)
			{
				_logger.Info("Error: " + ex.Message);
				_logger.Info(ex.StackTrace);
				Exit();
			}

			Process(DateTime.Now);
			StartTimer();
		}

		private void Process(DateTime signalTime)
		{
			_logger.Info($"Process started at {signalTime}");
			_logger.Message("--------------------------------");

			try
			{
				bool success = false;
				var postDetailsFromFile = _fileManager.ReadFile<IEnumerable<PostDetail>>(_filePath).ToList();
				_logger.Message($"Found {postDetailsFromFile.Count()} posts from {_filePath}");
				var postDetail = postDetailsFromFile.FirstOrDefault();

				if (postDetail != null)
					success = _twitter.ProcessTweet(postDetail);
				else
					Exit();

				_logger.Message($"Removing Id: {postDetail.Id} from {_filePath}");
				postDetailsFromFile.Remove(postDetail);
				_fileManager.SaveFile(postDetailsFromFile, _filePath);
				if (success)
					_logger.Message($"\nWaiting {_interval / MINUTE_MILLISECOND_CONVERSION} minute(s) for next iteration..");
				else
					Process(DateTime.Now);

			}
			catch (Exception ex)
			{
				_logger.Message("Error: " + ex.Message);
			}
		}

		private void StartTimer()
		{
			_logger.Message("Starting timer");

			aTimer = new Timer
			{
				Interval = _interval
			};

			aTimer.Elapsed += OnTimedEvent;
			aTimer.AutoReset = true;
			aTimer.Enabled = true;

			_logger.Message($"Executing every {_interval / MINUTE_MILLISECOND_CONVERSION} minute(s). Press Enter to stop at anytime\n");

			Console.ReadLine();
			Exit();
		}

		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			Process(e.SignalTime);
		}

		private void Exit()
		{
			var files = new string[] { _filePath, _trendFilePath };

			_logger.Message("Exiting..");
			if (aTimer != null)
			{
				aTimer.Stop();
				aTimer.Dispose();
			}

			foreach(var file in files)
			{
				_fileManager.Delete(file);
			}

			_fileManager.SaveFile(_logger.GetLogs(), "logs.txt", true);
			_logger.Message("Press Enter to Exit..\n");
			Console.ReadLine();
			Environment.Exit(0);
		}
	}
}

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
		private int _limit;
		private SubredditNames _sub;
		private TimeFrame _timeframe;
		private int _interval;
		private readonly IFileManager _fileManager;
		private readonly ILogger _logger;
		private const int MINUTE_MILLISECOND_CONVERSION = 60000;

		public Startup(IFileManager fileManager, ILogger logger)
		{
			_fileManager = fileManager;
			_logger = logger;

			_logger.Message("Reddit Retweeter v0.5");
			_logger.Message("Pulls Reddit posts then Tweets them on interval\n");

			//PurgeTwitterTimeline();

			GetUserInput();
			Initialize();
		}

		private void PurgeTwitterTimeline() // Run this to erase timeline and start fresh
		{
			_twitter = new TwitterPoster(_logger, _fileManager);
			_twitter.BulkDelete();
		}

		private void GetUserInput()
		{
			try
			{
				_sub = SelectSubreddit();
				_limit = SelectFetchLimit();
				_timeframe = SelectTimeFrame();
				_interval = SelectTimeInterval();
			}
			catch (FormatException ex)
			{
				_logger.Message(ex.Message + "Retrying user input\n");
				GetUserInput();
			}
		}

		public SubredditNames SelectSubreddit()
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
		public TimeFrame SelectTimeFrame()
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

		public int SelectFetchLimit()
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

		public int SelectTimeInterval()
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
				_twitter = new TwitterPoster(_logger, _fileManager);

				var posts = _reddit.GetTopPosts(_reddit.GetSubreddit(_sub), _timeframe, _limit);
				_fileManager.SaveFile(posts, _filePath, true);
				_logger.Message("Successfully setup configuration\n");
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
				var postDetailsFromFile = _fileManager.ReadFile<IEnumerable<PostDetail>>(_filePath).ToList();
				_logger.Message($"Found {postDetailsFromFile.Count()} posts from {_filePath}");
				var postDetail = postDetailsFromFile.FirstOrDefault();

				if (postDetail != null)
					_twitter.ProcessTweet(postDetail);
				else
					Exit();

				_logger.Message($"Removing Id: {postDetail.Id} from {_filePath}");
				postDetailsFromFile.Remove(postDetail);

				_fileManager.SaveFile(postDetailsFromFile, _filePath);
				_logger.Message($"\nWaiting {_interval / MINUTE_MILLISECOND_CONVERSION} minute(s) for next iteration..");
			}
			catch (Exception ex)
			{
				_logger.Message("Error: " + ex.Message);
			}
		}

		private void StartTimer()
		{
			ConsoleKeyInfo input;
			do
			{
				aTimer = new Timer
				{
					Interval = _interval
				};

				aTimer.Elapsed += OnTimedEvent;
				aTimer.AutoReset = true;
				aTimer.Enabled = true;

				_logger.Message($"Executing every {_interval / MINUTE_MILLISECOND_CONVERSION} minute(s). Press ESC to exit at anytime\n");
				input = Console.ReadKey();
			} while (input.Key != ConsoleKey.Escape || input.Key != ConsoleKey.Enter);
			
			Exit();
		}

		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			Process(e.SignalTime);
		}

		private void Exit()
		{
			_logger.Message("Exiting..");
			if(aTimer != null)
			{
				aTimer.Stop();
				aTimer.Dispose();
			}

			_fileManager.SaveFile(_logger.GetLogs(), "logs.txt", true);
			Environment.Exit(0);
		}
	}
}

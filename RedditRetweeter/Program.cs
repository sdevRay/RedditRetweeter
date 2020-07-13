using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace RedditRetweeter
{
	public class Program
	{
		private static Timer aTimer;
		private RedditFetcher _reddit;
		private TwitterPoster _twitter;
		private const string _filePath = @"reddit_post.json";
		private const int MINUTE_MILLISECOND_CONVERSION = 60000;
		private int _limit;
		private SubredditNames _sub;
		private TimeFrame _timeframe;
		private int _interval;

		public Program()
		{
			Console.WriteLine("RedditRetweeter v0.3");
			Console.WriteLine("Pulls Reddit posts, saves content to file");
			Console.WriteLine("Retrieves data from file and tweets it on interval\n");

			GetUserInput();
			Initialize();

			//PurgeTwitterTimeline();
		}

		private void PurgeTwitterTimeline() // Run this to erase timeline and start fresh
		{
			_twitter = new TwitterPoster();
			_twitter.BulkDelete();
		}

		private SubredditNames SelectSubreddit()
		{
			Console.WriteLine("Select Subreddit\n");
			var jsNames = Enum.GetNames(typeof(SubredditNames));
			var count = 0;
			foreach (var js in jsNames)
			{
				Console.WriteLine(count + " " + js);
				count++;
			}
			int sub = Convert.ToInt32(Console.ReadLine());
			if (sub > count || sub < 0)
			{
				Console.WriteLine("\nIncorrect selection\n");
				SelectSubreddit();
			}

			return (SubredditNames)sub;
		}

		private int SelectFetchLimit()
		{
			Console.WriteLine("\nChose post fetch limit (1 - 100) (How many posts to scrape from Reddit)");
			int limit = Convert.ToInt32(Console.ReadLine());
			if (limit > 100 || limit < 1)
			{
				Console.WriteLine("\nIncorrect selection\n");
				SelectFetchLimit();
			}

			return limit;
		}

		private TimeFrame SelectTimeFrame()
		{
			Console.WriteLine("\nSelect Timeframe (How for to aggregate posts from Reddit)");
			var tfNames = Enum.GetNames(typeof(TimeFrame));
			var count = 0;
			foreach (var tf in tfNames)
			{
				Console.WriteLine(count + " " + tf);
				count++;
			}
			int timeframe = Convert.ToInt32(Console.ReadLine());
			if (timeframe >= count || timeframe < 0)
			{
				Console.WriteLine("\nIncorrect selection\n");
				SelectTimeFrame();
			}

			return (TimeFrame)timeframe;
		}

		public int SelectTimeInterval()
		{
			Console.WriteLine("\nChose time interval in minutes (1 - 720 Minutes (12hrs))\nThis will be how often to post to Twitter (First post will be immediate)\n");
			int interval = Convert.ToInt32(Console.ReadLine());
			if (interval > 720 || interval < 1)
			{
				Console.WriteLine("\nIncorrect selection\n");
				SelectTimeInterval();
			}

			return interval * MINUTE_MILLISECOND_CONVERSION; 
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
				Console.WriteLine("\n" + ex.Message + "\nRetrying user input\n");
				GetUserInput();
			}
		}

		private void Initialize()
		{
			try
			{
				_reddit = new RedditFetcher();
				_twitter = new TwitterPoster();
				var posts = _reddit.GetTopPosts(_reddit.GetSubreddit(_sub), _timeframe, _limit).ToList();
				Console.WriteLine(Environment.NewLine);
				SaveFile(posts);
				Console.WriteLine("Success\n");
			}
			catch (Exception ex)
			{
				Console.WriteLine("\nError: " + ex.Message);
				Console.WriteLine(ex.StackTrace);
				Exit();
			}

			Process(DateTime.Now);
			StartTimer(_interval);
		}

		private void Process(DateTime signalTime)
		{
			Console.WriteLine($"Process started at {signalTime}");
			Console.WriteLine("--------------------------------");

			try
			{
				var postDetailsFromFile = ReadFile(_filePath).ToList();
				Console.WriteLine($"Found {postDetailsFromFile.Count()} Reddit posts from {_filePath}\n");
				var postDetail = postDetailsFromFile.FirstOrDefault();
				if (postDetail != null)
					_twitter.ProcessTweet(postDetail);
				else
				{
					Console.WriteLine("Unable to parse Reddit post\n");
					Exit();
				}
				
				
				Console.WriteLine($"Removing Id: {postDetail.Id} from {_filePath}\n");
				postDetailsFromFile.Remove(postDetail);

				SaveFile(postDetailsFromFile, false);
				Console.WriteLine("Waiting for next iteration..");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
		}

		private void StartTimer(int interval)
		{
			aTimer = new Timer
			{
				Interval = interval
			};

			aTimer.Elapsed += OnTimedEvent;
			aTimer.AutoReset = true;
			aTimer.Enabled = true;

			Console.WriteLine($"\nExecuting every {_interval / MINUTE_MILLISECOND_CONVERSION} minute(s). Press Enter to exit at anytime\n");
			Console.ReadLine();
		}

		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			Process(e.SignalTime);
		}

		private IEnumerable<PostDetail> ReadFile(string filePath)
		{
			Console.WriteLine($"Reading {filePath}");
			try
			{
				using StreamReader sr = new StreamReader(filePath);
				var serializer = new JsonSerializer();
				return (IEnumerable<PostDetail>)serializer.Deserialize(sr, typeof(IEnumerable<PostDetail>));
			}
			catch (System.IO.FileNotFoundException ex)
			{
				throw ex;
			}
		}

		private void SaveFile(IEnumerable<PostDetail> postDetails, bool initial = true)
		{
			if (initial)
			{
				if (File.Exists(_filePath))
					File.Delete(_filePath);
			}

			Console.WriteLine($"Writing {postDetails.Count()} Reddit post(s) to {_filePath}");			
			using StreamWriter file = new StreamWriter(_filePath, false);
			var serializer = new JsonSerializer();
			serializer.Serialize(file, postDetails);

			if (postDetails.Count() == 0)
			{
				Console.WriteLine($"{_filePath} is empty. Terminating\n");	
				Exit();
			}
		}

		private void Exit()
		{
			if(aTimer != null)
			{
				aTimer.Stop();
				aTimer.Dispose();
			}

			Console.WriteLine("Enter to exit\n");
			Console.ReadLine();
			Environment.Exit(0);
		}
	}
}

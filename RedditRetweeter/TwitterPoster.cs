using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace RedditRetweeter
{
	class TwitterPoster
	{
		private const int CHARACTER_LIMIT = 280; //Twitter to expand 280 - character tweets
		private const string CONSUMER_KEY = Vault.CONSUMER_KEY;
		private const string CONSUMER_SECRET = Vault.CONSUMER_SECRET;
		private const string USER_ACCESS_TOKEN = Vault.USER_ACCESS_TOKEN;
		private const string USER_ACCESS_SECRET = Vault.USER_ACCESS_SECRET;
		private IAuthenticatedUser _user;
		private readonly ILogger _logger;
		private readonly IFileManager _fileManager;
		private readonly string _trendFilePath;
		private readonly Random _rand;

		public TwitterPoster(ILogger logger, IFileManager fileManager, string trendFilePath, bool appendTrend)
		{
			_trendFilePath = trendFilePath;
			_fileManager = fileManager;
			_logger = logger;

			Auth.SetUserCredentials(CONSUMER_KEY, CONSUMER_SECRET, USER_ACCESS_TOKEN, USER_ACCESS_SECRET);
			_user = User.GetAuthenticatedUser();

			_logger.Info("Logging into Twitter");
			_logger.Info($"Username: {_user.Name} Screenname: @{_user.ScreenName}\n");
			_rand = new Random();

			if(appendTrend)
				FetchTwitterTrends();
		}

		public void FetchTwitterTrends()
		{
			try
			{
				_logger.Info("Fetching top Twitter trends..");
				var details = Trends.GetTrendsAt(1).Trends.Select(t => t.Name);
				_fileManager.SaveFile(details, _trendFilePath, true);
			} 
			catch(Exception ex)
			{
				_logger.Info(ex.Message);
				return;
			} 
		}

		public bool ProcessTweet(PostDetail postDetail)
		{
			var trend = "";
			if (File.Exists(_trendFilePath))
			{
				var trends = _fileManager.ReadFile<IEnumerable<string>>(_trendFilePath).ToArray();
				var index = _rand.Next(trends.Count());
				trend = trends[index];
			}

			_logger.Info($"Converting Reddit post to Tweet.. ");
			var title = StringSplitter(postDetail.Title, CHARACTER_LIMIT);
			var body = StringSplitter(postDetail.Body, CHARACTER_LIMIT);
			var detailString = "Subreddit: r/" + postDetail.Subreddit + " Author: u/" + postDetail.Author + " http://redd.it/" + postDetail.Id + " .. " + trend;
			var detail = StringSplitter(detailString, CHARACTER_LIMIT);

			var tweetDetails = title.Concat(body).Concat(detail);

			return PostTweet(tweetDetails.ToList(), postDetail);
		}

		private bool PostTweet(List<string> tweetDetails, PostDetail postDetail)
		{
			_logger.Info($"Posting Tweet Id: {postDetail.Id}");
			_logger.Info($"UpVotes: {postDetail.UpVotes} DownVotes: {postDetail.DownVotes}");
			ITweet initialTweet = null;
			foreach (var tweetStr in tweetDetails)
			{
				try
				{
					if (initialTweet == null)
						initialTweet = Tweet.PublishTweet(tweetStr);
					else
					{
						PublishTweetOptionalParameters optParams;

						if (postDetail.IsText)
						{
							optParams = new PublishTweetOptionalParameters
							{
								InReplyToTweet = initialTweet
							};
						}
						else
						{
							optParams = new PublishTweetOptionalParameters
							{
								InReplyToTweet = initialTweet,
								Medias = new List<IMedia> { GetMediaToBytes(tweetStr) }
							};
			
							postDetail.IsText = true;
						}
						var reply = Tweet.PublishTweet(tweetStr, optParams);
						initialTweet = reply;
					}
				}
				catch (Exception ex)
				{
					_logger.Info(ex.Message);
				}

				if (initialTweet == null)
				{
					_logger.Info("Failed: Initial Tweet refused to post\n");
					return false;
				}
			}

			_logger.Info("Success..\n");
			return true;
		}

		private IMedia GetMediaToBytes(string url)
		{
			using WebClient client = new WebClient();
			byte[] file = client.DownloadData(url);
			var media = Upload.UploadBinary(file);
			return media;
		}

		private static IEnumerable<string> StringSplitter(string str, int chunkSize)
		{
			var delimiter = " ";
			var split = str.Split(delimiter);
			var splitStr = new List<string>();

			string tempString = string.Empty;
			for (int i = 0; i < split.Length; i++)
			{
				var spStr = split[i] + delimiter;
				tempString += spStr;
				if (tempString.Length <= chunkSize)
				{
					if (i == split.Length - 1)
						splitStr.Add(tempString);

					continue;
				}
				else
				{
					splitStr.Add(tempString.Remove(tempString.Length - spStr.Length));
					tempString = string.Empty;
					tempString += spStr;
				}
			}

			return splitStr;
		}
		public void BulkDelete() // This will purge the whole timeline
		{
			_logger.Info("Bulk deleting all posts from user timeline..");
			RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;
			var tweetsToDestroy = Timeline.GetUserTimeline(_user, 3200).ToArray();

			for (var i = 0; i < tweetsToDestroy.Length; ++i)
			{
				var tweet = tweetsToDestroy[i];
				var destroyOperationSucceeded = tweet.Destroy();

				if (!destroyOperationSucceeded)
				{
					// Ensure that the tweet still exist
					var verification = Tweet.GetTweet(tweet.Id);
					if (verification != null)
					{
						// Waiting for 30 seconds for the credentials to be available.
						Task.Delay(30000);

						// Let's try again to delete the tweet
						--i;
					}
				}
			}

			_logger.Info("Success");
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace RedditRetweeter
{
	class TwitterPoster
	{
		private const int CHARACTER_LIMIT = 280; //Twitter to expand 280 - character tweets
		private const string CONSUMER_KEY = "";
		private const string CONSUMER_SECRET = "";
		private const string USER_ACCESS_TOKEN = "";
		private const string USER_ACCESS_SECRET = "";
		private IAuthenticatedUser _user;

		public TwitterPoster()
		{
			Auth.SetUserCredentials(CONSUMER_KEY, CONSUMER_SECRET, USER_ACCESS_TOKEN, USER_ACCESS_SECRET);
			_user = User.GetAuthenticatedUser();

			Console.WriteLine("Logging into Twitter");
			Console.WriteLine($"Username: {_user.Name} Screenname: @{_user.ScreenName}\n");
		}

		public void BulkDelete() // This will purge the whole timeline
		{
			Console.WriteLine("Bulk deleting all posts from user timeline..");
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

			Console.WriteLine("Success");
		}

		public void ProcessTweet(PostDetail postDetail)
		{
			Console.WriteLine($"Converting Reddit post to Tweet.. ");
			Console.WriteLine($"Id: {postDetail.Id}");
			Console.WriteLine($"UpVotes: {postDetail.UpVotes} DownVotes: {postDetail.DownVotes} ");
			Console.Write($"Created: {postDetail.Created}\n");

			var title = StringSplitter(postDetail.Title, CHARACTER_LIMIT);
			var body = StringSplitter(postDetail.Body, CHARACTER_LIMIT);
			var detail = StringSplitter($"Subreddit: r/{postDetail.Subreddit} Author: u/{postDetail.Author} https://www.reddit.com{postDetail.Permalink}", CHARACTER_LIMIT);

			var tweetDetails = title.Concat(body).Concat(detail);
			PostTweet(tweetDetails.ToList());
		}
		private void PostTweet(List<string> tweetDetails)
		{
			Console.WriteLine("\nPosting Tweet");
			ITweet initialTweet = null;
			//PublishTweetParameters initialTweet = null;
			foreach (var tweetStr in tweetDetails)
			{
				try
				{
					if (initialTweet == null)
						initialTweet = Tweet.PublishTweet(tweetStr);
					else
					{
						var reply = Tweet.PublishTweet(tweetStr, new PublishTweetOptionalParameters
						{
							InReplyToTweet = initialTweet
						});

						initialTweet = reply;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}

				if(initialTweet == null)
				{
					Console.WriteLine("Failed: Initial Tweet refused to post\n");
					return;
				}
			}

			Console.WriteLine("Success\n");
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
	}
}

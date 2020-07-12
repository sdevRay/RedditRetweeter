using Reddit;
using Reddit.Controllers;
using Reddit.Inputs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedditRetweeter
{
	public class RedditFetcher
	{
		private const string REDDIT_APP_ID = "";
		private const string REDDIT_TOKEN = "";
		public RedditClient _client;

		public RedditFetcher()
		{
			if (_client == null)
				_client = new RedditClient(REDDIT_APP_ID, REDDIT_TOKEN);
			Console.WriteLine("Logging into Reddit");
			Console.WriteLine("Username: " + _client.Account.Me.Name + "\n");
		}

		public Subreddit GetSubreddit(SubredditNames subredditNames)
		{
			var js = Enum.GetName(typeof(SubredditNames), subredditNames);
			Subreddit subreddit;
			subreddit = _client.Subreddit(js).About();
			return subreddit;		
		}

		public IEnumerable<PostDetail> GetTopPosts(Subreddit subreddit, TimeFrame timeframe, int limit)
		{
			var tf = Enum.GetName(typeof(TimeFrame), timeframe);
			Console.Write("********************\n");
			Console.Write($"Fetching: r/{subreddit.Name}\nTopPost(s) count: {limit}\nTimeframe: {timeframe}\n");
			Console.Write("********************");

			var posts = subreddit.Posts.GetTop(new TimedCatSrListingInput(t: tf, limit: limit));
			var postDetails = new List<PostDetail>();
			if (posts.Count > 0)
			{
				foreach (Post post in posts)
				{
					postDetails.Add(new PostDetail()
					{
						Id = post.Id,
						Author = post.Author,
						Subreddit = post.Subreddit,
						Permalink = post.Permalink,
						Title = post.Title,
						Body = post.Listing.IsSelf ? ((SelfPost)post).SelfText : "",
						Created = post.Created,
						UpVotes = post.UpVotes,
						DownVotes = post.DownVotes			
					});
				}
			}
			else
			{
				Console.WriteLine($"There are no new posts from the last {timeframe}");
			}

			return postDetails.Where(pd => !string.IsNullOrEmpty(pd.Body));
		}
	}
}

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
		private const string REDDIT_APP_ID = Vault.REDDIT_APP_ID;
		private const string REDDIT_TOKEN = Vault.REDDIT_TOKEN;
		public RedditClient _client;
		private readonly ILogger _logger;

		public RedditFetcher(ILogger logger)
		{
			_logger = logger;

			if (_client == null)
				_client = new RedditClient(REDDIT_APP_ID, REDDIT_TOKEN);
			_logger.Info("Logging into Reddit");
			_logger.Info("Username: " + _client.Account.Me.Name + "\n");
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
			_logger.Info($"Fetching {limit} posts from r/{subreddit.Name} aggregated by {timeframe} timeframe");

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
						Domain = post.Listing.Domain,
						Title = post.Title,
						Body = post.Listing.IsSelf ? ((SelfPost)post).SelfText : ((LinkPost)post).URL,
						IsText = post.Listing.IsSelf,
						Created = post.Created,
						UpVotes = post.UpVotes,
						DownVotes = post.DownVotes
					});
				}
			}
			else
			{
				_logger.Info($"There are no new posts from the last {timeframe}");
			}

			return ValidatePosts(postDetails);
		}

		private IEnumerable<PostDetail> ValidatePosts(IEnumerable<PostDetail> postDetails)
		{
			var domains = new List<string>() { "i.imgur.com", "i.redd.it" };
			var posts = postDetails.Where(pd => !string.IsNullOrEmpty(pd.Body)).Where(pd =>
			{
				if (!pd.IsText)
					return domains.Contains(pd.Domain);

				return true;
			});

			_logger.Info($"{posts.Count()} posts succesfully validated");
			return posts;
		}
	}
}

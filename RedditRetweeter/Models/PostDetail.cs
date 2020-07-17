using System;

namespace RedditRetweeter
{
	public class PostDetail
	{
		public string Id { get; set; }
		public string Permalink { get; set; }
		public string Subreddit { get; set; }
		public string Author { get; set; }
		public string Title { get; set; }
		public string Domain { get; set; }
		public string Body { get; set; }
		public bool IsText { get; set; }
		public int UpVotes { get; set; }
		public int DownVotes { get; set; }
		public DateTime Created { get; set; }
	}
}

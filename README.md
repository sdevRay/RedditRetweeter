# RedditRetweeter v0.5

C# Console app Reddit/Twitter bot [@Alpha_Retweeter](https://twitter.com/Alpha_Retweeter)

This project was an experiment at using libraries and APIs with C#. It retrieves Reddit data saves it to a file, then retrieves it on interval and tweets it to Twitter.

Libraries
- [Reddit.NET](https://github.com/sirkris/Reddit.NET)
- [Tweetinvi](https://github.com/linvi/tweetinvi)

A predetermined limit of Reddit posts are pulled from a subreddit, this content is formatted and saved to a file in JSON format. A timer scans the file then pulls the contents so a single entry can be extracted. This entry is removed from the file and then formatted for a post to twitter. The title, body and credits are separate into different replies on the same post.

![RedditRetweeter Screenshot](https://github.com/sdevRay/sdevRay.github.io/blob/master/assets/redditretweeter.png?raw=true)

These values will need to be acquired from their respective places

[Twitter](https://developer.twitter.com/en/docs)
- CONSUMER_KEY
- CONSUMER_SECRET
- USER_ACCESS_TOKEN
- USER_ACCESS_SECRET

[Reddit](https://www.reddit.com/dev/api/)
- REDDIT_APP_ID
- REDDIT_TOKEN

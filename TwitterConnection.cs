using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToTwitter;

namespace SharpConsoleTweeter {
    public class TwitterConnection {

        TwitterContext TwitterContext { get; set; }
        public List<Status> Tweets { get; set; }

        public DateTime LastTweetTime {
            get {
                if (this.Tweets.Count > 0) {
                    return this.Tweets.OrderBy(tweet => tweet.CreatedAt).Last().CreatedAt;
                }
                else {
                    return DateTime.MinValue;
                }
            }
        }
        
        public TwitterConnection() {
            this.Tweets = new List<Status>();
            this.createTwitterContext();
        }

        public int UpdateTimeline() {
            return this.UpdateTimeline(true);
        }

        public int UpdateTimeline(bool forwards) {
            if (forwards) {
                // grab newest updates
                var homeTweets = from tweet in this.TwitterContext.Status
                                 where tweet.Type == StatusType.Home && tweet.CreatedAt > this.LastTweetTime
                                 orderby tweet.CreatedAt
                                 select tweet;

                var homeTweetsList = homeTweets.ToList();
                homeTweetsList.ForEach(tweet => tweet.Text = tweet.Text.Replace('\n', ' '));

                this.Tweets.AddRange(homeTweetsList);

                // return the number of new tweets
                return homeTweetsList.Count();
            }
            else {
                // grab older updates
                var oldTweets = from tweet in this.TwitterContext.Status
                                 where tweet.Type == StatusType.Home && tweet.MaxID == Convert.ToUInt64(this.Tweets.First().StatusID) && tweet.StatusID != this.Tweets.First().StatusID
                                 orderby tweet.CreatedAt
                                 select tweet;

                var oldTweetsList = oldTweets.ToList();
                oldTweetsList.ForEach(tweet => tweet.Text = tweet.Text.Replace('\n', ' '));

                this.Tweets.InsertRange(0, oldTweets);

                return oldTweetsList.Count();
            }
        }

        public void Tweet(string tweet) {
            this.TwitterContext.UpdateStatus(tweet);
        }

        void createTwitterContext() {
            var auth = new DesktopOAuthAuthorization(); 
            this.TwitterContext = new TwitterContext(auth, "https://api.twitter.com/1/", "https://search.twitter.com/");
            if (this.TwitterContext.AuthorizedClient is OAuthAuthorization) {
                this.initializeOAuthConsumerStrings(this.TwitterContext);
            }

            try {
                auth.SignOn();
            }
            catch (OperationCanceledException) {
                Console.WriteLine("Couldn't connect to Twitter!");
                Environment.Exit(-1);
            }
        }

        void initializeOAuthConsumerStrings(TwitterContext twitterCtx) {
            var oauth = (DesktopOAuthAuthorization)twitterCtx.AuthorizedClient;
            oauth.GetVerifier = () => {
                Console.WriteLine("Next, you'll need to tell Twitter to authorize access.\nThis program will not have access to your credentials, which is the benefit of OAuth.\nOnce you log into Twitter and give this program permission,\n come back to this console.");
                Console.Write("Please enter the PIN that Twitter gives you after authorizing this client: ");
                return Console.ReadLine();
            };

            if (oauth.CachedCredentialsAvailable) {
                return;
            }
        }
    }
}


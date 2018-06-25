using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpConsoleTweeter {

    class Program {
        static Thread updateTimer;

        static void Main(string[] args) {
            var twitter = new TwitterConnection();
            DateTime lastTweetTime = twitter.LastTweetTime;
            twitter.UpdateTimeline();

            var consoleWindow = new ConsoleWindow(twitter);
            consoleWindow.StartLoop();
            //bool running = true;
            //updateTimer = new Thread(() => {
            //    while (running) {
            //        var tweets = twitter.UpdateTimeline();
            //        Console.WriteLine("Found {0} new tweets", tweets);
            //        Thread.Sleep(5000);
            //    }
            //});
            //updateTimer.Start();

            //while (running) {
            //    twitter.Tweets.Where(tweet => tweet.CreatedAt > lastTweetTime).ToList().ForEach(tweet => Console.WriteLine("{0}: {1} ({2})", tweet.User.Identifier.ScreenName, tweet.Text, tweet.CreatedAt));
            //    Thread.Sleep(1000);
            //}
        }
    }
}

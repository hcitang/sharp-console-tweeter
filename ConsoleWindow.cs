using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CursesSharp;
using LinqToTwitter;

namespace SharpConsoleTweeter {
    public class ConsoleWindow {
        string TweetText { get; set; }
        string[] visibleTimelineBuffer;
        int topTweet;
        List<Status> tweets;
        TwitterConnection twitterConnection;
        int insertionPt, scrollPt;

        const int STATUS_HEIGHT = 2;

        public ConsoleWindow(TwitterConnection twitterConnection) {
            Curses.InitScr();           
            Curses.CBreakMode = true;
            Curses.Echo = false;
            Curses.StdScr.Keypad = true;
            Curses.StartColor();
            Curses.InitPair(1, Colors.WHITE, Colors.RED);
            Curses.InitPair(2, Colors.RED, Colors.WHITE);

            this.tweets = twitterConnection.Tweets;
            this.twitterConnection = twitterConnection;
            this.visibleTimelineBuffer = new string[Curses.Lines - STATUS_HEIGHT - 1];
            this.TweetText = "";

            this.topTweet = this.tweets.Count - 1;
            this.updateVisibleTimeline(this.topTweet);
            this.refreshTimeline();

            this.refreshTweetWindow();                        
        }

        public void StartLoop() {
            while (true) {
                var i = Curses.StdScr.GetChar();
                var c = (char)i;
                switch (c) {
                    case 'n':
                    case 'j':
                        this.tweetIncrement(1);
                        break;
                    case 'p':
                    case 'k':
                        this.tweetIncrement(-1);
                        break;
                    case (char)11: /* ^K */
                    case (char)16: /* ^P */
                        this.tweetIncrement(-5);
                        break;
                    case (char)32: /* [spacebar] */
                    case (char)14: /* ^N */
                        this.tweetIncrement(5);
                        break;
                    case (char)10: /* ^M */
                        var tweetText = this.tweets[topTweet].Text;
                        var urls = tweetText.Split(new char[] { ' ' }).Where(token => token.StartsWith("http://"));
                        if (urls.Count() > 0) {
                            System.Diagnostics.Process.Start(urls.First());
                        }
                        break;
                    case (char)18: /* ^R */
                        this.refreshStatusBar("Refreshing...");
                        var tweets = this.refreshTweets();
                        this.refreshStatusBar(string.Format("Refreshed: {0} new tweets", tweets));
                        break;
                    case 'c':
                    case 'C':
                    case 'r':
                    case 'R':
                        // turn off highlight
                        this.refreshTimeline();
                        this.refreshStatusBar("Compose");

                        var tweet = this.tweets[topTweet];

                        if (c == 'r') {
                            this.TweetText = string.Format("@{0} ", tweet.User.Identifier.ScreenName);
                        }
                        else if (c == 'R') {
                            this.TweetText = string.Format("RT @{0}: {1}", tweet.User.Identifier.ScreenName, tweet.Text);
                        }
                        this.refreshTweetWindow(true);
                        var sent = this.composeTweet();
                        break;
                    case 'q':
                        Curses.EndWin();
                        Environment.Exit(-1);
                        break;
                }
            }
        }

        bool composeTweet() {
            // don't know how to get the arrow keys to work

            this.insertionPt = 0;
            while (true) {
                //var i = Curses.StdScr.GetChar();
                //if (i >= 32 && i <= 176) { // readable characters
                //    this.TweetText += (char)i;
                //}
                //else if (i == 8 /*^H - backspace */ && this.TweetText.Length > 0) {
                //    this.TweetText = this.TweetText.Substring(0, this.TweetText.Length - 1);
                //}
                //else if (i == 9 /*TAB*/ || i == 27 /*ESC*/ || i == 3 /*^C*/) {
                //    this.TweetText = "";
                //    this.refreshStatusBar("Tweet cancelled");

                //    this.refreshTweetWindow();
                //    return false;
                //}
                var i = Curses.StdScr.GetChar();
                if (i >= 32 && i <= 176) { // readable characters
                    var beforeText = this.TweetText.Substring(0, insertionPt);
                    var afterText = this.TweetText.Substring(insertionPt);
                    this.TweetText = beforeText + (char)i + afterText;
                    insertionPt++;
                    if (insertionPt >= Curses.Cols) {
                        scrollPt++;
                    }
                }
                else if (i == 8 /*^H - backspace */ && this.TweetText.Length > 0) {
                    var beforeText = this.TweetText.Substring(0, insertionPt);
                    var afterText = this.TweetText.Substring(insertionPt);
                    if (beforeText.Length > 0) {
                        this.TweetText = beforeText.Substring(0, beforeText.Length - 1) + afterText;
                        insertionPt--;

                        if (insertionPt < scrollPt) {
                            scrollPt--;
                        }
                        else if (scrollPt > 0) {
                            scrollPt--;
                        }
                    }
                }
                else if (i == 9 /*TAB*/ || i == 27 /*ESC*/ || i == 3 /*^C*/) {
                    this.TweetText = "";
                    this.refreshStatusBar("Tweet cancelled");
                    this.refreshTweetWindow();
                    return false;
                }
                else if (i == 260) { /* <- */
                    insertionPt--;
                    if (insertionPt < 0) {
                        insertionPt = 0;
                        if (scrollPt > 0) {
                            scrollPt--;
                        }
                    }
                }
                else if (i == 261) { /* -> */
                    insertionPt++;
                    if (insertionPt > this.TweetText.Length) {
                        insertionPt = this.TweetText.Length;
                    }
                    if (insertionPt >= scrollPt + Curses.Cols) {
                        scrollPt++;
                    }
                }

                else if (i == 10 /*^M - enter */) {
                    if (this.TweetText.Length >= 140) {
                        this.refreshStatusBar("Compose - Ensure tweet has 140 characters or fewer");
                        continue;
                    }
                    System.Diagnostics.Debug.WriteLine("Tweet update: " + this.TweetText);
                    this.twitterConnection.Tweet(this.TweetText);
                    if (this.TweetText.Length > 66) {
                        this.refreshStatusBar("Tweet sent: " + this.TweetText.Substring(0, 66));
                    }
                    else {
                        this.refreshStatusBar("Tweet sent: " + this.TweetText);
                    }
                    this.TweetText = "";

                    this.refreshTweetWindow();
                    return true;
                }

                this.refreshTweetWindow(true);
            }

        }

        int refreshTweets() {
            var newTweetCount = this.twitterConnection.UpdateTimeline();
            if (newTweetCount > 0) {
                this.updateVisibleTimeline(this.topTweet);
            }
            return newTweetCount;
        }

        void tweetIncrement(int direction) {
            if (direction > 0) {
                this.topTweet += -direction;
                if (this.topTweet < 0) {

                    this.refreshStatusBar("Fetching older tweets...");
                    var count = this.twitterConnection.UpdateTimeline(false);
                    this.topTweet = count;
                    this.refreshStatusBar("Fetched " + count + " older tweets");
                }
            }
            else {
                this.topTweet += -direction;
                if (this.topTweet > this.tweets.Count - 1) {
                    this.topTweet = this.tweets.Count - 1;
                }
            }

            this.updateVisibleTimeline(topTweet);
            this.refreshTimeline();
            this.refreshHighlight();
        }

        void refreshHighlight() {
            Curses.StdScr.AttrOn(Attrs.REVERSE);
            var twoLiner = this.visibleTimelineBuffer[1].IndexOf('|') == -1;
            for (int i = 0, y = 2; i < (twoLiner ? 2 : 1); i++, y++) {
                Curses.StdScr.Move(y, 0);
                Curses.StdScr.HLine(32, Curses.Cols);
                var currentLine = this.visibleTimelineBuffer[i];
                var separatorIndex = currentLine.IndexOf('|');
                if (separatorIndex > -1) {
                    Curses.StdScr.AttrOn(Attrs.BOLD);
                    Curses.StdScr.Add(y, 0, currentLine.Substring(0, separatorIndex) + " ");
                    Curses.StdScr.AttrOff(Attrs.BOLD);
                    Curses.StdScr.Add(y, separatorIndex + 1, currentLine.Substring(separatorIndex + 1));
                }
                else {
                    Curses.StdScr.Add(y, 0, currentLine);
                }
            }
            Curses.StdScr.AttrOff(Attrs.REVERSE);
            Curses.StdScr.Redraw(Curses.Lines - 1, 1);

            this.refreshStatusBar(string.Format("{0} ({1})", this.tweets[topTweet].User.Name, this.tweets[topTweet].User.Identifier.ScreenName), this.tweets[topTweet].CreatedAt.ToString());
        }

        void updateVisibleTimeline(int topTweet) {
            int line = 0;
            int totalLines = this.visibleTimelineBuffer.Length;
            int currentTweet = topTweet;
            string currentTweetText;

            currentTweetText = string.Format("{0}|{1}", this.tweets[currentTweet].User.Identifier.ScreenName, this.tweets[currentTweet].Text);

            while (line < totalLines) {
                var charsInLine = Math.Min(currentTweetText.Length, Curses.Cols);
                this.visibleTimelineBuffer[line] = currentTweetText.Substring(0, charsInLine);
                if (charsInLine < currentTweetText.Length) {
                    currentTweetText = currentTweetText.Substring(charsInLine);
                }
                else {
                    currentTweet--;
                    // fill the rest of the buffer if we're at the last tweet in the timeline
                    if (currentTweet >= 0) {
                        currentTweetText = string.Format("{0}|{1}", this.tweets[currentTweet].User.Identifier.ScreenName, this.tweets[currentTweet].Text);
                    }
                    else {
                        for (int i = line + 1; i < totalLines; i++) {
                            this.visibleTimelineBuffer[i] = "";
                        }
                        break;
                    }
                }
                line++;
            }
        }

        void refreshTimeline() {
            int i, y;
            for (i = 0, y = STATUS_HEIGHT; i < this.visibleTimelineBuffer.Length; i++, y++) {
                
                Curses.StdScr.Move(y, 0);
                Curses.StdScr.HLine(32, 80);

                var currentLine = this.visibleTimelineBuffer[i];
                var separatorIndex = currentLine.IndexOf('|');
                if (separatorIndex > -1) {
                    Curses.StdScr.AttrOn(Attrs.BOLD);
                    Curses.StdScr.Add(y, 0, currentLine.Substring(0, separatorIndex));
                    Curses.StdScr.AttrOff(Attrs.BOLD);
                    Curses.StdScr.Add(y, separatorIndex + 1, currentLine.Substring(separatorIndex + 1));
                }
                else {
                    Curses.StdScr.Add(y, 0, currentLine);
                }
            }            
            Curses.StdScr.Redraw(STATUS_HEIGHT, Curses.Lines - STATUS_HEIGHT);
        }

        void refreshTweetWindow() {
            this.refreshTweetWindow(false);
        }
        void refreshTweetWindow(bool composing) {
            var drawString = TweetText;
            if (TweetText.Length > 80) {
                if (insertionPt < scrollPt) {
                    scrollPt--;
                }
                if (scrollPt > 0) {
                    drawString = "<" + TweetText.Substring(scrollPt);
                }
                if ((insertionPt - scrollPt) == 79) {
                    drawString = drawString.Substring(0, 80);
                }
                if ((insertionPt - scrollPt) < 78 && drawString.Length > 80) {
                    drawString = drawString.Substring(0, 79) + ">";
                }
            }
            if (composing) {
                Curses.StdScr.AttrOn(Attrs.REVERSE);
            }
            if (TweetText.Length > 140) {
                Curses.StdScr.Color = 2;
            }
            Curses.StdScr.Move(0, 0);
            Curses.StdScr.HLine(32, Curses.Cols);
            Curses.StdScr.Add(0, 0, drawString);
            if (composing) {
                Curses.StdScr.AttrOff(Attrs.REVERSE);
            }
            Curses.StdScr.Color = 0;

            Curses.StdScr.Move(STATUS_HEIGHT - 1, 0);
            Curses.StdScr.HLine(45, 80);
            if (TweetText.Length > 0) {
                var length = string.Format("({0})", TweetText.Length);
                if (TweetText.Length > 140) {
                    Curses.StdScr.Color = 1;
                }
                Curses.StdScr.Add(STATUS_HEIGHT - 1, Curses.Cols - (1 + length.Length), length);
                if (TweetText.Length > 140) {
                    Curses.StdScr.Color = 0;
                }
            }

            Curses.StdScr.Move(0, insertionPt - scrollPt);
            Curses.StdScr.Redraw(0, STATUS_HEIGHT);
            //var drawString = this.TweetText;
            //if (this.TweetText.Length > 80) {
            //    drawString = "<" + this.TweetText.Substring(this.TweetText.Length - 79, 79);
            //}
            //if (composing) {
            //    Curses.StdScr.AttrOn(Attrs.REVERSE);
            //}
            //if (this.TweetText.Length > 140) {
            //    Curses.StdScr.Color = 2;
            //} 
            //Curses.StdScr.Move(0, 0);
            //Curses.StdScr.HLine(32, Curses.Cols);
            //Curses.StdScr.Add(0, 0, drawString);
            //if (composing) {
            //    Curses.StdScr.AttrOff(Attrs.REVERSE);
            //}
            //Curses.StdScr.Color = 0;

            //Curses.StdScr.Move(STATUS_HEIGHT - 1, 0);
            //Curses.StdScr.HLine(45, 80);
            //if (this.TweetText.Length > 0) {
            //    var length = string.Format("({0})", this.TweetText.Length);
            //    if (this.TweetText.Length > 140) {
            //        Curses.StdScr.Color = 1;
            //    }
            //    Curses.StdScr.Add(STATUS_HEIGHT - 1, Curses.Cols - (1 + length.Length), length);
            //    if (this.TweetText.Length > 140) {
            //        Curses.StdScr.Color = 0;
            //    }
            //}
            //Curses.StdScr.Redraw(0, STATUS_HEIGHT);
        }

        void refreshStatusBar(string left) {
            this.refreshStatusBar(left, "");
        }

        void refreshStatusBar(string left, string right) {
            Curses.StdScr.AttrOn(Attrs.REVERSE);
            Curses.StdScr.AttrOn(Attrs.BOLD);

            Curses.StdScr.Move(Curses.Lines - 1, 0);
            Curses.StdScr.HLine(32, Curses.Cols);

            Curses.StdScr.Add(Curses.Lines - 1, 1, left);
            Curses.StdScr.Add(Curses.Lines - 1, Curses.Cols - (right.Length + 1), right);
                        
            Curses.StdScr.AttrOff(Attrs.BOLD);
            Curses.StdScr.AttrOff(Attrs.REVERSE);

            Curses.StdScr.Redraw(Curses.Lines - 1, 1);
        }
    }
}

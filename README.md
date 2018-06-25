# Sharp Console Tweeter

A console-based twitter app with geeky keybindings. Makes use of [CursesSharp](http://sourceforge.net/projects/curses-sharp/) and [LinqToTwitter](http://linqtotwitter.codeplex.com/).

## Key bindings

| Key   | Action |
| ----- | ------ |
| c     | compose |
| j/k   | move down one tweet/move up one tweet |
| ^j/^k | move down a bunch of tweets/move up a bunch of tweets |
| ^r    | refresh for newest tweets |
| esc   | break out of compose mode |
| q     | quits |

## Cool dude features

* Launches URLs in default browser
* Uses OAuth somehow (don't really know how this works)
* A weird bug in the compose mode that has to do with how it handles scrolling

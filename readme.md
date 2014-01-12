GitScrobbler
============

Cross-reference commit history with your Last.fm scrobbles.
Released into the [public domain](https://creativecommons.org/publicdomain/zero/1.0/).

Usage
-----
Assuming you have a Last.fm account, you need an API-key as
well. Get one [here](http://www.last.fm/api/account/create).
Write your favourite Git log command (e.g. to filter out
only your commits in a multi-user repository), and then
format it with `%at %h %s`. Now pipe this through GitScrobbler,
with the Last.fm username of the user that you want to check
the scrobbles for, as well as your API-key.

    $ git log --format='%at %h %s' | gitscrobbler --username <username> --apikey <apikey>

GitScrobbler will print the commits that matched a track,
and provide you with some basic statistics afterwards.

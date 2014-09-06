// GitScrobbler -- Cross-reference commit history with your Last.fm scrobbles
// Written in 2013 and 2014 by Ruud van Asseldonk
//
// To the extent possible under law, the author has dedicated all copyright and
// related and neighboring rights to this software to the public domain worldwide.
// This software is distributed without any warranty. 
//
// You should have received a copy of the CC0 Public Domain Dedication along with
// this software. If not, see <https://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GitScrobbler
{
  class Program
  {
    static void Main(string[] args)
    {
      // Get Last.fm API key and username as args.
      string apiKey   = null;
      string username = null;

      for (int i = 0; i < args.Length - 1; i++)
      {
        if (args[i] == "--apikey")   apiKey   = args[i + 1];
        if (args[i] == "--username") username = args[i + 1];
      }

      if (apiKey == null || username == null)
      {
        Console.WriteLine("GitScrobbler -- Cross-reference commit history with your Last.fm scrobbles");
        Console.WriteLine("Usage: ");
        Console.WriteLine("git log --format='%at %h %s' | gitscrobbler --username <your-last.fm-username> --apikey <your-last.fm-api-key>");
        return;
      }

      // Read the commit log from standard input, and parse the messages.
      var commits = GetLog()
        .Select(x => x.Split(new char[] { ' ' }, 3))
        .Select(x => new
        {
          Timestamp = long.Parse(x[0]),
          Hash = x[1],
          Message = x[2]
        })
        .ToArray(); // GetLog is not pure, evaluate it once.

      // Keep some statistics about scrobbled tracks.
      var artists = new Dictionary<string,int>();
      var tracks = new Dictionary<Tuple<string,string>,int>();
      int totalScrobbles = 0;

      Console.WriteLine("Scrobbles");
      Console.WriteLine("=========");

      foreach (var commit in commits)
      {
        // Retrieve and parse a few scrobbles from Last.fm.
        var scrobbles = GetScrobbles(username, commit.Timestamp, apiKey)
          .Where(x => x.Attribute("nowplaying") == null)
          .Select(x => new
          {
            Timestamp = long.Parse(x.Element("date").Attribute("uts").Value),
            Track = x.Element("name").Value,
            Artist = x.Element("artist").Value,
            Duration = GetTrackDuration(x.Element("mbid").Value, apiKey)
          });

        // Find the one that was within the correct time frame.
        var closestMatch = scrobbles
          .OrderByDescending(s => s.Timestamp)
          .FirstOrDefault(s => s.Timestamp <= commit.Timestamp &&
                               s.Timestamp + s.Duration >= commit.Timestamp);

        // If any scrobble matched, print it and store statistics.
        if (closestMatch != null)
        {
          var artist = closestMatch.Artist;
          var track  = closestMatch.Track;
          var tuple  = Tuple.Create(track, artist);

          Console.WriteLine("{0} {1}", commit.Hash, commit.Message);
          Console.WriteLine("{0} {1} — {2}", FromTimestamp(commit.Timestamp).ToString("yyyy-MM-dd HH:mm"), track, artist);
          Console.WriteLine();

          if (!artists.ContainsKey(artist)) artists[artist] = 0;
          if (!tracks.ContainsKey(tuple)) tracks[tuple] = 0;
          artists[artist]++;
          tracks[tuple]++;
          totalScrobbles++;
        }
      }

      // And when all commits have been processed, print statistics.
      var topArtists = artists.OrderByDescending(kvp => kvp.Value);
      var topTracks = tracks.OrderByDescending(kvp => kvp.Value);

      Console.WriteLine("Top Artists");
      Console.WriteLine("===========");
      foreach (var kvp in topArtists.Take(10))
      {
        Console.WriteLine("{0} ({1} commits)", kvp.Key, kvp.Value);
      }
      Console.WriteLine();

      Console.WriteLine("Top Tracks");
      Console.WriteLine("==========");
      foreach (var kvp in topTracks.Take(10))
      {
        Console.WriteLine("{0} — {1} ({2} commits)", kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
      }
      Console.WriteLine();

      Console.WriteLine("Coverage");
      Console.WriteLine("========");
      Console.WriteLine("{0:0.0%} of the commits was matched to a scrobble.", (double)totalScrobbles / commits.Count());
      Console.WriteLine();
    }

    static IEnumerable<string> GetLog()
    {
      string line;

      // Read all lines from standard input.
      while ((line = Console.ReadLine()) != null)
      {
        yield return line;
      }
    }

    static IEnumerable<XElement> GetScrobbles(string username, long timestamp, string apiKey)
    {
      try
      {
        long from = timestamp - 60 * 10; // Started listining from ten minutes before the commit,
        long to = timestamp + 10;        // to ten seconds after the commit.

        string url = String.Format(
          "http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={0}&from={1}&to={2}&api_key={3}",
          Uri.EscapeUriString(username), from, to, apiKey);

        return XDocument.Load(url).Root.Element("recenttracks").Elements("track");
      }
      catch
      {
        return new XElement[] { };
      }
    }

    static int GetTrackDuration(string mbid, string apiKey)
    {
      try
      {
        string url = String.Format(
         "http://ws.audioscrobbler.com/2.0/?method=track.getInfo&mbid={0}&api_key={1}",
         mbid, apiKey);

        // Duration seems to be in milliseconds, convert it to seconds.
        return int.Parse(XDocument.Load(url).Root.Element("track").Element("duration").Value) / 1000;
      }
      catch
      {
        return -1;
      }
    }

    static DateTime FromTimestamp(long timestamp)
    {
      // Convert unix timestamp to a DateTime.
      DateTime epoch = new DateTime(1970, 1, 1, 00, 00, 00);
      return epoch.AddSeconds(timestamp);
    }
  }
}

# Podcast Downloader

This program is a simple F# console app for downloading in bulk podcast episodes and show notes directly from the RSS feed.

## Running the Program

To run the program, first clone this repository, then open the `specifications.json` file. The default contents of this file are shown below.

```
{
    "destination":"C:\\Podcast Archive",
    "podcasts": [
        {
            "link":"https://feeds.simplecast.com/BqbsxVfO",
            "includeNotes":true
        },
        {
            "link":"http://feed.songexploder.net/SongExploder",
            "includeNotes":true
        }
    ]
}
```

Set the `destination` field to an empty folder where you wish for the podcast episodes to be saved. `podcasts` then specifies the podcasts to download. The `link` field is a link to the RSS feed for the podcast to download. If `includeNotes` is set to true then for each episode a text file will also be created with the show notes (if they exist).

Then just run `application/podcast-downloader.exe`.

# Planned Changes

- Option to download only between a supplied date range
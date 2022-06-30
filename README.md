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
            "includeNotes":true,
            "dateRange":"AllTime"
        },
        {
            "link":"http://feed.songexploder.net/SongExploder",
            "includeNotes":true,
            "dateRange":"2021/05/12-2022/01/05"
        }
    ]
}
```

Here are definitions and specifications for each of the fields:

- `destination` should be the path to an empty folder where you wish for the podcast episodes to be downloaded to.
- `link` specifies the RSS feed for a given podcast.
- `includeNotes` determines if a `.txt` file should be created with the show notes (if they exist).
- `dateRange` specifies the range of publish dates to download. These should be formatted as "YYYY/MM/DD-YYYY/MM/DD" (an example is shown above) or as "AllTime" to remove date filtering. 

After specifying the above data in the file, just run `dotnet build` and call the produced executable from the directory with the specification file.

## Resources

For more information on the podcast RSS standard, [Google's documentation](https://support.google.com/podcast-publishers/answer/9889544?hl=en) is great.

module PodcastDownloader

open System.Xml
open System.Net.Http
open System.IO
open System


type DateRange =
    | AllTime
    | Range of DateTime * DateTime

type DownloadSpecifications =
    {
        destination : string;
        includeNotes : bool;
        link : string;
        dateRange : DateRange;
    }

type Episode =
    {
        name : string;
        url : string option;
        showNotes : string option;
        publishDate : DateTime option;
    }

type Podcast =
    {
        name : string;
        episodes : Episode list
    }


let fixFileName (filename : string) : string =
    let invalid =
        Array.concat [Path.GetInvalidFileNameChars (); Path.GetInvalidPathChars ()]

    filename
    |> Seq.where (fun c -> not (invalid |> Array.exists (fun x -> x = c)))
    |> Seq.fold (fun s m -> s + string m) ""


let getFeed (specs : DownloadSpecifications) : (DownloadSpecifications * string) =

    printfn $"Fetching Feed: {specs.link}"
    
    let client = new HttpClient ()
    
    let request = new HttpRequestMessage (System.Net.Http.HttpMethod.Get, specs.link)

    // To bypass Squarespace feeds blocking certain user agents
    request.Headers.UserAgent.ParseAdd "Mozilla/5.0"
    
    let feed =
        (client.Send request).Content.ReadAsStringAsync ()
        |> (Async.AwaitTask >> Async.RunSynchronously)
    
    client.Dispose ()
    
    (specs, feed)

let getPodcast (specs : DownloadSpecifications, feed : string) : (DownloadSpecifications * Podcast) =
    let document = new XmlDocument ()
    document.LoadXml feed

    let parsePubDate (pubDate : string) : DateTime option =
        let (success, date) = DateTime.TryParse (pubDate, null, System.Globalization.DateTimeStyles.None)
        
        if success then
            Some date
        else
            None

    let processItem (node : XmlNode) =
        let title = (node.Item "title").InnerText
        let enclosureNode = (node.Item "enclosure")


        let url =
            if enclosureNode = null then
                None
            else
                Some ((enclosureNode.Attributes.GetNamedItem "url").InnerText)

        let showNotes =
            try
                Some ((node.Item "description").InnerText)
            with
                | _ -> None

        let publishDate =
            try
                let date = (node.Item "pubDate").InnerText
                parsePubDate date
            with
                | _ -> None

        {
            name = title;
            url = url;
            showNotes = showNotes;
            publishDate = publishDate;
        }

    let channel =
        document
        |> (fun x -> x.DocumentElement)
        |> (fun x -> x.Item "channel")

    let episodes =
        channel
        |> Seq.cast<XmlNode>
        |> Seq.where (fun x -> x.Name.ToLower () = "item")
        |> Seq.map processItem
        |> List.ofSeq

    let title =
        channel
        |> (fun x -> x.Item "title")
        |> (fun x -> x.InnerText)

    let podcast =
        {
            name = title;
            episodes = episodes;
        }

    (specs, podcast)

let downloadEpisode (specs : DownloadSpecifications) (podcast : Podcast) (episode : Episode) (count : int, total : int) : unit =
    match episode.url with
    | Some url ->
        printfn $"Downloading {podcast.name} ({count}/{total}) - {episode.name}"

        let httpClient = new HttpClient ()

        let response =
            (httpClient.GetAsync url)
            |> (Async.AwaitTask >> Async.RunSynchronously)

        httpClient.Dispose ()
    
        let extension =
            response
            |> (fun x -> x.Content)
            |> (fun x -> x.Headers)
            |> (fun x -> x.ContentType)
            |> (fun x -> x.MediaType)
            |> ContentType.toExtension

        let filePath =
            specs.destination + @"\" + fixFileName podcast.name + @"\" + fixFileName episode.name + extension

        let fileStream = new FileStream (filePath, FileMode.Create)

        response
        |> (fun x -> x.Content)
        |> (fun x -> x.CopyToAsync fileStream)
        |> (Async.AwaitTask >> Async.RunSynchronously)

        fileStream.Close ()
        fileStream.Dispose ()

        if specs.includeNotes then
            match episode.showNotes with
            | Some notes ->
                File.WriteAllText (specs.destination + @"\" + fixFileName podcast.name + @"\" + fixFileName episode.name + ".txt", notes)
            | None -> ()

    | None ->
        printfn $"Skipping {podcast.name} ({count}/{total}) - {episode.name} as no enclosure was found."
    
let createFolder (specs : DownloadSpecifications, podcast : Podcast) : (DownloadSpecifications * Podcast) =
     let directoryInfo = Directory.CreateDirectory (specs.destination + @"\" + fixFileName podcast.name)
     
     if not directoryInfo.Exists then
         failwith $"Failed to create directory for podcast {podcast.name}."
     
     (specs, podcast)
    
let downloadPodcast (specs : DownloadSpecifications, podcast : Podcast) : unit =
    let episodesWithinDateRange =
        podcast.episodes
        |> List.where (fun x ->
            match specs.dateRange with
            | AllTime ->
                true
            | Range (fromDate, toDate) ->
                match x.publishDate with
                | Some date ->
                    (date >= fromDate) && (date < toDate)
                | None ->
                    true
            )
        
    let total =
        episodesWithinDateRange
        |> List.length

    episodesWithinDateRange
    |> List.mapi (fun count episode -> downloadEpisode specs podcast episode (count + 1, total))
    |> ignore

let bulkDownload (specsList : DownloadSpecifications list) =
    specsList
    |> List.map (getFeed >> getPodcast >> createFolder >> downloadPodcast)
    |> ignore
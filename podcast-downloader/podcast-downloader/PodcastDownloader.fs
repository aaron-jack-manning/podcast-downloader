module PodcastDownloader

open System.Xml
open FSharp.Data
open System.Net.Http
open System.Net
open MimeTypes
open System.IO


type DownloadSpecifications =
    {
        destination : string;
        includeNotes : bool;
        link : string;
    }

type Episode =
    {
        name : string;
        url : string;
        showNotes : string option
    }

type Podcast =
    {
        name : string;
        episodes : Episode list
    }


let fixFileName (filename : string) : string =
    let invalid =
        Array.append (Path.GetInvalidFileNameChars ()) (Path.GetInvalidPathChars ())

    filename
    |> Seq.where (fun c -> not (invalid |> Array.exists (fun x -> x = c)))
    |> Seq.fold (fun s m -> s + string m) ""


let getFeed (specs : DownloadSpecifications) : (DownloadSpecifications * string) =

    printfn "Fetching Feed"
    
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

    let processItem (node : XmlNode) =
        let title = (node.Item "title").InnerText
        let enclosureNode = (node.Item "enclosure")
        let url = (enclosureNode.Attributes.GetNamedItem "url").InnerText

        let showNotes =
            try
                Some ((node.Item "description").InnerText)
            with
                | _ -> None
            

        {
            name = title;
            url = url;
            showNotes = showNotes;
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

let downloadFile (url : string) (destination : string) : unit =
    
    
    let httpClient = new HttpClient ()

    let response =
        httpClient.GetAsync (url)
        |> (Async.AwaitTask >> Async.RunSynchronously)


    let fileStream = new FileStream (destination, FileMode.CreateNew)

    response.Content.CopyToAsync(fileStream)
    |> (Async.AwaitTask >> Async.RunSynchronously)


    ()

let downloadEpisode (specs : DownloadSpecifications) (podcast : Podcast) (episode : Episode) (count : int, total : int) : unit =

    printfn $"Downloading {podcast.name} ({count}/{total}) - {episode.name}"

    let httpClient = new HttpClient ()

    let response =
        (httpClient.GetAsync episode.url)
        |> (Async.AwaitTask >> Async.RunSynchronously)

    httpClient.Dispose ()
    
    let extension =
        response
        |> (fun x -> x.Content)
        |> (fun x -> x.Headers)
        |> (fun x -> x.ContentType)
        |> (fun x -> x.MediaType)
        |> MimeTypeMap.GetExtension


    let filePath =
        specs.destination + @"\" + podcast.name + @"\" + fixFileName episode.name + extension

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
            File.WriteAllText (specs.destination + @"\" + podcast.name + @"\" + fixFileName episode.name + ".txt", notes)
        | None -> ()
    
let createFolder (specs : DownloadSpecifications, podcast : Podcast) : (DownloadSpecifications * Podcast) =
     Directory.CreateDirectory (specs.destination + @"\" + fixFileName podcast.name) |> ignore
     (specs, podcast)
    
let downloadPodcast (specs : DownloadSpecifications, podcast : Podcast) : unit =
    let total =
        podcast.episodes
        |> List.length

    podcast.episodes
    |> List.mapi (fun count episode -> downloadEpisode specs podcast episode (count + 1, total))
    |> ignore

let bulkDownload (specsList : DownloadSpecifications list) =
    specsList
    |> List.map (getFeed >> getPodcast >> createFolder >> downloadPodcast)
    |> ignore
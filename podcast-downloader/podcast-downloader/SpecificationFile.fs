module SpecificationFile

open PodcastDownloader

open System.IO
open Newtonsoft.Json


type PodcastJSON =
    {
        link : string;
        includeNotes : bool;
    }

type TopLevelJSON =
    {
        destination : string;
        podcasts : PodcastJSON list;
    }


let readSpecificationFile () : DownloadSpecifications list =
    
    let contents = File.ReadAllText "../specifications.json"
    let topLevel = JsonConvert.DeserializeObject<TopLevelJSON> contents

    topLevel.podcasts
    |> List.map (fun x -> {
        destination = topLevel.destination;
        includeNotes = x.includeNotes;
        link = x.link;
        })





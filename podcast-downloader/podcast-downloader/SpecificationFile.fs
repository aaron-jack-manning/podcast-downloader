module SpecificationFile

open PodcastDownloader

open System.IO
open Newtonsoft.Json
open System


type PodcastJSON =
    {
        link : string;
        includeNotes : bool;
        dateRange : string; // Formatted "YYYY/MM/DD-YYYY/MM/DD" or "AllTime"
    }

type TopLevelJSON =
    {
        destination : string;
        podcasts : PodcastJSON list;
    }

let parseDate (date : string) (start : bool) =
    match date.Split "/" with
    | [|year; month; day|] ->
        if start then
            DateTime (int year, int month, int day, 0, 0, 0)
        else
            DateTime (int year, int month, int day + 1, 0, 0, 0)
    | _ -> failwith "Invalid date format. Please provide in the format \"YYYY/MM/DD-YYYY/MM/DD\" or specify \"AllTime\"."

let parseDateRange (range : string) =
    if range = "AllTime" then
        AllTime
    else
        match range.Split "-" with
        | [|fromDate; toDate|] ->
            PodcastDownloader.Range (parseDate fromDate true, parseDate toDate false)
        | _ -> failwith "Invalid date format. Please provide in the format \"YYYY/MM/DD-YYYY/MM/DD\" or specify \"AllTime\"."

let readSpecificationFile () : DownloadSpecifications list =
    
    let contents = File.ReadAllText "../specifications.json"
    let topLevel = JsonConvert.DeserializeObject<TopLevelJSON> contents

    topLevel.podcasts
    |> List.map (fun x -> {
        destination = topLevel.destination;
        includeNotes = x.includeNotes;
        link = x.link;
        dateRange = parseDateRange x.dateRange;
        })



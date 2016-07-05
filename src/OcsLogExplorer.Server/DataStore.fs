﻿namespace OcsLogExplorer.Server

module DataStore =
    open System
    open System.Collections.Generic

    let filePaths = new Dictionary<Guid, string>()
    let ulsCache = new Dictionary<string, OCSULS.OcsLogItem[]>() 

    let newPath path =
        let guid = Guid.NewGuid()
        filePaths.Add(guid, path)
        guid;

    let tryGetPath guid =
        let result, path = filePaths.TryGetValue(guid)
        match result with
        | true -> Some path
        | false -> None

    let getUls path =
        match ulsCache.TryGetValue path with
        | true, uls -> uls
        | false, _ -> 
            System.Console.WriteLine("Reading ULS from file: " + path)
            let uls = OCSULS.fromFile path
            ulsCache.[path] <- uls
            uls

    let getOverview path =
        getUls path |> OcsLogExplorer.Server.DataExtractors.OcsSessionOverviewExtractor.extract

    let getRequests path ocsSessionId =
        getUls path |> OcsLogExplorer.Server.DataExtractors.extract ocsSessionId



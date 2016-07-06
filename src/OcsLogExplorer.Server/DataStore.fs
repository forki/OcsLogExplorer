namespace OcsLogExplorer.Server

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

    let getUlsFromFile path =
        match ulsCache.TryGetValue path with
        | true, uls -> uls
        | false, _ -> 
            System.Console.WriteLine("Reading ULS from file: " + path)
            let uls = OCSULS.fromFile path
            ulsCache.[path] <- uls
            uls

    let getUls path correlation =
        let uls = getUlsFromFile path
        Console.WriteLine("Extracting correlation data for Correlation:" + correlation.ToString() + " from " + path)
        
        uls
        |> Seq.filter (fun x -> x.LogItem.Correlation = Some correlation && x.LogItem.Level <> ULS.Level.Verbose && x.LogItem.Level <> ULS.Level.VerboseEx)
        |> Seq.map (fun x -> x.LogItem)

    let getOverview path =
        let uls = getUlsFromFile path
        Console.WriteLine("Extracting overview from " + path)
        OcsLogExplorer.Server.DataExtractors.OcsSessionOverviewExtractor.extract uls

    let getRequests path ocsSessionId =
        let uls = getUlsFromFile path
        Console.WriteLine("Extracting requests for OcsSessionId:" + ocsSessionId.ToString() + " from " + path)
        OcsLogExplorer.Server.DataExtractors.extract ocsSessionId uls



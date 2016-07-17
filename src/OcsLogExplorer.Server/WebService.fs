namespace OcsLogExplorer.Server

module WebService =

    open Suave
    open Suave.Operators
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization
    open Newtonsoft.Json.Converters
    open System
    open OcsLogExplorer.Server.OcsDataModel

    // Adds a new mime type to the default map
    let mimeTypes =
      Writers.defaultMimeTypesMap
        @@ (function | ".ttf" -> Writers.mkMimeType "font" false | _ -> None)
        @@ (function | ".woff" -> Writers.mkMimeType "font" false | _ -> None)
        @@ (function | ".woff2" -> Writers.mkMimeType "font" false | _ -> None)

    let webConfig = { defaultConfig with mimeTypesMap = mimeTypes }

    let JSON v =
      let jsonSerializerSettings = new JsonSerializerSettings()
      jsonSerializerSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()
      jsonSerializerSettings.Converters.Add(new IdiomaticDuConverter())
      jsonSerializerSettings.NullValueHandling <- NullValueHandling.Ignore

      JsonConvert.SerializeObject(v, jsonSerializerSettings)
      |> Successful.OK >=> Writers.setMimeType "application/json; charset=utf-8"

    // api/init - generates new pathId and redirects to index.html with the right search value
    let init (ctx: HttpContext) : WebPart =
        let files = ctx.request.files
        match files with
        | [ file ] ->
            let path = file.tempFilePath
            let pathId = DataStore.newPath path
            Redirection.redirect <| sprintf "/?%O" pathId
        | _ -> RequestErrors.BAD_REQUEST "Incorrect number of files uploaded"

    let handlePathRequest handler path =
        let result, pathId = Guid.TryParse path
        match result with
        | true ->
            match DataStore.tryGetPath pathId with
            | Some path ->
                JSON <| handler path
            | None -> RequestErrors.BAD_REQUEST "Unknown pathId"
        | false -> RequestErrors.BAD_REQUEST "pathId should be a guid"

    let handlePathAndOcsSessionIdRequest handler (path, ocsSessionId) =
        let result, pathId = Guid.TryParse path
        let guidResult, ocsSessionId = Guid.TryParse ocsSessionId
        match result, guidResult with
        | true, true ->
            match DataStore.tryGetPath pathId with
            | Some path ->
                JSON <| handler path ocsSessionId
            | None -> RequestErrors.BAD_REQUEST "Unknown pathId"
        | _ -> RequestErrors.BAD_REQUEST "pathId and ocsSessionId should be guids"

    let handlePathAndCorrelationRequest handler (path, correlation) =
        let result, pathId = Guid.TryParse path
        let guidResult, correlation = Guid.TryParse correlation
        match result, guidResult with
        | true, true ->
            match DataStore.tryGetPath pathId with
            | Some path ->
                JSON <| handler path correlation
            | None -> RequestErrors.BAD_REQUEST "Unknown pathId"
        | _ -> RequestErrors.BAD_REQUEST "pathId and correlation should be guids"

    // gets the path, does initial processing on the file and returns json-formatted overview
    let getOverview = handlePathRequest DataStore.getOverview

    // gets the path, extracts request data and returns in json-formated list
    let getRequests = handlePathAndOcsSessionIdRequest DataStore.getRequests

    // gets the path, extracts ULS data for given correlation and returns in json-formatted list
    let getUls = handlePathAndCorrelationRequest DataStore.getUls

    let getContentPath() =
        let exeLocation = System.Reflection.Assembly.GetEntryAssembly().Location
        let fileInfo = new System.IO.FileInfo(exeLocation)
        let directoryName = fileInfo.Directory.FullName
        System.IO.Path.Combine(directoryName, @"content\")

    let app : WebPart =
        choose [
            Filters.GET >=> choose [
                Filters.path "/" >=> Files.file "content/index.html"
                Filters.pathStarts "/" >=> Files.browse (getContentPath()) ]
            Filters.pathStarts "/api/" >=> choose [
                Filters.POST >=> Filters.path "/api/init" >=> context init
                Filters.GET >=> Filters.pathScan "/api/overview/%s" getOverview
                Filters.GET >=> Filters.pathScan "/api/requests/%s/%s" getRequests
                Filters.GET >=> Filters.pathScan "/api/uls/%s/%s" getUls
            ]
            RequestErrors.NOT_FOUND "Page not found." 
        ]


    [<EntryPoint>]
    let Main args =
        let cts = new System.Threading.CancellationTokenSource()

        let startingServer, shutdownServer = startWebServerAsync defaultConfig app
        Async.Start(shutdownServer, cts.Token)

        startingServer |> Async.RunSynchronously |> printfn "started: %A"

        match args |> Array.contains "-noBrowser" with
        | true-> ()
        | false -> System.Diagnostics.Process.Start("http://localhost:8083") |> ignore

        Console.Read() |> ignore

        cts.Cancel()
        1
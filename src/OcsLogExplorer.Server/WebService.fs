namespace OcsLogExplorer.Server

module WebService =

    open Suave
    open Suave.Operators
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization
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

      JsonConvert.SerializeObject(v, jsonSerializerSettings)
      |> Successful.OK >=> Writers.setMimeType "application/json; charset=utf-8"

    // api/init - generates new pathId and redirects to index.html with the right hash value
    let init (ctx: HttpContext) : WebPart =
        let files = ctx.request.files
        match files with
        | [ file ] ->
            let path = file.tempFilePath
            let pathId = DataStore.newPath path
            Redirection.redirect <| sprintf "/#%O" pathId
        | _ -> RequestErrors.BAD_REQUEST "Incorrect number of files uploaded"

    // gets the path, does initial processing on the file and returns json-formatted overview
    let getOverview path =
        let ocsSessionInfo : OcsSession = {
            OcsSessionId = Guid.NewGuid()
            Details =
            {
                OcsClientSessionIds = [ Guid.NewGuid(); Guid.NewGuid() ]
                StartTime = DateTime.UtcNow
                EndTime = DateTime.UtcNow.AddHours(2.)
                Environment = "Dogfood"
                Datacenter = "DF2"
                Application = "PowerPoint"
                RequestDataUrl = "http://location/something"
            }
        }
        JSON ([ ocsSessionInfo ])

//        let result, pathId = Guid.TryParse path
//        match result with
//        | true ->
//            match DataStore.tryGetPath pathId with
//            | Some path ->
//                let overview = DataStore.initFile path
//                JSON overview
//            | None -> RequestErrors.BAD_REQUEST "Unknown pathId"
//        | false -> RequestErrors.BAD_REQUEST "pathId should be a guid"

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
            ]
            RequestErrors.NOT_FOUND "Page not found." 
        ]


    [<EntryPoint>]
    let Main args =
        startWebServer defaultConfig app
        1
namespace OcsLogExplorer.Server

module WebService =

    open Suave
    open Suave.Operators
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization

    // 'a -> WebPart
    let JSON v =
      let jsonSerializerSettings = new JsonSerializerSettings()
      jsonSerializerSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()

      JsonConvert.SerializeObject(v, jsonSerializerSettings)
      |> Successful.OK >=> Writers.setMimeType "application/json; charset=utf-8"

    let init (ctx: HttpContext) : WebPart =
        let files = ctx.request.files
        match files with
        | [ file ] ->
            let path = file.tempFilePath
            Redirection.redirect <| sprintf "/?path=%s" path
        | _ -> RequestErrors.BAD_REQUEST "Incorrect number of files uploaded"

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
            ]
            RequestErrors.NOT_FOUND "Page not found." 
        ]


    [<EntryPoint>]
    let Main args =
        startWebServer defaultConfig app
        1
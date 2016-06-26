namespace OcsLogExplorer.Server

module WebService =

    open Suave
    open Suave.Operators

    let app : WebPart =
      choose [
        Filters.GET >=> choose [ Filters.path "/content" >=> Files.file "index.html"; Files.browseHome ]
        RequestErrors.NOT_FOUND "Page not found." 
        ]


    [<EntryPoint>]
    let Main args =
        startWebServer defaultConfig app
        1
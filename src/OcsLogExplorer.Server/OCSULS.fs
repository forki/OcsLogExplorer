namespace OcsLogExplorer.Server

module OCSULS =

    open System
    open OcsLogExplorer.Server.ULS

    type OcsCorrelationMapping = {
        OcsClientSessionId: Guid option
        OcsSessionId: Guid option
    }

    type OcsLogItem = {
        LogItem: LogItem
        CorrelationMapping: OcsCorrelationMapping
    }

    let emptyCorrelationMapping = { OcsClientSessionId = None; OcsSessionId = None }

    let getCorrelationMappingForCorrelation xmnvs =
        let updateMapping (mapping:OcsCorrelationMapping) xmnv =
            let parts = xmnv.Message.Split([|'='|])
            match parts with
            | [| name; value |] ->
                let idParseSuccess, id = Guid.TryParse(value)
                match idParseSuccess with
                | true ->
                    match name with
                    | "OcsSessionID" -> { mapping with OcsSessionId = Some id }
                    | "OcsClientSessionID" -> { mapping with OcsClientSessionId = Some id }
                    | _ -> mapping
                | _ -> mapping
            | _ -> mapping
        xmnvs |> Seq.fold updateMapping emptyCorrelationMapping

    let generateCorrelationMapping logs =
        let xmnv = logs |> Seq.filter (fun x -> x.EventID = "xmnv")
        let groupedByCorrelation = xmnv |> Seq.groupBy (fun x -> x.Correlation)
        let correlationMappings =
            groupedByCorrelation |> Seq.map (fun (correlation, xmnvs) -> correlation, getCorrelationMappingForCorrelation xmnvs)
        correlationMappings |> Map

    let getOcsLogItem (mappings: Map<Guid option, OcsCorrelationMapping>) logItem =
        match mappings.TryFind logItem.Correlation with
        | Some mapping -> { LogItem = logItem; CorrelationMapping = mapping }
        | None -> { LogItem = logItem; CorrelationMapping = emptyCorrelationMapping }

    let fromFile path =
        let uls = ULS.fromFile path |> Seq.toArray
        let correlationMappings = uls |> generateCorrelationMapping
        uls |> Array.map (fun log -> getOcsLogItem correlationMappings log)

    let toFile path logs =
        logs |> Seq.map(fun x -> x.LogItem) |> ULS.toFile path
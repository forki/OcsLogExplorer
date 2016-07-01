namespace OcsLogExplorer.Server

module DataExtractors =

    open OcsLogExplorer.Server.OcsDataModel
    open System

    let getEnvironment = function 
    | "DF1" | "DF2" -> "Dogfood"
    | "PP1" | "PP2" -> "PPE"
    | _ -> "Production"

    module OcsSessionOverviewExtractor =

        // Generated [EndpointName=InternalPowerPointJoinRequestHandler] Front-end [Url=http://DF2-powerpoint-collab.officeapps-df.live.com/ocs/Execute.ashx?app=powerpoint]
        let parseBihio (message: string) =
            let endpointNameStart = message.IndexOf("EndpointName=") + "EndpointName=".Length
            let endpointNameEnd = message.IndexOf("]", endpointNameStart)
            let executeUrlStart = message.IndexOf("[Url=", endpointNameEnd) + "[Url=".Length
            let executeUrlEnd = message.Length - 1
            match endpointNameStart, endpointNameEnd, executeUrlStart, executeUrlEnd with
            | -1, -1, -1, -1 -> None
            | _ -> 
                let endpointName = message.Substring(endpointNameStart, endpointNameEnd - endpointNameStart)
                let executeUrl = message.Substring(executeUrlStart, executeUrlEnd - executeUrlStart)
                Some (endpointName, executeUrl)

        let parseDatacenter (executeUrl:string) =
            let start = executeUrl.IndexOf("//") + "//".Length
            let hypen = executeUrl.IndexOf("-", start)
            match start, hypen with
            | -1, -1 -> None
            | _ -> Some (executeUrl.Substring(start, hypen - start))

        let parseApplication (endpointName: string) =
            let start = endpointName.IndexOf("Internal") + "Internal".Length
            let last = endpointName.IndexOf("JoinRequestHandler")
            match start, last with
            | -1, -1 -> None
            | _ -> Some (endpointName.Substring(start, last - start))

        let private extractApplicationAndDatacenter (uls: seq<OCSULS.OcsLogItem>) =
            let bihio = uls |> Seq.filter (fun x -> x.LogItem.EventID = "bihio") |> Seq.head
            match parseBihio bihio.LogItem.Message with
            | Some (endpointName, executeUrl) -> 
                match (parseApplication endpointName), (parseDatacenter executeUrl) with
                | Some application, Some datacenter -> Some application, Some datacenter
                | _ -> None, None
            | None -> None, None

        let extract (uls: OCSULS.OcsLogItem[]) =
            uls
            |> Seq.ofArray
            |> Seq.groupBy (fun uls -> uls.CorrelationMapping.OcsSessionId)
            |> Seq.map (fun (g, uls) ->
                let ocsClientSessions =
                    uls
                    |> Seq.map (fun u -> u.CorrelationMapping.OcsClientSessionId)
                    |> Seq.filter (fun u -> u.IsSome)
                    |> Seq.map (fun id -> match id with | Some guid -> guid | None -> Guid.Empty)
                    |> Seq.distinct
                    |> Seq.toList

                let application, datacenter = extractApplicationAndDatacenter uls

                {
                    OcsSessionId = match g with | Some guid -> guid | None -> Guid.Empty
                    Details =
                    {
                        OcsClientSessionIds = ocsClientSessions
                        StartTime = uls |> Seq.map (fun x -> x.LogItem.TimeStamp) |> Seq.min
                        EndTime = uls |> Seq.map (fun x -> x.LogItem.TimeStamp) |> Seq.max
                        Environment = match datacenter with | Some dc -> getEnvironment dc | None -> ""
                        Datacenter = match datacenter with | Some dc -> dc | None -> ""
                        Application = match application with | Some app -> app | None -> ""
                    }
                })
            |> Seq.toArray



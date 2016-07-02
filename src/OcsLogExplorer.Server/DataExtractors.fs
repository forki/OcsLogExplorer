namespace OcsLogExplorer.Server

module DataExtractors =

    open Newtonsoft.Json
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
            | -1, _, _, _ | _, -1, _, _ | _ , _, -1, _ | _, _, _, -1-> None
            | _ -> 
                let endpointName = message.Substring(endpointNameStart, endpointNameEnd - endpointNameStart)
                let executeUrl = message.Substring(executeUrlStart, executeUrlEnd - executeUrlStart)
                Some (endpointName, executeUrl)

        let parseDatacenter (executeUrl:string) =
            let start = executeUrl.IndexOf("//") + "//".Length
            let hypen = executeUrl.IndexOf("-", start)
            match start, hypen with
            | -1, _ | _, -1 -> None
            | _ -> Some (executeUrl.Substring(start, hypen - start))

        let parseApplication (endpointName: string) =
            let start = endpointName.IndexOf("Internal") + "Internal".Length
            let last = endpointName.IndexOf("JoinRequestHandler")
            match start, last with
            | -1, _ | _, -1 -> None
            | _ -> Some (endpointName.Substring(start, last - start))

        let private extractApplicationAndDatacenter (uls: seq<OCSULS.OcsLogItem>) =
            let bihio = uls |> Seq.filter (fun x -> x.LogItem.EventID = "bihio") |> Seq.head
            match parseBihio bihio.LogItem.Message with
            | Some (endpointName, executeUrl) ->
                let application = match (parseApplication endpointName) with | Some application -> application | None -> ""
                let datacenter = match (parseDatacenter executeUrl) with | Some datacenter -> datacenter | None -> ""
                (application, datacenter)
            | None -> ("", "")

        let private getOcsClientSessions (uls: seq<OCSULS.OcsLogItem>) =
            uls
            |> Seq.map (fun u -> u.CorrelationMapping.OcsClientSessionId)
            |> Seq.filter (fun u -> u.IsSome)
            |> Seq.map (fun id -> match id with | Some guid -> guid | None -> Guid.Empty)
            |> Seq.distinct
            |> Seq.toArray

        let private extractDetails (key, uls) =
            let ocsClientSessions = getOcsClientSessions uls
            let application, datacenter = extractApplicationAndDatacenter uls
            {
                OcsSessionId = match key with | Some guid -> guid | None -> Guid.Empty
                Details =
                {
                    OcsClientSessionIds = ocsClientSessions
                    StartTime = uls |> Seq.map (fun x -> x.LogItem.TimeStamp) |> Seq.min
                    EndTime = uls |> Seq.map (fun x -> x.LogItem.TimeStamp) |> Seq.max
                    Environment = getEnvironment datacenter
                    Datacenter = datacenter
                    Application = application
                }
            }

        let extract (uls: OCSULS.OcsLogItem[]) =
            uls
            |> Seq.ofArray
            |> Seq.groupBy (fun uls -> uls.CorrelationMapping.OcsSessionId)
            |> Seq.map extractDetails
            |> Seq.toArray

    module OuterLoopRequestsDataExtractor =

        let groupByCorrelation (uls: seq<OCSULS.OcsLogItem>) =
            uls |> Seq.filter (fun u -> Option.isSome u.LogItem.Correlation) |> Seq.groupBy (fun u -> u.LogItem.Correlation)

        let tryFindTag (uls: seq<OCSULS.OcsLogItem>) eventId = uls |> Seq.tryFind (fun u -> u.LogItem.EventID = eventId)

        type private bi00jData = { RequestType:string; StatusCode: string }

        let parseStatusCodeFromBi00jMessage (bi00j: string) =
            // SendOuterLoopRequestAsync returned invalid StatusCode: {"RequestType": "StartSession", "StatusCode": "ServerError"}
            let json = bi00j.Substring(bi00j.IndexOf('{'))
            let data = JsonConvert.DeserializeAnonymousType(json, {RequestType="requestType"; StatusCode="OK"})
            let result, value = Enum.TryParse<System.Net.HttpStatusCode>(data.StatusCode)
            match result with
            | true -> Some (int value)
            | false -> None

        let parseMethodFromBf4yrMessage (bf4yr: string) =
            let requestType = bf4yr.Substring(bf4yr.LastIndexOf('=') + 1)
            match requestType with
            | "startSession" -> "StartSession"
            | "synchronize" -> "Synchronize"
            | _ -> ""

        let parseMethodFromBf4yr (bf4yr: OCSULS.OcsLogItem) = parseMethodFromBf4yrMessage bf4yr.LogItem.Message

        let parseStatusCodeFromBi00j (bi00j: OCSULS.OcsLogItem) = parseStatusCodeFromBi00jMessage bi00j.LogItem.Message

        let parseOuterLoopStatusCode (uls: OCSULS.OcsLogItem) =
            match uls.LogItem.EventID with
            | "bj67v" -> Some 200
            | "bi00j" -> parseStatusCodeFromBi00j uls
            | _ -> None

        let rec extractOuterLoopResult (uls: OCSULS.OcsLogItem list) (result: Request) =
            match uls with
            | head :: tail ->
                match head.LogItem.EventID with
                | "bf4yp" | "bgcgg" -> { result with Result = Some true }
                | "bf4yo" | "bgcgh" -> { result with Result = Some false }
                | _ -> extractOuterLoopResult tail result
            | [] -> result

        let rec extractOuterLoopStatusCode (uls: OCSULS.OcsLogItem list) result =
            match uls with
            | head :: tail ->
                match head.LogItem.EventID with
                | "bj67v" | "bi00j" | "bi00k" -> 
                    let result = { result with EndTime = Some head.LogItem.TimeStamp; StatusCode = parseOuterLoopStatusCode head }
                    extractOuterLoopResult tail result
                | _ -> extractOuterLoopStatusCode tail result
            | [] -> result

        let private extractOuterLoopRequest (uls: seq<OCSULS.OcsLogItem>) =
            let tryFindTag = tryFindTag uls

            // Sending a request to OL on {url}
            let bf4yr = tryFindTag "bf4yr"
            match bf4yr with 
            | Some bf4yr ->
                let result = {
                    StartTime = bf4yr.LogItem.TimeStamp
                    EndTime = None
                    Correlation = Option.get bf4yr.LogItem.Correlation
                    OcsClientSessionId = bf4yr.CorrelationMapping.OcsClientSessionId
                    Method = parseMethodFromBf4yr bf4yr
                    StatusCode = None
                    Result = None
                }
                let result = extractOuterLoopStatusCode (uls |> Seq.toList) result
                Some result
            | None -> None

        let extract ocsSessionId (uls: OCSULS.OcsLogItem[]) =
            let groupedByCorrelation =
                uls
                |> Seq.filter (fun u -> match u.CorrelationMapping.OcsSessionId with | Some guid when guid = ocsSessionId -> true | _ -> false)
                |> groupByCorrelation
            let outerLoopRequests = groupedByCorrelation
                                    |> Seq.map (fun (_, uls) -> extractOuterLoopRequest uls)
                                    |> Seq.filter (fun x -> Option.isSome x)
                                    |> Seq.map (fun x -> Option.get x)
            outerLoopRequests



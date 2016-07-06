namespace OcsLogExplorer.Server

module DataExtractors =

    open Newtonsoft.Json
    open OcsLogExplorer.Server.OcsDataModel
    open OcsLogExplorer.Server.OCSULS
    open System

    let getEnvironment = function 
    | "DF1" | "DF2" -> "Dogfood"
    | "PP1" | "PP2" -> "PPE"
    | _ -> "Production"

    let groupByCorrelation uls =
        uls |> Seq.filter (fun u -> Option.isSome u.LogItem.Correlation) |> Seq.groupBy (fun u -> u.LogItem.Correlation)

    let tryFindTag uls eventId = uls |> Seq.tryFind (fun u -> u.LogItem.EventID = eventId)

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

        let private extractApplicationAndDatacenter uls =
            let bihio = tryFindTag uls "bihio"
            match bihio with
            | Some bihio ->
                match parseBihio bihio.LogItem.Message with
                | Some (endpointName, executeUrl) ->
                    let application = match (parseApplication endpointName) with | Some application -> application | None -> ""
                    let datacenter = match (parseDatacenter executeUrl) with | Some datacenter -> datacenter | None -> ""
                    (application, datacenter)
                | None -> ("", "")
            | None -> ("", "")

        let private getOcsClientSessions uls =
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

        let extract uls =
            uls
            |> Seq.ofArray
            |> Seq.groupBy (fun uls -> uls.CorrelationMapping.OcsSessionId)
            |> Seq.filter (fun (key, _) -> match key with | Some key -> key <> Guid.Empty | None -> false)
            |> Seq.map extractDetails
            |> Seq.toArray

    module private OuterLoopRequestsDataExtractor =

        let parseStatusCodeFromBi00jMessage (bi00j: string) =
            // SendOuterLoopRequestAsync returned invalid StatusCode: {"RequestType": "StartSession", "StatusCode": "ServerError"}
            let json = bi00j.Substring(bi00j.IndexOf('{')).Replace("\"\"", "\"").TrimEnd('"')
            let jObject = Newtonsoft.Json.Linq.JObject.Parse json
            let statusCode = jObject.["StatusCode"] |> string
            let result, value = Enum.TryParse<System.Net.HttpStatusCode>(statusCode)
            match result with
            | true -> Some (int value)
            | false -> None

        let parseMethodFromBf4yrMessage (bf4yr: string) =
            let requestType = bf4yr.Substring(bf4yr.LastIndexOf('=') + 1)
            match requestType with
            | "startSession" -> "StartSession"
            | "synchronize" -> "Synchronize"
            | _ -> ""

        let parseMethodFromBf4yr bf4yr = parseMethodFromBf4yrMessage bf4yr.LogItem.Message

        let parseStatusCodeFromBi00j bi00j = parseStatusCodeFromBi00jMessage bi00j.LogItem.Message

        let parseOuterLoopStatusCode uls =
            match uls.LogItem.EventID with
            | "bj67v" -> Some 200
            | "bi00j" -> parseStatusCodeFromBi00j uls
            | _ -> None

        let rec extractOuterLoopResult uls result =
            match uls with
            | head :: tail ->
                match head.LogItem.EventID with
                | "bf4yp" | "bgcgg" -> { result with Result = Some true }
                | "bf4yo" | "bgcgh" -> { result with Result = Some false }
                | _ -> extractOuterLoopResult tail result
            | [] -> result

        let rec extractOuterLoopStatusCode uls result =
            match uls with
            | head :: tail ->
                match head.LogItem.EventID with
                | "bj67v" | "bi00j" | "bi00k" -> 
                    let result = { result with EndTime = Some head.LogItem.TimeStamp; StatusCode = parseOuterLoopStatusCode head }
                    extractOuterLoopResult tail result
                | _ -> extractOuterLoopStatusCode tail result
            | [] -> result

        let extractOuterLoopRequest uls =
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

        let extract groupedByCorrelation =
            let outerLoopRequests = groupedByCorrelation
                                    |> Seq.map (fun (_, uls) -> extractOuterLoopRequest uls)
                                    |> Seq.filter (fun x -> Option.isSome x)
                                    |> Seq.map (fun x -> Option.get x)
            outerLoopRequests

    module private InnerLoopRequestsDataExtractor =

        let parseStatusCodeFromBe9x2Message (be9x2: string) =
            // CollabHttpModule.EndRequest {"Target":"url","StatusCode":200,"Duration":44}
            let json = be9x2.Substring(be9x2.IndexOf('{')).Replace("\"\"", "\"").TrimEnd('"')
            let jObject = Newtonsoft.Json.Linq.JObject.Parse json
            jObject.["StatusCode"] |> int

        // replace _ with a space and upparcase first letter of the words (e.g. PUT_BLOBS -> Put Blobs) 
        let formatOverride (overrideName: string) =
            let rec getOverride source shouldUppercase = seq {
                match source with
                | '_' :: tail ->
                    yield ' '
                    yield! getOverride tail true
                | current :: tail ->
                    match shouldUppercase with
                    | true -> yield Char.ToUpper(current)
                    | false -> yield Char.ToLower(current)

                    yield! getOverride tail false
                | [] -> ()
            }
            getOverride (overrideName |> List.ofSeq) true
            |> Seq.toArray
            |> System.String

        let tryGetRequestStart uls =
            // Starting processing a request on {EndpointName} ----- ERH
            tryFindTag uls "be9te" 

        let parseEndpointFromBe9teMessage (be9te: string) =
            let join = be9te.Contains("Join")
            let execute = be9te.Contains("Execute")
            match join, execute with
            | true, false -> Some "JoinSession"
            | false, true -> None
            | _ -> None

        let parseOverrideFromBe9a7Message (be9az: string) =
            let start = be9az.IndexOf("[Override:") + "[Override:".Length
            let last = be9az.IndexOf("]", start)
            match start, last with
            | -1, _ | _, -1 -> "Unknown"
            | _ -> be9az.Substring(start, last - start) |> formatOverride

        let parseMethod requestStart uls =
            let methodFromRequestStart = parseEndpointFromBe9teMessage requestStart.LogItem.Message
            match methodFromRequestStart with
            | Some method -> method
            | None -> 
                // for now lets parse it from other tags
                // but we should add override header value to be9te to make it easier and more reliable
                let be9a7 = tryFindTag uls "be9a7"
                match be9a7 with
                | Some be9a7 ->
                    parseOverrideFromBe9a7Message be9a7.LogItem.Message
                | _ -> "LeaveSession?"

        let parseEndTimeAndStatusCode uls =
            let be9x2 = tryFindTag uls "be9x2"
            match be9x2 with
            | Some be9x2 ->
                let endTime = Some be9x2.LogItem.TimeStamp
                let statusCode = parseStatusCodeFromBe9x2Message be9x2.LogItem.Message
                endTime, Some statusCode
            | None -> None, None

        let extractRequestData uls requestStart =
            let endTime, statusCode = parseEndTimeAndStatusCode uls
            {
                StartTime = requestStart.LogItem.TimeStamp
                EndTime = endTime
                Correlation = Option.get requestStart.LogItem.Correlation
                OcsClientSessionId = requestStart.CorrelationMapping.OcsClientSessionId
                Method = parseMethod requestStart uls
                StatusCode = statusCode
                Result = None
            }

        let extract groupedByCorrelation =
            let requests =
                groupedByCorrelation
                |> Seq.map (fun (_, uls) -> (uls, tryGetRequestStart uls))
                |> Seq.filter (fun (uls, requestStart) -> Option.isSome requestStart)
                |> Seq.map (fun (uls, requestStart) -> (uls, (Option.get requestStart)))

            requests
            |> Seq.map (fun (uls, requestStart) -> extractRequestData uls requestStart)

    let extract ocsSessionId uls =
            let groupedByCorrelation =
                uls
                |> Seq.filter (fun u -> match u.CorrelationMapping.OcsSessionId with | Some guid when guid = ocsSessionId -> true | _ -> false)
                |> groupByCorrelation

            let outerLoop = OuterLoopRequestsDataExtractor.extract groupedByCorrelation
            let innerLoop = InnerLoopRequestsDataExtractor.extract groupedByCorrelation

            Seq.concat [ outerLoop;  innerLoop ]
            |> Seq.sortBy (fun r -> r.StartTime)




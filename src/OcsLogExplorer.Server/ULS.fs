namespace OcsLogExplorer.Server

module ULS =
    open System
    open System.Globalization
    open System.IO

    /// Log.Level enumeration
    type Level =
        | VerboseEx
        | Verbose
        | Medium
        | Monitorable
        | Hight
        | Assert
        | Unexpected

    /// ULS LogItem data - represents a single line from ULS log file
    type LogItem = {
        TimeStamp: DateTime
        Process: string
        Thread: string
        Area: string
        Category: string
        EventID: string
        Level: Level
        Message: string
        Correlation: Guid option
    }

    [<Literal>]
    let timeStampFormat = @"MM/dd/yyyy HH:mm:ss.ff"

    let parseLevel = function
        | "VerboseEx" -> Some Level.VerboseEx
        | "Verbose" -> Some Level.Verbose
        | "Medium" -> Some Level.Medium
        | "Monitorable" -> Some Level.Monitorable
        | "Hight" -> Some Level.Hight
        | "Assert" -> Some Level.Assert
        | "Unexpected" -> Some Level.Unexpected
        | _ -> None

    let writeLevel = function
        | VerboseEx -> "VerboseEx"
        | Verbose -> "Verbose"
        | Medium -> "Medium"
        | Monitorable -> "Monitorable"
        | Hight -> "Hight"
        | Assert -> "Assert"
        | Unexpected -> "Unexpected"

    /// Parses a single ULS-formatted line
    let parseLine (line: string) =
        let parts = line.Split [|'\t'|]
        match parts.Length with
        | 9 ->
            // parse non-string data first
            let timeStampIsValid, timeStamp = DateTime.TryParseExact(parts.[0].Trim(), timeStampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
            let correlationIsValid, correlationValue = Guid.TryParse(parts.[8])
            let correlation =
                match correlationIsValid with
                | true -> Some correlationValue
                | false -> None
            let level = parseLevel (parts.[6].Trim())

            match timeStampIsValid, level with
            | true, Some level ->
                Some {
                    TimeStamp = timeStamp
                    Process = parts.[1].Trim()
                    Thread = parts.[2].Trim()
                    Area = parts.[3].Trim()
                    Category = parts.[4].Trim()
                    EventID = parts.[5].Trim()
                    Level = level
                    Message = parts.[7].Trim()
                    Correlation = correlation
                }
            | a, b ->
                Console.WriteLine(sprintf "%b %s" a line)
                None
        | _ -> None

    let writeCorrelation (correlation: Guid option) =
        match correlation with
        | Some correlation -> correlation.ToString("d")
        | None -> ""

    /// Formats a LogItem in ULS way
    let writeLine logItem =
        sprintf "%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s"
            (logItem.TimeStamp.ToString(timeStampFormat, CultureInfo.InvariantCulture)) logItem.Process
            logItem.Thread logItem.Area logItem.Category logItem.EventID (writeLevel logItem.Level) logItem.Message
            (writeCorrelation logItem.Correlation)

    /// Reads ULS logs from a file in a lazy-evaludated way. 
    let fromFile (path: string) : LogItem seq = seq {
        use sr = new StreamReader(path)
        while not sr.EndOfStream do
            let logItem = sr.ReadLine() |> parseLine
            match logItem with
            | Some logItem -> yield logItem
            | None -> ()
    }

    /// Writes provided LogItems into a file.
    let toFile (path: string) (logs: LogItem seq) =
        use sw = new StreamWriter(path)
        logs |> Seq.iter (fun logItem -> writeLine logItem |> sw.WriteLine)

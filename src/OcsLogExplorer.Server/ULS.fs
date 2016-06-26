namespace OcsLogExplorer.Server

module ULS =
    open System
    open System.Globalization
    open System.IO

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
        Machine: string
        LogIndex: int64
        TimeStamp: DateTime
        Process: string
        Thread: string
        Product: string
        Category: string
        EventID: string
        Level: Level
        Message: string
        Correlation: Guid option
        LineIndex: int64
        FileName: string
    }

    [<Literal>]
    let timeStampFormat = @"dd/MM/yyyy HH:mm:ss.ff"

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
        | 11 ->
            let metadata = parts.[0].Split [|','|]
            match metadata.Length with 
            | 3 ->
                // parse non-string data first
                let logIndexIsValid, logIndex = Int64.TryParse(metadata.[1])
                let timeStampIsValid, timeStamp = DateTime.TryParseExact(metadata.[2].Trim(), timeStampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
                let correlationIsValid, correlationValue = Guid.TryParse(parts.[8])
                let correlation =
                    match correlationIsValid with
                    | true -> Some correlationValue
                    | false -> None
                let lineIndexIsValid, lineIndex = Int64.TryParse(parts.[9])
                let level = parseLevel (parts.[6].Trim())
                match logIndexIsValid, timeStampIsValid, lineIndexIsValid, level with
                | true, true, true, Some level ->
                    Some {
                        Machine = metadata.[0].Trim()
                        LogIndex = logIndex
                        TimeStamp = timeStamp
                        Process = parts.[1].Trim()
                        Thread = parts.[2].Trim()
                        Product = parts.[3].Trim()
                        Category = parts.[4].Trim()
                        EventID = parts.[5].Trim()
                        Level = level
                        Message = parts.[7].Trim()
                        Correlation = correlation
                        LineIndex = lineIndex
                        FileName = parts.[10].Trim()
                    }
                | _, _, _, _ -> None
            | _ -> None
        | _ -> None

    let writeCorrelation (correlation: Guid option) =
        match correlation with
        | Some correlation -> correlation.ToString("d")
        | None -> ""

    /// Formats a LogItem in ULS way
    let writeLine logItem =
        sprintf "%s,%d,%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%d\t%s"
            logItem.Machine logItem.LogIndex (logItem.TimeStamp.ToString(timeStampFormat, CultureInfo.InvariantCulture)) logItem.Process
            logItem.Thread logItem.Product logItem.Category logItem.EventID (writeLevel logItem.Level) logItem.Message
            (writeCorrelation logItem.Correlation) logItem.LineIndex logItem.FileName

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

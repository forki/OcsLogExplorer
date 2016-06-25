namespace OcsLogExplorer.Server

module ULS =
    open System
    open System.IO

    /// ULS LogItem data - represents a single line from ULS log file
    type public LogItem = {
        Machine: string
        LogIndex: int64
        TimeStamp: DateTime
        Process: string
        Thread: string
        Product: string
        Category: string
        EventID: string
        Level: string
        Message: string
        Correlation: Guid
        LineIndex: int64
        FileName: string
    }

    let public parseLine (line: string) =
        let parts = line.Split [|'\t'|]
        match parts.Length with
        | 11 ->
            let metadata = parts.[0].Split [|','|]
            match metadata.Length with 
            | 3 ->
                // parse non-string data first
                let logIndexIsValid, logIndex = Int64.TryParse(metadata.[1])
                let timeStampIsValid, timeStamp = DateTime.TryParseExact(metadata.[2].Trim(), @"dd/MM/yyyy hh:mm:ss.ff", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None)
                let correlationIsValid, correlation = Guid.TryParse(parts.[8])
                let lineIndexIsValid, lineIndex = Int64.TryParse(parts.[9])
                match logIndexIsValid, timeStampIsValid, correlationIsValid, lineIndexIsValid with
                | true, true, true, true ->
                    Some {
                        Machine = metadata.[0].Trim()
                        LogIndex = logIndex
                        TimeStamp = timeStamp
                        Process = parts.[1].Trim()
                        Thread = parts.[2].Trim()
                        Product = parts.[3].Trim()
                        Category = parts.[4].Trim()
                        EventID = parts.[5].Trim()
                        Level = parts.[6].Trim()
                        Message = parts.[7].Trim()
                        Correlation = correlation
                        LineIndex = lineIndex
                        FileName = parts.[10].Trim()
                    }
                | _, _, _, _-> None
            | _ -> None
        | _ -> None
        

        

    /// Reads ULS logs from a file in a lazy-evaludated way. 
    let public fromFile (path: string) : LogItem seq =
        Seq.empty

    /// Writes provided LogItems into a file.
    let public toFile (logs: LogItem seq) (path: string) =
        ()

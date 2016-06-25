namespace OcsLogExplorer.Server

module ULS =
    open System

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
        Level: string
        Message: string
        LineIndex: int64
        FileName: string
    }

    /// Reads ULS logs from a file in a lazy-evaludated way. 
    let public fromFile (path: string) : LogItem seq =
        Seq.empty

    /// Writes provided LogItems into a file.
    let public toFile (logs: LogItem seq) (path: string) =
        ()

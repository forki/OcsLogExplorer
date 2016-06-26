namespace OcsLogExplorer.Server.Tests

open System
open OcsLogExplorer.Server
open OcsLogExplorer.Server.ULS
open NUnit.Framework
open FsUnit
open FsUnit.TopLevelOperators

module ULSTests =

    [<Test>]
    let ``lineParse returns None on invalid input`` () =
        let result = parseLine "something"
        result |> Option.isNone |> should be True

    [<Test>]
    let ``lineParse returns correct LogItem on valid input`` () =
        let line = @"06/18/2016 17:28:37.65 	telemetrywatchdog.exe (0x1CB8)          	0x1CBC	Hosted Services Infrastructure	Services Infrastructure Health	aruwk	Medium  	Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds	0a252638-0a3e-43f6-bf08-3b45178436a7"

        let result = parseLine line

        result |> Option.isSome |> should be True

        match result with 
        | Some logLine ->
            logLine.TimeStamp |> should equal (DateTime(2016, 6, 18, 17, 28, 37, 650, DateTimeKind.Utc))
            logLine.Process |> should equal "telemetrywatchdog.exe (0x1CB8)"
            logLine.Thread |> should equal "0x1CBC"
            logLine.Area |> should equal "Hosted Services Infrastructure"
            logLine.Category |> should equal "Services Infrastructure Health"
            logLine.EventID |> should equal "aruwk"
            logLine.Level |> should equal Level.Medium
            logLine.Message |> should equal @"Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds"
            logLine.Correlation |> should equal (Some (Guid("0a252638-0a3e-43f6-bf08-3b45178436a7")))
        | None -> ()

    [<Test>]
    let ``lineParse returns correct LogItem on valid input even without Correlation`` () =
        let line = @"06/18/2016 17:28:37.65 	telemetrywatchdog.exe (0x1CB8)          	0x1CBC	Hosted Services Infrastructure	Services Infrastructure Health	aruwk	Medium  	Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds	 "

        let result = parseLine line

        result |> Option.isSome |> should be True

        match result with 
        | Some logLine ->
            logLine.TimeStamp |> should equal (DateTime(2016, 6, 18, 17, 28, 37, 650, DateTimeKind.Utc))
            logLine.Process |> should equal "telemetrywatchdog.exe (0x1CB8)"
            logLine.Thread |> should equal "0x1CBC"
            logLine.Area |> should equal "Hosted Services Infrastructure"
            logLine.Category |> should equal "Services Infrastructure Health"
            logLine.EventID |> should equal "aruwk"
            logLine.Level |> should equal Level.Medium
            logLine.Message |> should equal @"Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds"
            logLine.Correlation |> should equal None
        | None -> ()

    [<Test>]
    let ``lineParse returns None for unknown Log.Level value`` () =
        let line = @"06/18/2016 17:28:37.65 	telemetrywatchdog.exe (0x1CB8)          	0x1CBC	Hosted Services Infrastructure	Services Infrastructure Health	aruwk	BadLevel  	Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds	0a252638-0a3e-43f6-bf08-3b45178436a7"

        let result = parseLine line

        result |> Option.isNone |> should be True

    [<Test>]
    let ``writeLine returns correct string`` () =
        let logItem = {
            TimeStamp = (DateTime(2016, 6, 18, 17, 28, 37, 650, DateTimeKind.Utc))
            Process = "telemetrywatchdog.exe (0x1CB8)"
            Thread = "0x1CBC"
            Area = "Hosted Services Infrastructure"
            Category = "Services Infrastructure Health"
            EventID = "aruwk"
            Level = Level.Medium
            Message = @"Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds"
            Correlation = Some (Guid("0a252638-0a3e-43f6-bf08-3b45178436a7"))
        }

        let line = writeLine logItem

        let expected = @"06/18/2016 17:28:37.65	telemetrywatchdog.exe (0x1CB8)	0x1CBC	Hosted Services Infrastructure	Services Infrastructure Health	aruwk	Medium	Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds	0a252638-0a3e-43f6-bf08-3b45178436a7"
        line |> should equal expected

    [<Test>]
    let ``writeLine returns correct string even without Correlaiton`` () =
        let logItem = {
            TimeStamp = (DateTime(2016, 6, 18, 17, 28, 37, 650, DateTimeKind.Utc))
            Process = "telemetrywatchdog.exe (0x1CB8)"
            Thread = "0x1CBC"
            Area = "Hosted Services Infrastructure"
            Category = "Services Infrastructure Health"
            EventID = "aruwk"
            Level = Level.Medium
            Message = @"Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds"
            Correlation = None
        }

        let line = writeLine logItem

        let expected = @"06/18/2016 17:28:37.65	telemetrywatchdog.exe (0x1CB8)	0x1CBC	Hosted Services Infrastructure	Services Infrastructure Health	aruwk	Medium	Read sleep interval setting (""WatchdogSleepDelayInSeconds"") from configuration store, value = 240 seconds	"
        line |> should equal expected




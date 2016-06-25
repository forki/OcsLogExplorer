namespace OcsLogExplorer.Server.Tests

open System
open OcsLogExplorer.Server
open NUnit.Framework
open FsUnit
open FsUnit.TopLevelOperators

module ULSTests =

    [<Test>]
    [<Ignore>]
    let ``lineParse returns None on invalid input`` () =
        let result = OcsLogExplorer.Server.ULS.parseLine "something"
        result |> Option.isNone |> should be True

    [<Test>]
    let ``lineParse returns correct LogItem on valid input`` () =
        let line = "WT1WACWEB134,190442935,04/01/2016 00:42:37.83 	w3wp.exe (0x3324)                       	0x24B0	Hosted Services Infrastructure	Services Infrastructure Health	a9mkg	Verbose 	[WacCore] is running and was started at [03/31/2016 21:41:04].	9009a7d3-8120-43ee-9c80-ddd570873310	2202	E:\Data\Logs\ULS\MineCart\WT1WACWEB134-20160401-0042.log"

        let result = OcsLogExplorer.Server.ULS.parseLine line

        result |> Option.isSome |> should be True

        match result with 
        | Some logLine ->
            logLine.Machine |> should equal "WT1WACWEB134"
            logLine.LogIndex |> should equal 190442935L
            logLine.TimeStamp |> should equal (new DateTime(2016, 1, 4, 0, 42, 37, 830))
            logLine.Process |> should equal "w3wp.exe (0x3324)"
            logLine.Thread |> should equal "0x24B0"
            logLine.Product |> should equal "Hosted Services Infrastructure"
            logLine.Category |> should equal "Services Infrastructure Health"
            logLine.EventID |> should equal "a9mkg"
            logLine.Level |> should equal "Verbose"
            logLine.Message |> should equal "[WacCore] is running and was started at [03/31/2016 21:41:04]."
            logLine.Correlation |> should equal (new Guid("9009a7d3-8120-43ee-9c80-ddd570873310"))
            logLine.LineIndex |> should equal 2202L
            logLine.FileName |> should equal "E:\Data\Logs\ULS\MineCart\WT1WACWEB134-20160401-0042.log"
        | None -> ()




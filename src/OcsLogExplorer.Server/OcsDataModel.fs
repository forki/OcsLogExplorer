﻿namespace OcsLogExplorer.Server

module OcsDataModel =

    open System

    type Request = {
        Correlation: Guid
        StartTime: DateTime
        EndTime: DateTime option
        Duration: int64 option
        TargetUrl: string option
        StatusCode: int option
    }

    type JoinSessionResult =
        | NewSessionCreated
        | JoinedExistingSession
        | Failure

    type JoinSession = {
        Request: Request
        Result: JoinSessionResult
        DidDatacenterRedirectionHappen: bool
    }

    type OcsClientSession = {
        OcsClientSessionId: Guid
        JoinSession: JoinSession
    }

    type OcsSessionData = {
        startTime: DateTime
        endTime: DateTime option
        OcsSessionId: Guid
        ClientSessions: OcsClientSession list
    }

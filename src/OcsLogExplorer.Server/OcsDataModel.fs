namespace OcsLogExplorer.Server

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
        ExecuteEndpointUrl: string
    }

    type OcsClientSession = {
        OcsClientSessionId: Guid
        JoinSession: JoinSession
    }

    type OcsSessionDetails = {
        OcsClientSessionIds: Guid list
        StartTime: DateTime
        EndTime: DateTime
        Environment: string
        Datacenter: string
        Application: string
        RequestDataUrl: string
    }

    type OcsSession = {
        OcsSessionId: Guid
        Details: OcsSessionDetails
    }


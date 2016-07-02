namespace OcsLogExplorer.Server

module OcsDataModel =

    open System

    type OcsSessionDetails = {
        OcsClientSessionIds: Guid[]
        StartTime: DateTime
        EndTime: DateTime
        Environment: string
        Datacenter: string
        Application: string
    }

    type OcsSession = {
        OcsSessionId: Guid
        Details: OcsSessionDetails
    }

    type Request = {
        StartTime: DateTime
        EndTime: DateTime option
        Correlation: Guid
        OcsClientSessionId: Guid option
        Method: string
        StatusCode: int option
        Result: bool option
    }
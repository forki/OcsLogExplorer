namespace OcsLogExplorer.Server

module DataStore =
    open System
    open System.Collections.Generic

    let filePaths = new Dictionary<Guid, string>();

    let newPath path =
        let guid = Guid.NewGuid()
        filePaths.Add(guid, path)
        guid;

    let tryGetPath guid =
        let result, path = filePaths.TryGetValue(guid)
        match result with
        | true -> Some path
        | false -> None


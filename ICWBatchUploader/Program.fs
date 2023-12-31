﻿
module ICWBatchUploader.MainModule
    open System
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers

    let mutable config:ParseArgs.CommandLineOptions = ParseArgs.defaults

    let rec scanDir (path: string) : array<string> =
        let mutable newFiles = [||]

        try
            newFiles <- System.IO.Directory.GetFiles(path)

            printfn $"Scanned Directory {path} - files: {newFiles.Length}"

            let dirList = System.IO.Directory.GetDirectories(path)

            if dirList.Length > 0 then

                for d in dirList do
                    let subDirFiles = scanDir d
                    newFiles <- Array.append subDirFiles newFiles

            newFiles
        with error ->
            printfn $"Couldn't Read Directory {path}: {error.Message}"
            newFiles

    let tryDeleteFile (path:string):bool =
        try
            System.IO.File.Delete(path)
            true
        with error -> printfn $"Unable to delete file {error.Message}"; false
        
        
    let toLower (inStr:string):string =
        // note for me - the () allows the compiler to figure out the return type for tolower
        // otherwise there are two methods and it gets confused
        inStr.ToLower()
    
    let canIngest (filepath:String):Boolean =
        match toLower (System.IO.Path.GetExtension filepath) with
        | ".jpeg" -> true
        | ".jpg" -> true
        | ".gif" -> true
        | ".jpeg_large" -> true
        | ".png" -> true
        | ".webp" -> true
        | ".jfif" -> true
        | _ -> false

    let createFileHttpContent (path:string):MultipartFormDataContent =
        let file = System.IO.File.OpenRead(path)
        let content = new MultipartFormDataContent()
        let fileContent = new StreamContent(file)
        fileContent.Headers.ContentType <- MediaTypeHeaderValue.Parse("multipart/form-data")
        content.Add(fileContent, "file", path)
        content
    
    let triggerProcImage =
        async {
            use client = new HttpClient()
            let content = new MultipartFormDataContent()
            let constructUrl:string =
                match config.secure with
                | true -> $"https://{config.icwHostName}:{config.icwPort}/api_procimage"
                | false -> $"http://{config.icwHostName}:{config.icwPort}/api_procimage"
            try
                let! _ = Async.AwaitTask(client.PostAsync(constructUrl, content))
                printfn("Running Procimage")
                ()
            with error -> printfn $"%s{error.Message}"
        }
    
    let uploadFile path =
        async {
            use fileContent = createFileHttpContent path
            use client = new HttpClient()
            
            // if we don't construct this function in here the compiler will compile in the initial values
            // TODO: Do this more elegantly
            
            let constructUrl:string =
                match config.secure with
                | true -> $"https://{config.icwHostName}:{config.icwPort}/api_uploadfile"
                | false -> $"http://{config.icwHostName}:{config.icwPort}/api_uploadfile"
            
            try
                let! response = Async.AwaitTask(client.PostAsync(constructUrl, fileContent))
                printfn $"{path} - {response.StatusCode}"
                match response.StatusCode with
                | HttpStatusCode.OK ->
                    match config.deleteFilesAfterUpload with
                    | true -> 
                        match tryDeleteFile(path) with
                        | true -> printfn "File Deleted"
                        | false -> printfn "Error: File Not Deleted"
                    | false -> ()
                | _ -> ()
            with error -> printfn $"%s{error.Message}"
            
        }
    

    
    [<EntryPoint>]
    let main args =
        let parsedArgs = ParseArgs.parseArgs (args |> Array.toList)
        config <- parsedArgs
        printfn $"Uploading files from: \n{config.directoryToUpload}\nTo: ICW Host {config.icwHostName}"
        
        let willWont =
            match config.deleteFilesAfterUpload with
            | true -> "will"
            | false -> "will not"
            
        printfn $"Files {willWont} be deleted after they are successfully uploaded"
        printfn $"{config}"
        
        
        // If you don't reverse pipe array.append prepends the parameter to the array meaning procimage runs first
        // which means images don't get ingested
        scanDir config.directoryToUpload
        |> Array.filter canIngest
        |> Array.map uploadFile
        |> Array.append <| [|triggerProcImage|]  
        |> Async.Sequential
        |> Async.Ignore
        |> Async.RunSynchronously
        
        
        0
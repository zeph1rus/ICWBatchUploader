

module ICWBatchUploader.ParseArgs

    type CommandLineOptions = {
        deleteFilesAfterUpload: bool;
        icwHostName: string;
        directoryToUpload: string;
        icwPort: string;
        secure: bool;
    }
    
    let defaults = {
            deleteFilesAfterUpload = false;
            icwHostName = "127.0.0.1";
            directoryToUpload = ".";
            icwPort = "5000"
            secure = false;
        }
       
    let rec parseOptions args workingParse =
        match args with
        | [] ->
            workingParse
        | "--directory"::nextArgs ->
            let newWorkingParse = { workingParse with directoryToUpload = nextArgs.Head }
            parseOptions nextArgs newWorkingParse
        | "--delete"::nextArgs ->
            let newWorkingParse = { workingParse with deleteFilesAfterUpload = true }
            parseOptions nextArgs newWorkingParse
        | "--secure"::nextArgs ->
            let newWorkingParse = { workingParse with secure =  true }
            parseOptions nextArgs newWorkingParse
        | "--host"::nextArgs ->
            let newWorkingParse = { workingParse with icwHostName = nextArgs.Head }
            parseOptions nextArgs newWorkingParse
        | "--port"::nextArgs ->
            let newWorkingParse = { workingParse with icwPort =  nextArgs.Head }
            parseOptions nextArgs newWorkingParse
        | arg::nextArgs ->
            parseOptions nextArgs workingParse


    let parseArgs args =
        parseOptions args defaults
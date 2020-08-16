![Icon](https://raw.github.com/kzu/dotnet-config/master/docs/img/icon-32.png) dotnet-file
============

A dotnet global tool for downloading and updating loose files from arbitrary URLs.

[![Build Status](https://dev.azure.com/kzu/oss/_apis/build/status/dotnet-file?branchName=master)](https://dev.azure.com/kzu/oss/_build/latest?definitionId=35&branchName=master)
[![Version](https://img.shields.io/nuget/v/dotnet-file.svg?color=royalblue)](https://www.nuget.org/packages/dotnet-file)
[![Downloads](https://img.shields.io/nuget/dt/dotnet-file.svg?color=darkmagenta)](https://www.nuget.org/packages/dotnet-file)
[![License](https://img.shields.io/github/license/kzu/dotnet-file.svg?color=blue)](https://github.com/kzu/dotnet-file/blob/master/LICENSE)

Installing or updating (same command can be used for both):

```
dotnet tool update -g dotnet-file
```

To get the CI version:

```
dotnet tool update -g dotnet-file --no-cache --add-source https://pkg.kzu.io/index.json
```

Usage:

    dotnet file [add|changes|delete|list|update] [file or url]*
        = <- [url]        remote file equals local file
        √ <- [url]        local file updated with remote file
        ^ <- [url]        remote file is newer (ETags mismatch)
        ? <- [url]        local file not found for remote file
        x <- [url]        error processing the entry

All arguments after the action are tried for URL parsing automatically to 
disambiguate file (local path) from url (remote file location).

Examples:

    dotnet file add [url]           // downloads a file to the current directory and adds its URL+ETag in dotnet-config
    dotnet file add [url] [file]    // downloads the url to the (relative) relative file local path specifed and adds
                                    // its URL+ETag in dotnet-config
    dotnet file update [file]       // updates a specific file, based on its dotnet-config configuration
    dotnet file update [url]        // updates a specific file by its url, based on its dotnet-config configuration
    dotnet file update              // updates all recorded files, according to the dotnet-config configuration
    dotnet file delete [file]       // deletes a local file and its entry in .netconfig
    dotnet file list                // lists all configured files
    dotnet file changes             // lists all configured files and their status with regards to the configured 
                                    // remote URL and ETag matching

After downloading a file, a new entry is created in a local `.netconfig` file, which
leverages [dotnet config](https://github.com/kzu/dotnet-config):

    [file "relative file path"]
      url = [url]
      etag = [etag]

This information is used to later update the file contents if necessary, by issuing a 
conditional http get to retrieve updates. It’s generally advisable to commit the .netconfig file 
to source control, so that updating is simply a matter of running `dotnet file update`. 

> Note: `dotnet file update [url]` behaves just like `dotnet file download [url]` when a matching 
> entry for the file isn't found in the `.netconfig` file.

Symbols are used to denote actions (pending or performed) on files:

* `√`: file has no pending updated (ETag matches) or it was just downloaded successfully.
* `^`: file has pending updates (ETag doesn't match the remote).
* `=`: no changes to file were necessary in an update
* `?`: file not found locally. A new version can be downloaded from the remote.
* `x`: could not update file or refresh ETag status, with reason noted in subsequent line.

Downloading entire repositories or specific directories within them is supported through the 
[GitHub CLI](https://cli.github.com/manual/installation).

Concrete examples:

    > dotnet file add https://github.com/kzu/dotnet-file/blob/master/azure-pipelines.yml
    azure-pipelines.yml √ <- https://github.com/kzu/dotnet-file/blob/master/azure-pipelines.yml

    > dotnet file add https://github.com/kzu/dotnet-file/blob/master/docs/img/icon.png img/icon.png
    img/icon.png √ <- https://github.com/kzu/dotnet-file/blob/master/docs/img/icon.png

    > dotnet file list
    azure-pipelines.yml = <- https://github.com/kzu/dotnet-file/raw/master/azure-pipelines.yml
    img/icon.png        = <- https://github.com/kzu/dotnet-file/blob/master/docs/img/icon.png

    > del img\icon.png
    > dotnet file list
    azure-pipelines.yml = <- https://github.com/kzu/dotnet-file/raw/master/azure-pipelines.yml
    img/icon.png        ? <- https://github.com/kzu/dotnet-file/blob/master/docs/img/icon.png

    ; missing file downloaded successfully
    > dotnet file update
    azure-pipelines.yml = <- https://github.com/kzu/dotnet-file/raw/master/azure-pipelines.yml
    img/icon.png        √ <- https://github.com/kzu/dotnet-file/blob/master/docs/img/icon.png

    ; file updated on remote, changes detected
    > dotnet file changes
    azure-pipelines.yml ^ <- https://github.com/kzu/dotnet-file/raw/master/azure-pipelines.yml
    img/icon.png        = <- https://github.com/kzu/dotnet-file/blob/master/docs/img/icon.png

    ; file renamed or deleted from remote
    > dotnet file changes
    azure-pipelines.yml = <- https://github.com/kzu/dotnet-file/raw/master/azure-pipelines.yml
    img/icon.png        x <- https://github.com/kzu/dotnet-file/blob/master/docs/img/icon.png
                             404: Not Found

    > dotnet file add https://github.com/dotnet/runtime/tree/master/docs/coding-guidelines/api-guidelines
    api-guidelines  => fetching via gh cli...
    docs/coding-guidelines/api-guidelines/README.md        √ <- https://raw.githubusercontent.com/dotnet/runtime/master/docs/coding-guidelines/api-guidelines/README.md
    docs/coding-guidelines/api-guidelines/System.Memory.md √ <- https://raw.githubusercontent.com/dotnet/runtime/master/docs/coding-guidelines/api-guidelines/System.Memory.md
    docs/coding-guidelines/api-guidelines/nullability.md   √ <- https://raw.githubusercontent.com/dotnet/runtime/master/docs/coding-guidelines/api-guidelines/nullability.md

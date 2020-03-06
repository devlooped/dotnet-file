![Icon](https://raw.github.com/kzu/dotnet-config/master/docs/img/icon-32.png) dotnet-file
============

A dotnet global tool for downloading and updating loose files from arbitrary URLs.

[![Build Status](https://dev.azure.com/kzu/oss/_apis/build/status/dotnet-file?branchName=master)](https://dev.azure.com/kzu/oss/_build/latest?definitionId=35&branchName=master)
[![Version](https://img.shields.io/nuget/v/dotnet-file.svg)](https://www.nuget.org/packages/dotnet-file)
[![Downloads](https://img.shields.io/nuget/dt/dotnet-file.svg)](https://www.nuget.org/packages/dotnet-file)
[![License](https://img.shields.io/github/license/kzu/dotnet-file.svg?color=blue)](https://github.com/kzu/dotnet-file/blob/master/LICENSE)

Installing or updating (same command for both):

```
dotnet tool update -g dotnet-file
```

To get the CI version:

```
dotnet tool update -g dotnet-file --no-cache --add-source https://pkg.kzu.io/index.json
```

Example usage:

    dotnet file download [-u|url=]* [-path=]     // downloads file(s) to given path or the current dir
    dotnet file update [-p|path=]* [-u|url=]*    // updates a specific file(s) or all (if none provided)
    dotnet file delete [-p|path=]* [-u|url=]*    // deletes a file and its entry in .netconfig

After downloading a file, a new entry is created in a local `.netconfig` file, which
leverages [dotnet config](https://github.com/kzu/dotnet-config):

    [file "relative file path"]
      url = [url]
      etag = [etag]

This information is used to later update the file contents if necessary, by issuing a 
conditional http get to retrieve updates. It’s generally advisable to commit the .netconfig file 
to source control, so that updating is simply a matter of running `dotnet file update`. 

Note also that if matching `[file]` entries are not present for the files being updated, 
the can still be synced by just specifying `file update [url]` instead, which behaves 
just like `download` and overwrites the target file if the retrieved content is different.

![Icon](https://raw.github.com/kzu/dotnet-config/master/docs/img/icon-32.png) dotnet-file
============

A dotnet global tool for installing/downloading and updating/syncing loose flies from arbitrary URLs.

[![Build Status](https://dev.azure.com/kzu/oss/_apis/build/status/dotnet-file?branchName=master)](https://dev.azure.com/kzu/oss/_build/latest?definitionId=33&branchName=master)
[![Version](https://img.shields.io/nuget/vpre/dotnet-file.svg)](https://www.nuget.org/dotnet-file/dotnet-file)
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

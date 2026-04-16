![Icon](https://raw.githubusercontent.com/devlooped/dotnet-file/main/docs/img/icon-32.png) dotnet-file
============

[![Version](https://img.shields.io/nuget/v/dotnet-file.svg?color=royalblue)](https://www.nuget.org/packages/dotnet-file)
[![Downloads](https://img.shields.io/nuget/dt/dotnet-file.svg?color=darkmagenta)](https://www.nuget.org/packages/dotnet-file)
[![License](https://img.shields.io/github/license/devlooped/dotnet-file.svg?color=blue)](https://github.com/devlooped/dotnet-file/blob/master/LICENSE)

<!-- #content -->
A dotnet global tool for downloading and updating loose files from arbitrary URLs.

Installing or updating (same command can be used for both):

```
dotnet tool update -g dotnet-file
```

To get the CI version:

```
dotnet tool update -g dotnet-file --no-cache --add-source https://pkg.kzu.app/index.json
```

Usage:

    dotnet file [add|changes|delete|list|sync|update] [file or url]*

    Actions
        add        downloads a file or GitHub repository or directory from a URL
        changes    checks remote URLs for changes and lists the status of local files
        delete     deletes a file and its corresponding config entry from the local directory
        init       initializes the local directory from one or more remote .netconfig files
        list       lists the config entries and the status of their corresponding files
        sync       synchronizes with remote URLs, deleting local files and directories as needed
        update     updates local files from remote URLs, does not prune deleted remote files

    Status
      = <- [url]   remote file equals local file
      ✓ <- [url]   local file updated with remote file
      ^ <- [url]   remote file is newer (ETags mismatch)
      ? <- [url]   local file not found for remote file
      x <- [url]   error processing the entry


All arguments after the action are tried for URL parsing automatically to 
disambiguate file (local path) from url (remote file location).

Examples:

    dotnet file init [url]          // seeds the current directory with all files/URLs listed in a remote URL
    dotnet file add [url]           // downloads a file to the current directory and adds its URL+ETag in dotnet-config
    dotnet file add [url] [file]    // downloads the url to the (relative) file local path specifed and adds
                                    // its URL+ETag in dotnet-config
    dotnet file add [url] .         // downloads the url to the current directory and stores URL+ETag in dotnet-config, 
                                    // ignoring the source directory structure
    dotnet file add [url] docs/     // downloads the url to the specified directory, preserving the source directory structure
    dotnet file add [url] docs/.    // downloads the url to the specified directory, flattening the source directory structure
    dotnet file update [file]       // updates a specific file, based on its dotnet-config configuration
    dotnet file update [url]        // updates a specific file by its url, based on its dotnet-config configuration
    dotnet file update              // updates all recorded files, according to the dotnet-config configuration
    dotnet file sync                // just like update, but also prunes files/folders removed from their remote urls
    dotnet file delete [file]       // deletes a local file and its entry in .netconfig
    dotnet file list                // lists all configured files
    dotnet file changes             // lists all configured files and their status with regards to the configured 
                                    // remote URL and ETag matching

The target path is determined by the following rules:
* If `[file]` = `.`: download to the current directory, ignoring the source directory structure.
* If `[url]` ends with `/`: download to the current directory, preserving the source directory structure 
  from that point onwards (i.e. for GitHub tree/dir URLs)
* Otherwise, match the directory structure of the source file.  

After downloading a file, a new entry is created in a local `.netconfig` file, which
leverages [dotnet config](https://github.com/kzu/dotnet-config):

    [file "relative file path"]
      url = [url]
      etag = [etag]

This information is used to later update the file contents if necessary, by issuing a 
conditional http get to retrieve updates. It’s generally advisable to commit the .netconfig file 
to source control, so that updating is simply a matter of running `dotnet file update`. 

> Note: `dotnet file update [url]` behaves just like `dotnet file add [url]` when a matching 
> entry for the file isn't found in the `.netconfig` file.

If you want to skip further synchronization of a file, you can add `skip` to the entire: 

    [file "readme.md"]
      url = [url]
      skip

Symbols are used to denote actions (pending or performed) on files:

* `√`: file has no pending updated (ETag matches) or it was just downloaded successfully.
* `^`: file has pending updates (ETag doesn't match the remote).
* `=`: no changes to file were necessary in an update
* `?`: file not found locally. A new version can be downloaded from the remote.
* `x`: could not update file or refresh ETag status, with reason noted in subsequent line.

Downloading entire repositories or specific directories within them is supported through the 
[GitHub CLI](https://cli.github.com/manual/installation), which must be installed previously. 
You can verify you have the property authentication and access in place by running the following 
GH CLI command:

    gh repo view org/repo

If you can view the output (would be the README from the repo), you can download files from it
with `dotnet-file`.


Private repositories are supported from GitHub and BitBucket through the 
[Git Credentials Manager Core](https://github.blog/2020-07-02-git-credential-manager-core-building-a-universal-authentication-experience/) 
project.

When adding a file, it's possible to customize the local file location by specifying an absolute 
or relative file path, as follows:

* `src/External/File.cs`: fully custom target file path, doesn't need to match source URI file name 
  or directory structure.
* `src/External/`: use the given directory as the base directory, but otherwise preserve the source 
  URI directory structure and file name.
* `src/External/.`: download to the given directory, without recreating source URI directory structure, 
  using the source file name only.
* `.` (a single dot character as the target path): download to the current directory, don't preserve 
  source URI directory structure, use source file name only.


Concrete examples:

    > dotnet file add https://github.com/devlooped/dotnet-file/blob/main/azure-pipelines.yml
    azure-pipelines.yml √ <- https://github.com/devlooped/dotnet-file/blob/main/azure-pipelines.yml

    > dotnet file add https://github.com/devlooped/dotnet-file/blob/main/docs/img/icon.png img/icon.png
    img/icon.png √ <- https://github.com/devlooped/dotnet-file/blob/main/docs/img/icon.png

    > dotnet file list
    azure-pipelines.yml = <- https://github.com/devlooped/dotnet-file/blob/main/azure-pipelines.yml
    img/icon.png        = <- https://github.com/devlooped/dotnet-file/blob/main/docs/img/icon.png

    > del img\icon.png
    > dotnet file list
    azure-pipelines.yml = <- https://github.com/devlooped/dotnet-file/blob/main/azure-pipelines.yml
    img/icon.png        ? <- https://github.com/devlooped/dotnet-file/blob/main/docs/img/icon.png

    # missing file downloaded successfully
    > dotnet file update
    azure-pipelines.yml = <- https://github.com/devlooped/dotnet-file/blob/main/azure-pipelines.yml
    img/icon.png        √ <- https://github.com/devlooped/dotnet-file/blob/main/docs/img/icon.png

    # file updated on remote, changes detected
    > dotnet file changes
    azure-pipelines.yml ^ <- https://github.com/devlooped/dotnet-file/blob/main/azure-pipelines.yml
    img/icon.png        = <- https://github.com/devlooped/dotnet-file/blob/main/docs/img/icon.png

    # file renamed or deleted from remote
    > dotnet file changes
    azure-pipelines.yml = <- https://github.com/devlooped/dotnet-file/raw/main/azure-pipelines.yml
    img/icon.png        x <- https://github.com/devlooped/dotnet-file/blob/main/docs/img/icon.png
                             404: Not Found

    # download entire directory to local dir matching remote folder structure
    > dotnet file add https://github.com/dotnet/runtime/tree/main/docs/coding-guidelines/api-guidelines
    api-guidelines  => fetching via gh cli...
    docs/coding-guidelines/api-guidelines/README.md        √ <- https://github.com/dotnet/runtime/main/docs/coding-guidelines/api-guidelines/README.md
    docs/coding-guidelines/api-guidelines/System.Memory.md √ <- https://github.com/dotnet/runtime/main/docs/coding-guidelines/api-guidelines/System.Memory.md
    docs/coding-guidelines/api-guidelines/nullability.md   √ <- https://github.com/dotnet/runtime/main/docs/coding-guidelines/api-guidelines/nullability.md
    ...

    # download entire directory to a local subdirectory, from where dir structure will match remote structure
    > dotnet file add https://github.com/dotnet/runtime/tree/main/docs/coding-guidelines/api-guidelines external/dotnet/
    external/dotnet/ => fetching via gh cli...
    external/dotnet/docs/coding-guidelines/api-guidelines/README.md        √ <- https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/api-guidelines/README.md
    external/dotnet/docs/coding-guidelines/api-guidelines/System.Memory.md √ <- https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/api-guidelines/System.Memory.md
    external/dotnet/docs/coding-guidelines/api-guidelines/nullability.md   √ <- https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/api-guidelines/nullability.md
    ...

<!-- #content -->

# Dogfooding

[![CI Status](https://github.com/devlooped/dotnet-file/workflows/build/badge.svg?branch=main)](https://github.com/devlooped/dotnet-file/actions?query=branch%3Amain+workflow%3Abuild+)
[![CI Version](https://img.shields.io/endpoint?url=https://shields.kzu.app/vpre/dotnet-file/main&label=nuget.ci&color=brightgreen)](https://pkg.kzu.app/index.json)

We also produce CI packages from branches and pull requests so you can dogfood builds as quickly as they are produced. 

The CI feed is `https://pkg.kzu.app/index.json`. 

The versioning scheme for packages is:

- PR builds: *42.42.42-pr*`[NUMBER]`
- Branch builds: *42.42.42-*`[BRANCH]`.`[COMMITS]`

<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://avatars.githubusercontent.com/u/71888636?v=4&s=39 "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://avatars.githubusercontent.com/u/87181630?v=4&s=39 "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![Khamza Davletov](https://avatars.githubusercontent.com/u/13615108?u=11b0038e255cdf9d1940fbb9ae9d1d57115697ab&v=4&s=39 "Khamza Davletov")](https://github.com/khamza85)
[![SandRock](https://avatars.githubusercontent.com/u/321868?u=99e50a714276c43ae820632f1da88cb71632ec97&v=4&s=39 "SandRock")](https://github.com/sandrock)
[![DRIVE.NET, Inc.](https://avatars.githubusercontent.com/u/15047123?v=4&s=39 "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Keith Pickford](https://avatars.githubusercontent.com/u/16598898?u=64416b80caf7092a885f60bb31612270bffc9598&v=4&s=39 "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://avatars.githubusercontent.com/u/127185?u=7f50babfc888675e37feb80851a4e9708f573386&v=4&s=39 "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://avatars.githubusercontent.com/u/67574?u=3991fb983e1c399edf39aebc00a9f9cd425703bd&v=4&s=39 "Kori Francis")](https://github.com/kfrancis)
[![Reuben Swartz](https://avatars.githubusercontent.com/u/724704?u=2076fe336f9f6ad678009f1595cbea434b0c5a41&v=4&s=39 "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://avatars.githubusercontent.com/u/480334?v=4&s=39 "Jacob Foshee")](https://github.com/jfoshee)
[![](https://avatars.githubusercontent.com/u/33566379?u=bf62e2b46435a267fa246a64537870fd2449410f&v=4&s=39 "")](https://github.com/Mrxx99)
[![Eric Johnson](https://avatars.githubusercontent.com/u/26369281?u=41b560c2bc493149b32d384b960e0948c78767ab&v=4&s=39 "Eric Johnson")](https://github.com/eajhnsn1)
[![Jonathan ](https://avatars.githubusercontent.com/u/5510103?u=98dcfbef3f32de629d30f1f418a095bf09e14891&v=4&s=39 "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Ken Bonny](https://avatars.githubusercontent.com/u/6417376?u=569af445b6f387917029ffb5129e9cf9f6f68421&v=4&s=39 "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://avatars.githubusercontent.com/u/122666?v=4&s=39 "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://avatars.githubusercontent.com/u/5989304?v=4&s=39 "agileworks-eu")](https://github.com/agileworks-eu)
[![Zheyu Shen](https://avatars.githubusercontent.com/u/4067473?v=4&s=39 "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://avatars.githubusercontent.com/u/87844133?v=4&s=39 "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://avatars.githubusercontent.com/u/16239022?v=4&s=39 "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://avatars.githubusercontent.com/u/68428092?v=4&s=39 "4OTC")](https://github.com/4OTC)
[![domischell](https://avatars.githubusercontent.com/u/66068846?u=0a5c5e2e7d90f15ea657bc660f175605935c5bea&v=4&s=39 "domischell")](https://github.com/DominicSchell)
[![Adrian Alonso](https://avatars.githubusercontent.com/u/2027083?u=129cf516d99f5cb2fd0f4a0787a069f3446b7522&v=4&s=39 "Adrian Alonso")](https://github.com/adalon)
[![torutek](https://avatars.githubusercontent.com/u/33917059?v=4&s=39 "torutek")](https://github.com/torutek)
[![mccaffers](https://avatars.githubusercontent.com/u/16667079?u=110034edf51097a5ee82cb6a94ae5483568e3469&v=4&s=39 "mccaffers")](https://github.com/mccaffers)
[![Seika Logiciel](https://avatars.githubusercontent.com/u/2564602?v=4&s=39 "Seika Logiciel")](https://github.com/SeikaLogiciel)
[![Andrew Grant](https://avatars.githubusercontent.com/devlooped-user?s=39 "Andrew Grant")](https://github.com/wizardness)
[![Lars](https://avatars.githubusercontent.com/u/1727124?v=4&s=39 "Lars")](https://github.com/latonz)
[![prime167](https://avatars.githubusercontent.com/u/3722845?v=4&s=39 "prime167")](https://github.com/prime167)


<!-- sponsors.md -->
[![Sponsor this project](https://avatars.githubusercontent.com/devlooped-sponsor?s=118 "Sponsor this project")](https://github.com/sponsors/devlooped)

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->

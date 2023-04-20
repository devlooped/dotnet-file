<!-- include ../../readme.md#content -->
<!-- #content -->
A dotnet global tool for downloading and updating loose files from arbitrary URLs.

Installing or updating (same command can be used for both):

```
dotnet tool update -g dotnet-file
```

To get the CI version:

```
dotnet tool update -g dotnet-file --no-cache --add-source https://pkg.kzu.io/index.json
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

> NOTE: to download a file from GitHub to the current directory, ignoring the remote folder structure, 
> specify `.` as the `[file]` argument after the `[url]`. Otherwise, the default will be to match the 
> directory structure of the source file.  


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
<!-- ../../readme.md#content -->

<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/clarius.png "Clarius Org")](https://github.com/clarius)
[![Christian Findlay](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MelbourneDeveloper.png "Christian Findlay")](https://github.com/MelbourneDeveloper)
[![C. Augusto Proiete](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/augustoproiete.png "C. Augusto Proiete")](https://github.com/augustoproiete)
[![Kirill Osenkov](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/KirillOsenkov.png "Kirill Osenkov")](https://github.com/KirillOsenkov)
[![MFB Technologies, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MFB-Technologies-Inc.png "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![SandRock](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/sandrock.png "SandRock")](https://github.com/sandrock)
[![Eric C](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/eeseewy.png "Eric C")](https://github.com/eeseewy)
[![Andy Gocke](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/agocke.png "Andy Gocke")](https://github.com/agocke)


<!-- sponsors.md -->

[![Sponsor this project](https://raw.githubusercontent.com/devlooped/sponsors/main/sponsor.png "Sponsor this project")](https://github.com/sponsors/devlooped)
&nbsp;

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->

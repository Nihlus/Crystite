Remora.Neos.Headless
====================

> **Warning**
> This is experimental software in early alpha. Only use it for worlds you are
> comfortable damaging beyond repair should you hit a bug in the software. 

Remora.Neos.Headless is a custom headless server for NeosVR for modern .NET. The
server comes with a variety of benefits, chief among them a faster and more
efficient runtime. In addition, the server exposes a REST API for programmatic
control over its execution and configuration.

The server is not a drop-in replacement for the stock headless client and 
requires some additional configuration before you can run it. It does, however,
aim to be compatible with NeosVR's own configuration format, allowing you to
reuse existing configuration files.

> The server is currently in an open alpha. Use with caution! If you run into
> any issues, please open an issue and describe the problem.

## Installation
Binary releases are currently only available for Debian 11. To install the
server, run the following commands.

```bash
echo 'deb [signed-by=/usr/share/keyrings/algiz.gpg] https://repo.algiz.nu/neos bullseye main' | sudo tee /etc/apt/sources.list.d/remora-neos-headless.list
sudo mkdir -p /usr/share/keyrings
sudo wget -O /usr/share/keyrings/algiz.gpg https://repo.algiz.nu/algiz.gpg

sudo apt update
sudo apt install remora-neos-headless
```

By default, the server will not be enabled nor started. Primarily, this is due
to the fact that NeosVR must also be installed and the server configured before
it can start.

By default, the server expects NeosVR to be installed under a system user's home
directory. To install NeosVR, run the following commands after installing the
server.

```bash
# The following commands are only needed if you've never installed SteamCMD 
# before
dpkg --add-architecture i386
sudo apt update
sudo apt install steamcmd

# ...

cd /var/lib/neosvr
sudo -u neosvr /usr/games/steamcmd \
  +force_install_dir /var/lib/neosvr/NeosVR \
  +login USERNAME PASSWORD \
  +app_update 740250 \
  -beta headless-client \
  -betapassword BETA_PASSWORD \
  +validate \
  +quit

# This is to work around a bug in SteamCMD's installation procedures
sudo -u neosvr ln -s \
  /var/lib/neosvr/.local/share/Steam/steamcmd/linux64 \
  /var/lib/neosvr/.steam/sdk64 
```

Replace `USERNAME` and `PASSWORD` with a Steam account that has NeosVR in its
library and `BETA_PASSWORD` with the headless client beta password. 

The server is built against the API available in the stock headless client, but 
it *should* work with the normal desktop client as well. This is an unsupported 
configuration, however, and may come with unexpected issues.

## Configuration
The server's main configuration file is located at 
`/etc/remora-neos-headless/appsettings.json`. This file contains several 
subsections used to configure various parts of the program, but the most 
important ones are the `Headless` and the `Neos` sections.

> More configuration sections than the ones listed here may work, depending on
> the libraries and systems in use by the current version of the server.
> 
> The sections listed here are the only ones officially supported and known to
> work.

### `Headless`
This section provides configuration options for the headless server application
itself.

The following keys are defined for this section.

| Name                 | Type   | Description                                                            | Default                | Required |
|----------------------|--------|------------------------------------------------------------------------|------------------------|----------|
| neosPath             | string | The path to the NeosVR installation directory                          | /var/lib/neosvr/NeosVR | yes      |
| assetCleanupInterval | string | The interval at which cached files should be cleaned up                | null                   | no       |
| maxAssetAge          | string | The maximum time a cached asset can remain unused before being deleted | null                   | no       |
| maxUploadRetries     | byte   | The maximum number of times to retry record uploads while syncing      | 3                      | no       |
| retryDelay           | string | The time to wait between subsequent record uploas while retrying       | null                   | no       |
| invisible            | bool   | Whether the logged-in account's status should be set to invisible      | false                  | no       |
| enableSteam          | bool   | Whether to enable Steam API integration                                | false                  | no       |

> It is an absolute requirement that `neosPath` points to a valid NeosVR 
> installation. The server will not run without access to NeosVR's assemblies.

`assetCleanupInterval`, `maxAssetAge`, and `retryDelay` are C# `TimeSpan` strings, meaning that they are formatted as 
colon-separated groups of units of time. For example, ten days would be expressed as `10:00:00:00`, five seconds as 
`00:00:05`, and twenty-five minutes as `00:25:00`.

### `Neos`
This section contains the normal headless client's JSON configuration as defined
by [NeosVR][1]. A few additional configuration keys have been added to
supplement the existing schema as defined below.

| Name              | Type         | Description                                                                                          | Default     | Required |
|-------------------|--------------|------------------------------------------------------------------------------------------------------|-------------|----------|
| pluginAssemblies  | string array | Paths to additional assemblies to load. This replaces the typical `-LoadAssembly` command-line flag. | not present | no       |
| generatePreCache  | bool         | Whether pre-caches should be generated.                                                              | not present | no       |
| backgroundWorkers | int          | The number of background workers to create.                                                          | (internal)  | no       |
| priorityWorkers   | int          | The number of priority workers to create.                                                            | (internal)  | no       |

Additionally, the following extra keys can be used in the `startWorlds` array.

| Name                 | Type   | Description                                        | Default   | Required |
|----------------------|--------|----------------------------------------------------|-----------|----------|
| defaultFriendRole    | string | The default role to assign to friends of the host. | `"Guest"` | no       |
| defaultAnonymousRole | string | The default role to assign to anonymous accounts.  | `"Guest"` | no       |
| defaultVisitorRole   | string | The default role to assign to visitors.            | `"Guest"` | no       |


### `Logging`
Standard .NET logging configuration. See [Logging in C# and .NET][2] for more 
information.

### `Serilog`
Standard Serilog configuration. See [Serilog.Settings.Configuration][3] for more
information.

### `Kestrel`
This section is not present by default. If you want to customize the Kestrel
web server, add it manually.

Configures server options for the built-in REST API. Typically, this is used to
customize the address and port the web server listens on. See 
[Configure options for the ASP.NET Core Kestrel web server][4] for more 
information.

## Running
Once you've configured the server, you can start it by enabling and starting the
associated systemd service.

```bash
sudo systemctl enable remora-neos-headless
sudo systemctl start remora-neos-headless
```

> Logs can be viewed through journalctl just like any other service.

If you need to perform certain one-time operations, you can optionally run the 
server directly and pass one or more command-line options. If you do, ensure 
that the server is first shut down.

```bash
sudo systemctl stop remora-neos-headless

cd /var/lib/neosvr
sudo -u neosvr /usr/lib/remora-neos-headless/Remora.Neos.Headless <option>...
```

Do note that this runs the server with more access rights and privileges than
it normally has, and you should only run it like this for things like resolving
sync conflicts or repairing local databases.

> If an option has a value, it is passed as either the next space-separated 
> option or by placing an equals sign between the option and the value.
> 
> `--option value`
> 
> `--option=value`

### Command-Line Options
| Option            | Value | Description                                                                          |
|-------------------|-------|--------------------------------------------------------------------------------------|
| --force-sync      | none  | Forces synchronization of conflicting records, overwriting the versions on the cloud |
| --delete-unsynced | none  | Deletes local conflicting records, replacing them with the versions on the cloud     |
| --repair-database | none  | Repairs the local database, correcting any inconsistencies in its contents           |

## API Usage
See the [API documentation][5] for information related to the available 
endpoints and the data formats used to control the server.

By default, the API is available on `http://localhost:5000`, which only allows
requests from the same machine the server runs on. The API is unauthenticated, 
so you should only expose it on networks and hosts you trust.

> Note that, in contrast to the stock headless client, changes made at runtime
> are not saved to the configuration file automatically. If you want to retain
> changes made during runtime through the API you must edit the file manually.

## Building
You can build a release-ready copy of the server using the following command.
Required dependencies will be downloaded automatically, so you will need an
internet connection to build.

```bash
dotnet publish -f net7.0 -c Release -r linux-x64 --self-contained false -o bin Remora.Neos.Headless
```

Replace `linux-x64` with the target OS you want to run the server on. Do note
that `--self-contained false` is required due to the use of Harmony patches 
which do not work in single-file releases.

The resulting artifacts will be placed in the `bin` directory. 

If you want to build a Debian package for system-level installation, you can 
build from the root of the repository with any compatible tool of choice. For 
example, here's how one might build a package locally with `debuild`.

```bash
debuild -us -uc
```

The resulting packages will be placed in the directory above the root of the
repository.

## Technical Details
If you're interested in the nitty-gritty of how (and why!) the server works, or
if you just want some informal technical reading, please check out [this][6]
document.

It contains a bit of a dive into some internals and has a couple of explanations
that could be useful for future work with NeosVR and headless servers.


[1]: https://raw.githubusercontent.com/Neos-Metaverse/JSONSchemas/main/schemas/NeosHeadlessConfig.schema.json
[2]: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging#configure-logging
[3]: https://github.com/serilog/serilog-settings-configuration
[4]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options
[5]: ../docs/index.md
[6]: docs/nitty-gritty.md

Crystite
====================

> **Warning**
> This is experimental software in early alpha. Only use it for worlds you are
> comfortable damaging beyond repair should you hit a bug in the software. 

Crystite is a custom headless server for Resonite for modern .NET. The
server comes with a variety of benefits, chief among them a faster and more
efficient runtime. In addition, the server exposes a REST API for programmatic
control over its execution and configuration.

The server is not a drop-in replacement for the stock headless client and 
requires some additional configuration before you can run it. It does, however,
aim to be compatible with Resonite's own configuration format, allowing you to
reuse existing configuration files.

> The server is currently in an open alpha. Use with caution! If you run into
> any issues, please open an issue and describe the problem.

## Installation
Crystite can be installed as a Debian 12 package, or run from a Docker/Podman container.

### Docker/Podman container
TODO document container setup, [use compose repo in the mean time.](https://github.com/djsime1/Crystite-Compose)

### Debian package
Binary releases are currently only available for Debian 12. To install the
server, run the following commands.

```bash
echo 'deb [signed-by=/usr/share/keyrings/algiz.gpg] https://repo.algiz.nu/crystite bookworm main' | sudo tee /etc/apt/sources.list.d/crystite.list
sudo mkdir -p /usr/share/keyrings
sudo wget -O /usr/share/keyrings/algiz.gpg https://repo.algiz.nu/algiz.gpg

sudo apt update
sudo apt install crystite
```

By default, the server will not be enabled nor started. Primarily, this is due
to the fact that Resonite must also be installed and the server configured 
before it can start.

By default, the server expects Resonite to be installed under a system user's 
home directory. To install Resonite, you can either install Resonite manually or 
let Crystite handle it on its own.

If you want to install manually, run the following commands after installing the
server.

```bash
# The following commands are only needed if you've never installed SteamCMD 
# before
dpkg --add-architecture i386
sudo apt update
sudo apt install steamcmd

# ...

cd /var/lib/Resonite
sudo -u crystite /usr/games/steamcmd \
  +force_install_dir /var/lib/crystite/Resonite \
  +login USERNAME PASSWORD \
  +app_update 2519830 \
  -beta headless \
  -betapassword BETA_PASSWORD \
  +validate \
  +quit

# This is to work around a bug in SteamCMD's installation procedures
sudo -u resonite ln -s \
  /var/lib/crystite/.local/share/Steam/steamcmd/linux64 \
  /var/lib/crystite/.steam/sdk64 
```

Replace `USERNAME` and `PASSWORD` with a Steam account that has Resonite in its
library and `BETA_PASSWORD` with the headless client beta password. 

The server is built against the API available in the normal desktop client. You
do not need a headless installation of Resonite, though you should subscribe to
Resonite's Patreon and ensure you have access to it either way. Support Resonite
and its developers!

If you want to let Crystite handle the installation itself, continue reading and
look into the `manageResoniteInstallation` configuration key.

## Configuration
The server's main configuration file is located at 
`/etc/crystite/appsettings.json`. This file contains several 
subsections used to configure various parts of the program, but the most 
important ones are the `Headless` and the `Resonite` sections.

> More configuration sections than the ones listed here may work, depending on
> the libraries and systems in use by the current version of the server.
> 
> The sections listed here are the only ones officially supported and known to
> work.

If you don't want to edit the configuration file shipped with the package, you
can also use drop-in JSON configuration files in the 
`/etc/crystite/conf.d` directory. Any files placed here will 
be merged with `appsettings.json`, overriding any values specified there. 
Drop-ins can also override each other and are ordered by their filename. This
means that if two drop-in files define the same key, the latter file overrides
the value of the first one.

There is an exception to this, and that is when the value is either an object
or an array. In this case, the latter will not replace the value directly; 
rather, it will *merge* the two values, only replacing values when their
property names or array indices conflict. See [this issue][8] for more
information about the behaviour.

### `Headless`
This section provides configuration options for the headless server application
itself.

The following keys are defined for this section.

| Name                       | Type      | Description                                                               | Default                    | Required |
|----------------------------|-----------|---------------------------------------------------------------------------|----------------------------|----------|
| resonitePath               | string    | The path to the Resonite installation directory                           | /var/lib/crystite/Resonite | yes      |
| assetCleanupInterval       | string    | The interval at which cached files should be cleaned up                   | null                       | no       |
| maxAssetAge                | string    | The maximum time a cached asset can remain unused before being deleted    | null                       | no       |
| cleanupTypes               | dict      | The asset types to clean and their associated max ages.                   | all types at `maxAssetAge` | no       |
| cleanupLocations           | string[]  | The asset locations to clean.                                             | all locations              | no       |
| maxUploadRetries           | byte      | The maximum number of times to retry record uploads while syncing         | 3                          | no       |
| retryDelay                 | string    | The time to wait between subsequent record uploas while retrying          | null                       | no       |
| invisible                  | bool      | Whether the logged-in account's status should be set to invisible         | false                      | no       |
| enableSteam                | bool      | Whether to enable Steam API integration                                   | false                      | no       |
| enableYoutubeDL            | bool      | Whether to enable youtube-dl integration                                  | true                       | no       |
| youtubeDLPaths             | string[]? | Paths to check for youtube-dl binaries                                    | (internal)                 | no       |
| manageResoniteInstallation | bool      | Whether to manage the Resonite installation at resonitePath automatically | false                      | no       |
| steamCredential            | string    | The username to use when authenticating with Steam                        | null                       | no       |
| steamPassword              | string    | The password to use when authenticating with Steam                        | null                       | no       |
| allowUnsafeHosts           | bool      | Whether to allow preemptive whitelisting of unsafe API hosts.             | no                         |          |

> It is an absolute requirement that `resonitePath` points to a valid Resonite 
> installation. The server will not run without access to Resonite's assemblies.

`assetCleanupInterval`, `maxAssetAge`, and `retryDelay` are C# `TimeSpan` 
strings, meaning that they are formatted as colon-separated groups of units of 
time. For example, ten days would be expressed as `10:00:00:00`, five seconds as 
`00:00:05`, and twenty-five minutes as `00:25:00`.

`cleanupTypes` is a dictionary of `AssetCleanupType` keys and `TimeSpan` 
strings. This property can be used to configure individual maximum ages for 
known asset types, as well as limiting which asset types are cleaned up. If an 
asset type is present as a key in this dictionary, it will be cleaned up at 
either `maxAssetAge` (if the key's value is `null`) or at the time specified in 
the dictionary.

By default, all asset types older than `maxAssetAge` are cleaned up.

`cleanupLocations` is a list of well-known locations (`AssetCleanupLocation`) to 
periodically clean up. This property can be used to limit cleaning to one or the
other of the data and cache directories.

By default, all locations are cleaned up.

`allowUnsafeHosts` can be quite dangerous. Enabling this option is functionally 
the same as giving everyone that joins any session hosted by the headless full 
control over the headless. 

You should not enable this option unless you have complete trust in everyone who
joins your sessions and any and all items they bring with them.

#### `AssetCleanupType`
| Value      | Description          |
|------------|----------------------|
| Local      | `local://` assets    |
| ResoniteDB | `resdb://` assets    |
| Other      | any other URI scheme |

#### `AssetCleanupLocation`
| Value   | Description                           |
|---------|---------------------------------------|
| Data    | clean files in the `Assets` directory |
| Cache   | clean files in the `Cache` directory  |

`enableYoutubeDL` and `youtubeDLPaths` can be used to control whether video 
textures use youtube-dl (or a compatible alternative) to load information from a 
remote video service. You can either enable or disable the integration outright, 
or use one or more alternate search paths for the program used.

By default, the server will look for `yt-dlp` and `youtube-dl`, prioritizing the
former, in the following order:
  1. (Non-Windows only) /usr/bin
  2. (Non-Windows only) /usr/local/bin
  3. (Windows only) `resonitePath`\RuntimeData
  4. $PWD

If you set `youtubeDLPaths` to `null` or omit it from your configuration, this 
is the paths that will be searched. If you set your own list of paths, they will
be tried in the order you enter them.

`manageResoniteInstallation` can be enabled to let Crystite handle installation
and updating of the Resonite installation at `resonitePath`. Enabling this means 
that Crystite will, if no installation is present, authenticate with Steam using 
`steamCredential` and `steamPassword` and then download Resonite all on its own.
After that, Crystite will check for updates at startup and install them.

The credentials supplied here must be a valid Steam user account. The account
does not need to have Resonite added to its library, however, as Crystite will
add it if required. At present, you *must* disable Steam Guard on this account,
as Crystite does not provide a way for you to authenticate the account with a
Steam Guard code. Failure to do so will result in an inability to authenticate
with the Steam servers. Generally, you should set up a dedicated account for
this and not reuse one you might be using for other games.

Note that Crystite will only install updates if it's been built against that 
specific version - if not, the program will skip the update procedure and run 
with the old binaries. You can manually update the installation using something 
like SteamCMD, but note that this is an unsupported configuration and may cause
unexpected issues. Always keep a backup of the Resonite installation if you do 
this!

### `Resonite`
This section contains the normal headless client's JSON configuration as defined
by [Resonite][1]. A few additional configuration keys have been added to
supplement the existing schema as defined below.

| Name              | Type         | Description                                                                                          | Default     | Required |
|-------------------|--------------|------------------------------------------------------------------------------------------------------|-------------|----------|
| pluginAssemblies  | string array | Paths to additional assemblies to load. This replaces the typical `-LoadAssembly` command-line flag. | not present | no       |
| generatePreCache  | bool         | Whether pre-caches should be generated.                                                              | not present | no       |
| backgroundWorkers | int          | The number of background workers to create.                                                          | (internal)  | no       |
| priorityWorkers   | int          | The number of priority workers to create.                                                            | (internal)  | no       |

> **Warning**
> Ensure that any assemblies referenced in `pluginAssemblies` are *not* stored in the root of the Resonite installation
> directory (or, if you're using the desktop client, any directory containing Resonite assemblies). Doing so can cause
> runtime conflicts between assembly versions shipped by the server and ones shipped by Resonite (or ones shipped with the
> plugin!).

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

## Mods
[ResoniteModLoader][7] can be used with the server to add runtime patches and 
normal headless mods. Compatibility is not guaranteed, however, depending on 
what the mods do - as always, use with caution and your mileage may vary.

To install RML, follow their guide but replace step 3 (where you add the 
command-line argument) with an appropriate modification to `appsettings.json`'s
`pluginAssemblies` property. Add the full path to the RML assembly there.

There are some use cases that are affected by the security hardening options in 
use by the systemd service. Primarily, you cannot store mod files outside of 
`/var/lib/crystite` as Resonite requires write permissions to mod files in order 
to perform certain postprocessing steps. You should also consider the 
restrictions imposed when testing mods, as some mod behaviour may be blocked for 
security reasons and isn't always the mod's fault.

## Running
Once you've configured the server, you can start it by enabling and starting the
associated systemd service.

```bash
sudo systemctl enable crystite
sudo systemctl start crystite
```

> Logs can be viewed through journalctl just like any other service.

If you need to perform certain one-time operations, you can optionally run the 
server directly and pass one or more command-line options. If you do, ensure 
that the server is first shut down.

```bash
sudo systemctl stop crystite

cd /var/lib/crystite
sudo -u crystite /usr/lib/crystite/crystite <option>...
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
| Option                               | Value | Description                                                                          |
|--------------------------------------|-------|--------------------------------------------------------------------------------------|
| --force-sync                         | none  | Forces synchronization of conflicting records, overwriting the versions on the cloud |
| --delete-unsynced                    | none  | Deletes local conflicting records, replacing them with the versions on the cloud     |
| --repair-database                    | none  | Repairs the local database, correcting any inconsistencies in its contents           |
| --install-only                       | none  | Exits after installing/updating the game.                                            |
| --allow-unsupported-resonite-version | none  | Don't halt if the game version differs from the version Crystite was built against.  |

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
dotnet publish -f net8.0 -c Release -r linux-x64 --self-contained false -o bin Crystite
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

## Edge Cases
This is a collection of miscellaneous edge cases that have cropped up and that
prospective runners of the software might want to know about.

### systemd-sysusers and SSSD
If you use the Debian package, a sysusers.d file is installed that specifies
the presence of a crystite user. This can sometimes conflict with or override
users defined externally, such as via LDAP.

In order to avoid problems with this, ensure that `systemd-sysusers` runs 
*after* `sssd` so that any external users can be resolved when sysusers.d is 
read.

## Technical Details
If you're interested in the nitty-gritty of how (and why!) the server works, or
if you just want some informal technical reading, please check out [this][6]
document.

It contains a bit of a dive into some internals and has a couple of explanations
that could be useful for future work with Resonite and headless servers.


[1]: https://raw.githubusercontent.com/Yellow-Dog-Man/JSONSchemas/main/schemas/HeadlessConfig.schema.json
[2]: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging#configure-logging
[3]: https://github.com/serilog/serilog-settings-configuration
[4]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options
[5]: docs/index.md
[6]: docs/nitty-gritty.md
[7]: https://github.com/resonite-modding-group/ResoniteModLoader
[8]: https://github.com/dotnet/runtime/issues/36569

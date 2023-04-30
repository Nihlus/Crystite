Remora.Neos.Headless.API
========================
A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for 
[Neos VR](https://neos.com/)'s headless client that adds a REST API for external
programmatic control.

## Usage
Everything the mod does is controlled via HTTP requests to an embedded web
server. Generally, the routes provided functionally match up with the console
commands offered out of the box but with a more program-oriented design and data
format.

To get started, check out the [Route Documentation](docs/index.md).

## Configuration
The mod defines two configuration keys through NML's configuration system.

| Key            | Type   | Description                                  |
|----------------|--------|----------------------------------------------|
| listen_address | string | the address for the HTTP server to listen on |
| listen_port    | int    | the port for the HTTP server to listen on    |

## Installation
1. Install [NeosModLoader](https://github.com/neos-modding-group/NeosModLoader/releases).
2. Download [Remora.Neos.Headless.API.zip](https://github.com/Remora/Remora.Neos.Headless.API/releases/latest/download/Remora.Neos.Headless.API.zip) 
   and unpack it into your NeosVR installation folder. This folder should be at
   `C:\Program Files (x86)\Steam\steamapps\common\NeosVR` for a default install.
   The zip file contains two folders, `nml_mods` and `nml_libraries`, which
   should both go into the installation folder. You can create these beforehand
   if it's missing, or if you launch the game once with NeosModLoader installed
   it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check 
   your Neos logs.

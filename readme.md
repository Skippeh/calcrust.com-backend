# Backend for [calcrust.com](https://www.calcrust.com)

[Frontend can be found by clicking here](https://github.com/Skippeh/calcrust.com-frontend)

## Projects information

### RustExportData

This project exports item data etc from the game. It's an oxide plugin, so you'll need to install oxide on a rust server to export data.

[Oxide download can be found here](https://github.com/OxideMod/Snapshots), and [documentation here](http://docs.oxidemod.org).

There's 4 console commands available:
* calcrust.export - Dumps all data to an oxide data file (usually at server_identity/oxide/data/RustExportData.json).
* calcrust.upload - Uploads the data to a previously configured api url.
* calcrust.uploadurl - Gets or sets the url the upload POST should be sent to.
* calcrust.uploadpass - Gets or sets the password sent with the upload POST for authentication.

The last two command values can also be found in the config file in server_identity/oxide/config/RustExportData.json (generated the first time the plugin initializes if they don't exist).

If you don't know what server_identity means [Read this](http://rustdev.facepunchstudios.com/dedicated-server).

**It's not recommended to run this plugin on a real server since it spawns and destroys building blocks and performance isn't really prioritized at this point.**

###### WebApi

This project hosts a webserver on port 7545 which exposes various api functions. [They are defined here](https://github.com/Skippeh/calcrust.com-backend/blob/master/WebAPI/ApiModule.cs). The production branch is hosted here: https://api.calcrust.com.

The port can't be changed without editing the source at the moment.

###### ImageResizer

This project creates thumbnails of all the images in a specified directory and outputs them to a specified directory. It's used for making smaller icons for all items to reduce bandwidth usage when users are browsing all items/blueprints.

Only tested on Windows.

Example usage:
* ImageResizer.exe output_dir width height images_dir - Resizes all images found in the 4th arguments directory and copies them to the first arguments directory.
* ImageResizer.exe "c:\mywebsite\img\icons" 60 60 "c:\steam\steamapps\common\rust\bundles\items

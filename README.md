<!-- PROJECT LOGO -->
<br />
<div align="center">
    <a href="https://github.com/TotallyWholesome/TotallyWholesomeMod">
    <img src="images/TW_Logo_Pride.png" alt="Logo" width="150" height="150">
  </a>
  
<h3 align="center">Totally Wholesome</h3>

  <p align="center">
    Totally Wholesome is a mod for ChilloutVR that lets you put a leash of someone or get leashed as well as control your pet's Lovense or PiShock devices.
    <br />
    <a href="https://github.com/TotallyWholesomeVRC/TotallyWholesome/issues">Report Bug</a>
    ·
    <a href="https://github.com/TotallyWholesomeVRC/TotallyWholesome/issues">Request Feature</a>
    ·
    <a href="https://discord.gg/sh5zmYrRnV">Join the Discord</a>
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
    </li>
    <li><a href="#features">Features</a></li>
    <li><a href="#installation">Installation</a></li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#development">Development</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

Welcome to Totally Wholesome, an adult-oriented mod for ChilloutVR!

Introduces unique and new player-to-player interactions that allow for one player, the "master", to have various forms of control over another, the "pet".

This is intended to be used with one or more consenting parties, so all features can be toggled and role requests are mandatory by default.

### TotallyWholesome is not made by or affiliated with Alpha Blend Interactive


### Features
* Networked leashes that are visible to all other mod users
    * Optional Private leash mode
    * Optional no visible leash mode, to allow usage of shader/avatar based leashes
    * Leash break distance to prevent the pet getting stuck when the master gets too far away
    * Temporarily disable leash while still maintaining the master pet connection

* Master remote control features
    * Remote pet gagging
    * Leash length, color, and style control
    * Optional master instance change follow system (Both users must opt-in)
    * Pet movement restrictions to disable flight and seats
    * World/Prop leash pinning
    * Toy control via [Buttplug.io](https://buttplug.io/)
    * [PiShock](https://pishock.com/#/) control

* Optional global status system to let other TW users know you're using TW
    * Optional visible Beta/Alpha tag if you have one
    * Status hidden in public instances by default
    * Light blue status icon for users with auto-accept Master/Pet Requests enabled

* Optional advanced avatar integrations
    * Advanced Avatar Remote Config allows users to control avatar parameters and active profiles
    * Avatar Integration for custom leash position (TWGag, TWCollar and TWMaster)
    * Pre-set leash position target overrides
 
 More features are to come, so be sure to keep an eye on the Totally Wholesome Discord!


<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- GETTING STARTED -->
### Installation
To get started using Totally Wholesome, you must first install MelonLoader to your ChilloutVR installation. You can get the latest MelonLoader Installer from [here](https://github.com/LavaGang/MelonLoader.Installer/releases/latest/download/MelonLoader.Installer.exe)!

Once you have MelonLoader installed and working, you can simply grab the latest [WholesomeLoader.dll](https://github.com/TotallyWholesome/TotallyWholesomeMod/releases/latest) and put that into your ChilloutVR/Mods folder. Once that's done, you should see Totally Wholesome appear in-game!

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- USAGE EXAMPLES -->
## Usage

Using Totally Wholesome is as simple as starting up ChilloutVR once installed, and heading to the new TW icon that will be in your Quick Menu!

You'll be greeted by quite a few options; the first thing you'll probably want to do is head to the TW Settings and customize your consent options, among other things. Virtually all features of TW are able to be enabled and disabled both globally and on a per user basis!

Once that is done, click the player select button in the upper right corner of the Quick Menu to see all players in your world. From there, choose a player to send a pet or master request. If they also have TW installed and accept the request, then you'll now see a leash attaching the two of you!

There's alot more to TW than just leashes. A bit too much to go over in this usage section, but be sure to check out our documentation over [here](https://wiki.totallywholeso.me/)!

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- DEVELOPMENT -->
## Development

### Dependencies
 - Suitable modern C# development environment (VS 2019 Build Tools required for ILMerge)
 - ChilloutVR
 - MelonLoader >= 0.5.7

### Preparing a development environment

* Clone the current master branch
* Initialize and update the dependencies
```
git submodule init
git submodule update 
```
* Create the "3rdparty" folder in your newly cloned project root
* Open Command Prompt inside the "3rdparty" folder and create a symlinks to folders within your ChilloutVR install
```
mklink /j ml "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR\MelonLoader"
mklink /j Managed "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR\ChilloutVR_Data\Managed"
mklink /j Mods "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR\Mods"
```
* You should now be configured to begin working on Totally Wholesome


<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ROADMAP -->
## Roadmap

To see requests and hints for upcoming feature updates to Totally Wholesome please visit our [Discord](https://discord.gg/sh5zmYrRnV)

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- LICENSE -->
## License

Distributed under the Mozilla Public License v2.0. See `LICENSE` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

You can get in contact with the Totally Wholesome Team in our [Discord](https://discord.gg/sh5zmYrRnV)!

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [ChilloutVR](https://store.steampowered.com/app/661130/ChilloutVR/) for creating a mod friendly platform to have fun on!
* [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) for creating the mod loader that makes this possible!

<p align="right">(<a href="#readme-top">back to top</a>)</p>

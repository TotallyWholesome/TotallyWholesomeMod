<!-- PROJECT LOGO -->
<br />
<div align="center">
<h3 align="center">BTKUILib</h3>

  <p align="center">
    Welcome to the BTKUILib for ChilloutVR
    <br />
    <a href="https://github.com/BTK-Development/BTKUILib/wiki">Documentation</a>
    <br />
    <br />
    <a href="https://github.com/github_username/BTKUILib/issues">Report Bug</a>
    ·
    <a href="https://github.com/github_username/BTKUILib/issues">Request Feature</a>
    ·
    <a href="https://discord.gg/z3wAVGmFQP">Join the Discord</a>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

Welcome to the BTKUILib for ChilloutVR! This project is designed to make it very simple to create functional menus within the ChilloutVR Quick Menu.
The whole goal of this library is to provide a simple API to create usable menus properly integrated within the CVR QuickMenu, all without ever having to touch Cohtml!

This library in itself is a mod for ChilloutVR providing some small features internally, but primarily to avoid issues of similar projects that existed in the other game (cough cough ReMod.Core)

### BTKUILib is not made by or affiliated with Alpha Blend Interactive

Features:
 - Expandable Tab bar
 - Players in World selection menu
 - Basic buttons and toggles
 - Highly configurable sliders
 - Multiselection radio toggle menu (think a dropdown but it's a whole page)
 - Sub pages
 - Categories
 - Built in QM notices and confirmation dialogs
 - Built in (experimental) MelonPrefs editor (This is very work in progress and will likely change alot)
 - Number entry panel
 - Utility function including callback for the CVR Keyboard
 - Summary documentation on all public functions
 - Automatically regenerates when menu is refreshed (such as pressing F5)
 - And more!

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- GETTING STARTED -->
## Getting Started

To get started with BTKUILib you can simply drop the current release in your ChilloutVR/Mods folder, once that's done you will be greeted by a single tab, the CVR QuickMenu.

From there you can start creating mods using BTKUILib or download existing mods that use it! Tabs are automatically generated and have no limit in amount aside from when it becomes unusable for players.

## Contributing

### Dependencies
 - Visual Studio or equivalent C#/.net Framework development environment
 - MelonLoader version >= 0.5.7
 - ChilloutVR
 - [*Optional*] npm and less compiler (`npm install -g less`)

### Preparing your development environment
 - Clone the current main branch
 - Create the 3rdparty folder in the folder containing the BTKUILib project
 - Open the Windows Command Prompt within that folder and create the required symlinks to your CVR installation
```
mklink /j ml "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR\MelonLoader"
mklink /j Managed "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR\ChilloutVR_Data\Managed"
```
 - [*Optional*] Install `npm` and then `lessc` via `npm install -g less` to automatically compile `.less` into `.css` during build. *Only required if you changed any `.less` files.*
 - You should now be ready to work on and compile BTKUILib!

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- USAGE EXAMPLES -->
## Usage

Usage instructions are in progress, please check example mod for basic implementation and usage.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ROADMAP -->
## Roadmap

- [ ] Proper Documentation 

See the [open issues](https://github.com/BTK-Development/BTKUILib/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- LICENSE -->
## License

Distributed under the Apache 2.0 License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

DDAkebono#0001 on Discord

Project Link: [https://github.com/BTK-Development/BTKUILib](https://github.com/BTK-Development/BTKUILib)

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [ChilloutVR](https://store.steampowered.com/app/661130/ChilloutVR/) for creating a mod friendly platform to have fun on!
* [ReMod.Core](https://github.com/RequiDev/ReMod.Core) for inspiration for the structure of BTKUILib

<p align="right">(<a href="#readme-top">back to top</a>)</p>

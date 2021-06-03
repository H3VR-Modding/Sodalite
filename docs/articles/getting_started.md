# Getting started
These articles will guide you through creating a BepInEx plugin, using Sodalite,
and then running your plugin in game. While this is intended as a starting point
for people new to modding H3VR (and possibly modding Unity games in general)
this is _not_ a comprehensive guide to C#, Unity, or even BepInEx. Documentation
for each of those can be found on their respective websites.

## Required tools
To follow along with this guide, you must have a C# IDE installed. This guide
will assume you are using Visual Studio, however you are free to use whichever
tool you want as the concepts will all be the same.

## Setup
The H3VR Modding organization on GitHub contains a template project that you can
make use of to skip much of the initial setup (WIP). Download this template
using the `code` button on the page and extract it to somewhere on your
computer. GitHub also offers a feature where you can use the template to
automatically create a new repository with it's contents. While it is not
required, if you plan on putting your code on GitHub anyway then that is a good
option too.

## Opening, building, and running the project
With the template project downloaded, open it in your IDE. Contained in the
template is a simple example plugin class which demonstrates the basic usage of
BepInEx plugins and Sodalite APIs. It is already a complete plugin so go ahead
and compile the project using the `Build > Build Solution` menu item.

After building the example plugin, in your file explorer navigate to
`ExamplePlugin/ExamplePlugin/bin/Debug/net35/` and copy the `ExamplePlugin.dll`
to your `h3vr/BepInEx/plugins/` folder. These paths may vary slightly depending
on how you've setup your project and installed your mods.

Now all that's left is running the game. With any luck, the example plugin
will be shown loading in the BepInEx console window and it will have added a
wrist menu button to show that it is working!

## What's next
The basics of creating plugins for the game are identical to generic BepInEx.
If you have not done so already, it is recommended that you read [their
documentation](https://docs.bepinex.dev/master/articles/index.html) too since
many of the concepts are used here.

For more things relating specifically to H3VR, you can browse the Sodalite
documentation to see the APIs it offers. If you can't find what you're looking
for, it is also sometimes helpful to search the game's `Assembly-CSharp.dll` in
dnSpy as it often contains useful bits of code.

Finally, if you are totally stumped, feel free to join the H3VR Modding Discord
server and ask us a question!

[![Discord](https://img.shields.io/discord/777351065950879744?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2&style=flat-square)](https://discord.gg/DCsdXk4r9A)
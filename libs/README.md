# To update these libraries
## Assembly-CSharp_publicized.dll
1. Download and extract [CabbageCrow/AssemblyPublicizer](https://github.com/CabbageCrow/AssemblyPublicizer)
2. Download `mono-cil-strip`  
  - For Windows developers, I have no clue how this is done. Please add it if you ever figure it out.  
  - For Linux developers, this may be present in your distribution's Mono package  
    - Arch Linux: `mono`
3. Drag the original `Assembly-CSharp.dll` onto the publicizer, then drag the publicized result onto `mono-cil-strip`
## MMHOOK_Assembly-CSharp.dll
1. Download and extract the net35 binaries [the latest MonoMod release](https://github.com/MonoMod/MonoMod/releases/latest) to a temporary directory
2. Run `MonoMod.RuntimeDetour.HookGen.exe --private [PATH TO ORIGINAL Assembly-CSharp.dll]` within the temporary directory

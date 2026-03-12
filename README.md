This is an early work in progress which was originally based on https://github.com/seanocali/Libretro.NET, which implements Libretro in the .NET framework with a MonoGame sample. The project still uses MonoGame but has switched to using the wrapper code from https://github.com/humbertodias/RetroUnityFE. 

The intent is to provide a pipeline for publishing retro games (Particularly Scorpion Engine's target platforms of Amiga/Mega Drive/NeoGeo) on modern platforms such as PC, Android, iOS and consoles.

Essentially - an extremely stripped down single game emulator that facilitates one of Libretro's cores.

---

Regarding the included NeoGeo BIOS - the neogeo.zip file includes only free software:

- The BIOS is from a custom fork of neopenbios (https://github.com/earok/neopenbios_scorpion). Neopenbios has numerous incompatibilities with SNK games, but so far it seems OK with Scorpion Engin e demos.
- The zoom table is a custom algorithm. It won't scale 100% identically to a real NeoGeo because it is not the same algorithm as the one used to generate SNK's zoom table, but it does scale smoothly.
- The sound driver and fix table is from https://github.com/dciabrin/ngdevkit.

GeoLith only runs the BIOS in MVS mode.

Since there's the various issues listed above, I wouldn't recommend using it if you're legally able to use UniBIOS or the original ROMs instead.

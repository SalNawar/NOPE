# NOPE

Time Sorter, a Unity 6 (6000.4.11f1) game. `docs/FEATURES.md` is the behaviour contract.

## Building the demo

A standalone Windows demo (a release player: no dev overlay, no cheats):

1. In the editor: **Time Sorter > Build Windows Demo**. It builds the enabled scenes of the build settings (TitleScene first, then the art office AnimeHall, OfficeGameplay and HomeScene) to `E:/unity/NOPE-builds/TimeSorter_Demo/TimeSorter.exe`.
2. From the command line (with no editor open on that project):
   `Unity.exe -projectPath <project> -batchmode -quit -executeMethod DemoBuild.BuildWindowsDemo -demoBuildPath <folder>/TimeSorter.exe -logFile build.log`
   (exit code 0 = built). Without `-demoBuildPath` it uses the default path above.
3. The summary (result, size, time, errors) is written beside the output folder: `<output folder>_BuildReport.txt`.

The build refuses to start when the output is inside `Assets`, when the title is not the first enabled scene, or when a scene `RunConfig` loads by name is not enabled. Never commit the build output.

To play, run `TimeSorter.exe` (full screen at the desktop resolution; add `-screen-fullscreen 0 -screen-width 1280 -screen-height 720` for a window). The save is `%USERPROFILE%/AppData/LocalLow/NOPE/Time Sorter/nope_save.json` (delete it to start over) and the player log `%USERPROFILE%/AppData/LocalLow/NOPE/Time Sorter/Player.log`.

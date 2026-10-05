# Detailed hall perspective calibration preview — 2026-10-06

Preview: complete-hall-horizon-preview.png. Original scene captured in before.png.

The entire existing HallDeepMorning image is preserved without repainting, including elevator/pier, staircase and landing, bridge, five portals, marked doors, furniture and pipe detail. The temporary registration fits the complete panorama horizontally and aligns source vanishing point (1090,300) to the desk camera's actual level horizon at viewport y0.567164. Uniform root scale is approximately0.85. Desk and camera are unchanged. A separately generated ceiling sprite supplies missing overhead coverage.

This is PREVIEW ONLY. The authoring menu restores root position/scale, pan endpoints, lighting hour and dependent markers afterward. No scene is saved. The earlier full-scene generated perspective edit was rejected because convergence stayed too high. Ceiling extension continuity remains visibly unfinished. Pan endpoints are collapsed only during this calibration; runtime pan requires additional lateral coverage before installation. This is a measured registration proof, not a finished new background.

Build: Tools > Terminal Art > City > Preview Detailed Horizon Calibration. Code: Assets/Editor/TerminalArt/HallDetailedCalibrationAuthoring.cs. Zero Unity console errors after capture.

Built-in image generation produced CeilingExtensionDraft.png (2191x718). Only its upper228pixel strip is displayed in the native preview. Prompt: generate an upward ceiling extension above the existing hall, matching petrol-teal slabs, charcoal beams, pipework, brass and orange conduits, white fixtures and original perspective; ceiling only, no replacement walls, floor, stairs, elevator, portals or desk. Requested strip2172x228; returned image2191x718 includes extra area and is retained unmodified. It does not change any original hall pixels. A fully seamless extension is still needed.

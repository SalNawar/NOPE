# Visual iteration record — 24 September 2026

Reference: `../ReStory_current_reference.png`, the last image specifically chosen by the user. Current saved scene: `Assets/Scenes/OfficeScene.unity`. This record describes actual runtime captures, not generated mockups.

| Capture | Change and review |
|---|---|
| 00_baseline_clear | Baseline with the briefing dismissed. Computer on the right; mouse obscured; lamp washes out the board. |
| 01_mouse_lamp | Mouse made visible; lamp points onto the desk without the blown-out patch on the board. |
| 02_document_tools | Reduced tray, stamp and inkpad scale. |
| 03_soft_shadows | Unsuccessful settings attempt; no visual improvement. Superseded by pass 07. |
| 04_left_pc | User-directed rearrangement: computer left, mouse right of computer, document group right. Inspection found the computer still oversized and too close to the lower frame edge. |
| 05_pc_depth | Smaller computer and keyboard, moved back and turned inward. No longer clips the frame. |
| 06_grouped_tools | Stronger inward computer angle; narrower mat; tray and stamping tools grouped. The mat now occupies about the same central width and vertical band as the reference. |
| 07_shadow_filtering | Successfully enabled soft-shadow support and 4096 main-light map using public URP properties; verified serialized values. |
| 08_window_light | Rejected lighting direction: back-facing key left the whole desk too dark and cool. |
| 09_side_daylight | Side key gives readable planes on the computer and cast shadows under the equipment. Lower fill preserves separation. |
| 10_functional_groups | Lamp and stationery moved inward around document tools. Till and intercom grouped behind computer. Newspaper stays folded at the side, and the Voyager cover stays outside the central work surface. |
| 10_monitor_zoom | Invoked existing CRT click event in Play mode. `OfficeViewController.Current` becomes `MonitorFocus` and the existing desktop opens. |
| 11_booth_frame | Slim booth crown and supports establish foreground framing while retaining an open view into the hall. |
| 12_desk_stationery | Grouped forms, file sorter and computer media add functional detail around the clear work mat. |
| 13_material_separation | Bluer mat and a real-time reflection probe; dark blank CRT remains subtle. |
| 14_contact_shading | Increased SSAO radius from 0.035 to 0.16 with a separately editable renderer feature asset. Objects sit more clearly against supporting surfaces. |
| 15_finish_grade | Ineffective attempt: renderer's postProcessData reference was null. Superseded by pass 16. |
| 16_active_finish_grade | Linked official URP postprocess data; Neutral tonemapping, restrained contrast/saturation and SMAA now active. |
| 17_named_keepsakes | Rocket locomotive and Nefertiti shelf reproductions establish specific historical identities at the sides. |
| 18_panel_layers | Blank pinned slips plus Starry Night and Mondrian postcards add overlapping detail without hiding live readouts. |
| 19_exterior_depth | Cooler, lighter city material palette separates exterior from the warm desk without changing megablock geometry. |
| 20_lamp_reflector | Correct inward-facing reflector and small bulb replace the dark, incorrectly facing disk. Latest reviewed in-game capture; zero console errors. |
| 21_hardware_palette | Cooler gray-beige PC/keyboard and restrained metal response on the till. User then identified clipping shelves, repeated artworks, statue defects, background resolution and banner visibility; these are pending corrections, not accepted final art. |

The mouse remains to the right of the keyboard for right-handed use, with its cable modeled toward the left-hand computer. Computer zoom camera and original click proxy follow the moved glass. All changing text, the digital clock, stability readout and NEXT control remain the original game-controlled objects. No Unity C# was added or changed in these iterations.

The strongest remaining differences from the reference are the hall's blocky architecture, repetitive city detail, limited color variation across individual props and the overall material finish. The new framing and named details improve the booth but do not make the whole scene final quality. The large government hall, windows and megacity remain the game's distinct environment and should not be replaced by ReStory's small workshop.

Validation: each accepted pass was captured in Play mode and visually inspected against the reference. The console returned zero errors after the final wide view and monitor event check. Physical mouse interaction and a full gameplay day were not tested. Final placement snapshot is `../hardware_scene_placement.json`; it supersedes older placement records.

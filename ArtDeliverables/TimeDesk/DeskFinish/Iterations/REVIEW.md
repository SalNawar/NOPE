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
| 20_lamp_reflector | Correct inward-facing reflector and small bulb replace the dark, incorrectly facing disk. Reviewed in-game capture; zero console errors at that pass. |
| 21_hardware_palette | Cooler gray-beige PC/keyboard and restrained metal response on the till. User then identified clipping shelves, repeated artworks, statue defects, background resolution and banner visibility; subsequent corrections are recorded below. |
| 22_glass_transparency_25 / slider_0, 100, 25 | Three window walls and Inspector transparency slider. Saved value is literally 25% transparent, hence 75% opaque. Endpoint captures show opaque, absent and tinted panes. |
| 23_unclipped_booth | Removed clipping crown/shelves and duplicate postcards/miniatures. |
| 24_banner_lamp_statues | Visible full banner cloth with ceiling suspension; lamp shade/light aimed at mat; gentler sculpture smoothing and 24k triangles. |
| 25_grouped_hall | Two supported picture rails replace isolated stands. Grouped unique exhibits and removed second Voyager cover. |
| 26_city_meshes | Replaced stretched city raster with six mesh megablocks. Clear-glass image is diagnostic; saved transparency remains 25. Composition frozen after this pass by user request. |
| 27_tasklight_finish | Increased lamp coverage for review; pool too prominent, superseded by 30. |
| 28_asset_finish | Window reveals/sills, cashbox hinges/key and ruled blank forms. No camera/prop moves. |
| 29_walnut_mapping | Continuous walnut UV map replaces inherited cube channel. Grain now reads. |
| 30_soft_tasklight | Softer, wider warm lamp pool keeps mat grid visible. |
| 31_hall_craft | Continuous mitred mouldings, banner hems/tabs and restrained floor wear/cracks. No layout changes. |
| 32_desk_bounce / 32_final_art_pass | Restrained real-time foreground fill improves shaded PC face readability. Final capture with glass 25. |

PC stays left, tilted inward; mouse right; central mat clear. Original camera/click proxy and live text retained. Only the requested glass slider adds C# in this corrective pass.

Validation: slider 0/100/25 had expected pane visibility; original CRT event enters MonitorFocus; fresh console zero errors. See `verification_32.json`. Earlier UnitySkills request-abort during transition was archived before clearing. Physical pointer and full gameplay day were not tested. Review captures are retained outside Assets; byte-identical screenshot imports are removed.

Current art remains below the reference in architectural detail, background treatment, exhibit integration and light/material richness. ReStory-level parity is not established. Camera/hall proportion changes are deferred by the latest user instruction. This is an art checkpoint, not final quality sign-off.

# Approved deeper hall — installed 2026-10-05

Saleh approved deeper-left-room-preview.png. Installed in AnimeHall via Tools > Terminal Art > City > Install Approved Deeper Room.

The approved painting supplies the revised pier, bridge landing, stairs, lockers, rails, ceiling and floor. Architecture and left/front exterior are three independently drawn native sprite renderers. Exterior regions now sample CityCleanBacking.png, an exterior-only reconstruction at the registered canvas coordinates. The prior source contained window frames, producing doubled edges during parallax. Glazing masks were retraced around the actual mullions, diagonal ceiling beams and above the handrail. Frames remain in the stationary hall plate. Native textures retain their 2172x724 dimensions without power-of-two rescaling. Eight independent flying-vehicle sprites remain clipped to the openings. The city below the diagonal handrail remains in the static hall plate; full extraction there remains future work.

Assets: Assets/Art/Office/AnimeHallLayers/Completion/DeepRoom.
Shader: NOPE/Hall Deep Layout.
Installer/capture: Assets/Editor/TerminalArt/HallDeepRoomAuthoring.cs.

Original58 registered sprite sources, original palettes and city paintings remain preserved; original hall renderers are disabled in this scene. Existing public ring hooks remain as invisible bounds proxies repositioned for gameplay portal effects. Departure-board RectTransform follows the approved display. Foreground continuation samples the revised source and current lit backdrop.

Native morning/noon/evening shadow masks are rebuilt for the revised pier/portal/seating footprints; night omits directional floor casts. Existing time weights drive hall tint, these shadow masks, ceiling emission and desk/character daylight. This is an authored2D layout, not a deeper physical3D room. Exterior art is morning only: evening/night use temporary cycle tint; separate matching exterior paintings remain pending.

Verification: eight native1920x1080 player-camera captures covering both pans at08,12,16.5,22 hours; independent flying-vehicle movement recorded in validation.txt; Unity compilation/console checks. Capture forces gameplay board/effect following after each pan because all images render within one editor frame.

Limitations: one depth per window wall; moving smoke/cloud/sun/distant ships/ground traffic/building details remain pending. Further independent furniture/portal structural edits require resplitting those surfaces from the approved painting. The old foreground grout-ray configuration is retained and should be retraced if a later downward-camera review exposes changed joints.

Gameplay integration: the departure-board marker follows the revised display. Portal proxies were corrected against the live player camera; HallDeepPortalRegistration assigns NOPE/Hall Deep Portal Glow. Its inner-opening clip confines effects to the registered ring proxies and keeps their lower fence clear. PortalOpenings.png is retained as technical reference data.

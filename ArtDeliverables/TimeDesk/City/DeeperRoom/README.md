# Current status — detailed hall restored 2026-10-06

The camera-guided redraw below was rejected for lost architecture and details. The scene and installer now use the earlier detailed HallDeepMorning / CityDenseMorning layout from 22bf7e3. Perspective remains unresolved. Unity reload stalled, so the restored view has not been verified. Existing installed captures show the rejected redraw. New drawing assets remain preserved but unused.

---

# Desk-camera hall redraw — installed 2026-10-05

The current hall replaces the older wide painting with a new camera-guided drawing. A native 3D reference uses the desk camera position, 55-degree vertical field of view and 4-degree downward pitch. The generated hall follows that reference, with a level floor, a visible front portal and stationary window frames. The rejected uniform-zoom trial was undone and its helper removed.

Current sources in Assets/Art/Office/AnimeHallLayers/Completion/DeepRoom:
- HallDeskCameraMorning.png: 1778x885 hall painting.
- CityDeskCameraMorning.png: 1777x885 exterior-only backing with dense megatowers and little water.
- DeskRoomMasks.png: left/front glazing, floor receiver and traffic clipping data.
- DeskMorningShadow.png, DeskNoonShadow.png, DeskEveningShadow.png: authored floor shadows.
- DeskArchitecture.mat, DeskLeftCity.mat, DeskFrontCity.mat: separate rendering regions.

Install with Tools > Terminal Art > City > Install Desk Camera Hall. HallDeepRoomAuthoring registers the full vertical camera field, retaining root scale 1. Horizontal pan uses the 102.33-source-pixel margin outside the 16:9 view. HallDeepLayout reads each material's canvas dimensions. Original sprites, earlier paintings and palettes remain preserved; their renderers are disabled in this scene.

Existing gameplay ring hooks use invisible bounds proxies aligned to the new portals. Live portal emission stays inside the front ring; the departure-board marker follows the new display. The foreground continuation samples the current hall painting. Desk geometry and gameplay camera are unchanged.

Verification: eight native 1920x1080 captures cover front/left views at morning, noon, evening and night. Live portal glow and departure text were checked after starting the shift. The actual downward desk-camera transition was inspected; desk-camera-noon.png records its endpoint. Eight flying-vehicle sprites remain independent, with measured movement. Unity console count is zero and Hall Deep Layout has no shader errors.

Limits: this is a camera-guided 2D drawing, not a rebuilt 3D hall. Painting geometry approximates the reference. City glazing currently uses one depth per wall, with only safe upper apertures extracted; lower portions remain in the stationary painting. The city is morning art with temporary evening/night tint. Separate time-of-day city paintings, animated smoke/clouds/sun/ships/ground traffic/building details remain pending. Foreground grout parameters retain the earlier setup and need retracing if later camera changes expose mismatched joints.


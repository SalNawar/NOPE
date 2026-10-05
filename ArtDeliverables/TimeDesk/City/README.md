# Morning city outside AnimeHall

Generated with the built-in image tool. Exact prompts are saved beside this document. Native source assets are in Assets/Art/Office/AnimeHallLayers/Completion/City: CityMorning.png, FlyingTaxi.png and FlyingServiceVan.png.

Perspective: a 2172x724 panorama matching the hall's existing full-canvas registration. An elevated occupied-floor view looks slightly down across nearer rooftops, while distant megatowers rise above an approximately 38%-height horizon. The prominent Deco tower and worn residential blocks are deliberately in the left third, where the tall side windows expose the city. The statue, domed civic landmark and industrial waterfront extend across the panorama, portions visible in the smaller far windows. The current hall architecture naturally occludes much of this wide city; the exterior never replaces the interior.

Human near-future retro Deco city, 1980s anime linework, golden morning sunlight, vibrant teal/coral/cream surfaces. Working-class residential roofs, ornate civic infrastructure, wealthy corporate megatowers, and a dirty industrial waterfront have different silhouettes and construction histories. Grime, laundry, repairs and service infrastructure remain visible.

The original exterior sprite and its alpha are retained as the window aperture. A separate material samples the city painting through that aperture, preserving window frames, wall holes, railing and hall layers. Hall lighting rebakes preserve this exterior material. Morning is the only authored city lighting state; all four material slots temporarily reference Morning until matching noon/evening/night paintings are supplied. Do not call those three placeholders completed lighting variants.

Eight separate flying-car sprites use two vehicle designs and different lanes, sizes, speeds and phases. Smaller traffic receives atmospheric tint. Window-mask clipping prevents the sprites drawing across the hall; reduced-motion preference freezes them. Aircraft are not baked into the skyline. This is moving 2D traffic over one city painting, with no reconstructed exterior building depth or roof occlusion.

Unity: Tools > Terminal Art > City > Install Morning City (Edit mode). In Play mode, Capture Morning And Traffic writes forward/left screenshots and a two-second movement check. The morning lighting preview uses the existing slider and leaves shift progress alone.

Generation prompts:
- [Morning panorama](morning-city-prompt.txt)
- [Flying taxi](flying-taxi-prompt.txt)
- [Flying service van](flying-service-van-prompt.txt)

Validation: zero Unity errors after installation and compilation; eight separate vehicle renderers; original 2172x724 aperture, pivot and registration preserved. A subsequent four-state hall rebake retains the city material. The live traffic movement measurement and paired captures are stored beside this document.

## High-floor perspective revision

The active morning material now uses CityMorningHighFloor.png, a separate replacement candidate generated with the built-in image tool. It shows roof planes from above and separates dense residential (left), corporate/civic (center), and industrial (right) districts. Arbitrary bridges were removed. The previous CityMorning.png is retained for comparison. The current forward/left captures are refreshed for this candidate.

The camera perspective is under visual review in the actual hall; this installation does not claim it is approved. Layered parallax remains pending while the view is checked. No rejected foreground layer is installed.

Prompts: [district composition](high-floor-districts-prompt.txt), [selected backplate](high-floor-backplate-prompt.txt).

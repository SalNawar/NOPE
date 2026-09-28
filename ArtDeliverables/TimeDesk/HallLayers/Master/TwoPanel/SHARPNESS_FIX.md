# Remove double exposure and inspect perspective

The earlier seam repaint overlapped the hall at global x3084–3258 with a broad opacity feather. Its portal contours drifted from the source, creating the doubled/blurry edges the user reported. The patch is now completely disabled; it remains preserved as an unused asset. Hall, portals, board and stairs render from right-hall-v2-neutral.png alone.

The preview includes a 100% detail toggle. Hall display size is exactly2172x724 at that setting; horizontal position is rounded to a pixel to avoid extra subpixel softening. Browser DOM confirmed the native source/display dimensions and absence of any patch overlay. Visual inspection showed the front portal with a single contour.

The two source handrails also have different slope and height. Measured approximate hall line: y=528−0.270x. City line: y=1038−0.255x. A preview registration scales city horizontally to2390px and vertically by7/6 around y220. This makes the rail endpoints and slopes close without changing the hall. Master display extent is4562x724. Both full horizontal panels remain; city top/bottom overscan is clipped to the frame. This affine adjustment is a visual registration study, NOT a verified common 3D camera model.

Current limitations: city shoreline still changes at the raw join; window/mullion rhythm and wall cap need precise production review. Do not claim the panorama is seamlessly finished or begin final sprite extraction yet. Do not bring back alpha blending across unregistered solid edges. The source files themselves were not sharpened, repainted or overwritten in this correction. No Unity scene or lighting changes.

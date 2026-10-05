# Three connected morning city panels — review prototype

Active source: Assets/Art/Office/AnimeHallLayers/Completion/City/CityMorningConnected.png (2172x724). Native Unity slicing creates three adjacent 724x724 images: City_Left_Housing, City_Center_Civic, City_Right_Harbour. They share one source texture so edge pixels are continuous. Three aperture-clipped renderers use consecutive panel ranges [0,1/3], [1/3,2/3], [2/3,1]. No window repeats the complete skyline. Scale and vertical offset are identical across the panels.

Whole-exterior parallax relative to hall frames is present. Eight flying vehicles remain separately animated and clipped to the original aperture. This is a lightweight perspective/layout prototype; no building-depth reconstruction, smoke, moving clouds, sun animation, ships, ground traffic or building-detail animation is finished. Morning only; other city slots explicitly remain morning placeholders.

The selected hall palette and revised waiting bay are described in ../Palette/README.md. Rear seating/dispenser and their obsolete contact patch are disabled, with a separate restoration layer. The furniture service additions use the solid right wall; windows remain clear.

Reference direction: Cyberpunk 2077 megabuildings and sharply differentiated districts; Hong Kong density/harbour, New York skyline silhouettes, Tokyo infrastructure and signs, and industrial port fabric. Original human-only grounded retro anime / Art Deco city, vibrant colors, clear wealth contrasts, visible rooftop surfaces from an occupied high floor. The user is still reviewing scale, city identity and perspective.

Primary visual reference: https://www.cyberpunk.net/en/news/50122/your-trip-to-night-city-best-routes-to-take-your-ride-for-a-spin
Generation prompt: connected-city-prompt.txt. Prior candidates and prompts remain for comparison.

Tools > Terminal Art > City > Install Morning City installs the three images in Edit mode. Capture Morning And Traffic in Play mode records forward/left views and a two-second traffic movement check. Palette > Capture Selected Palette renders four native lighting-state proofs without modifying gameplay progress.

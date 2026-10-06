STATUS: UNRESOLVED — user rejected the completion claim. See ../../SCENE_REVIEW_CHECKLIST.md. Technical checks below do not establish visual correctness.

Hall floor shadow repair — 2026-10-06

Installed technical masks for the existing 2172x724 HallDeepMorning illustration. Replaced the offset rectangular cast strips with swept ground footprints registered to the pier, three floor bays, bench and individual foreground brass posts. Floor receiver now excludes traced bay silhouettes, stair/pier and bench furniture. Removed the fade-in at shadow origins; the algorithm removes origin fade-in and adds distance softening/fade. Visual contact correctness remains unverified.

Morning, noon and evening continue to blend using the existing lighting controller. Night has no added directional daylight cast. Glazing mask red/green/alpha channels verified unchanged pixel-for-pixel; receiver blue channel only was rebuilt. Camera, hall transform, desk alignment, traveller and source illustration unchanged. Future complete hall installation uses the same corrected authoring routine.

Verified in Unity: zero compile/runtime errors. Eight player-camera captures cover both pans at 08:00, 12:00, 16:30 and 22:00; all captures differ. Captures made with the existing live guest; the desk clock shows gameplay time independently from the lighting preview hour. Preview hour and pan restored after review.

These remain art-directed 2D casts, approximating the illustration's ground footprints rather than a 3D geometric shadow simulation.

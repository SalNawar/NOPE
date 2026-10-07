# Complete character poses

The separate animated hands and arm UV deformation were rejected and removed. Pose sprites now replace the entire figure so shoulders, arms, hands and clothing belong to the same drawing.

Implemented two complete poses each for the exact Egyptian female and Greek male looks recorded in pose-manifest.json: explaining with an open palm, and guarded with folded arms. Entries match all original look keys. Unmatched travellers retain their original art; the rest of the cast is not yet posed.

The traveller remains at world z=1.6. Uniform sprite registration aligns head height and feet without changing distance or deforming limbs. registration.txt records the fitted transforms.

Live/current-explaining.png and Live/current-guarded.png show the Greek male poses in the running AnimeHall scene. Both verified visually; Unity reports zero errors. The Game view is open on the guarded pose.

To add art without changing the current traveller: import PNGs, run Install Complete Character Poses, then Refresh Current Complete Pose. Capture Current Figure Source writes the canonical neutral composition and exact keys. Full source and pose art must preserve identity, clothing, skin and hair.

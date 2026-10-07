# Famous33 delivery verification

Date: 7 October 2026

PASS: 33 characters x 7 variants = 231 unique PNGs.

Verified from saved files with `python tools/characters/audit_famous33.py`:

- Exact roster and variant coverage, no missing or duplicate entries.
- All PNG headers report 1024 x 1536.
- Every SHA-256 matches the manifest; all 231 hashes are distinct.
- Every adjacent prompt file matches the recorded generation prompt.
- Every image has a visual-review record.
- The Raw directory and checklist match the manifest.

Each generated image was visually inspected before saving, checking expression or gesture, connected arms and hands, clothing continuity, framing, and background. Hokusai's pose images were corrected to restore ink-stained fingers before saving.

This is a raw source-art delivery. Pixel-perfect registration, green removal and edge cleanup, Unity import, runtime transitions, and in-scene testing remain pending. The review gallery has not been browser-tested. Read README.md for the known head-placement differences and integration guidance.

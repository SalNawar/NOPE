# Cashier pulse correction

- [x] Revert previous broad prop-grouping commit at user request.
- [x] Identify incorrect Nudge reaction: explicitly dips down.
- [x] Cashier-only pulse, 8% uniform growth then shrink to original size.
- [x] Base pivot remains planted on desk; all seven mesh pieces and display stay together.
- [x] Verify two real click cycles: growth, no descent, exact return.
- [x] Inspect rest/peak/return captures and Unity errors.

Prior check proved movement, not the intended animation. Reopened after user rejected the dip.

Verified in live Unity: peak scale 1.080001, base displacement 0 m, root translation 0, exact return after two click cycles; zero Unity errors. Rest/peak native captures inspected. Only Anchor_Till was added; other prop group changes remain reverted.

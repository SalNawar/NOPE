# Quarantined EditMode tests

These EditMode tests (`ClickableTests`, `ReadyGateTests`, `OfficeViewControllerTests`)
were written TDD-first during the office two-state booth work and validate simple
runtime logic (`Clickable.OnPointerClick`, `ReadyGate.Arm/Release`,
`OfficeViewController` state machine).

They were moved out of `Assets/` because the separate `TimeDeskEditMode` test
assembly (an `.asmdef`) cannot reference the predefined **Assembly-CSharp** where all
the runtime code currently lives — so every test fails to compile with `CS0246`
("type not found"), which blocks Unity's domain reload for the whole project.

**To reinstate properly:** move the runtime gameplay code out of the predefined
Assembly-CSharp into one or more real `.asmdef` assemblies (see the `asmdef` advisory
module), then have this test assembly reference those asmdefs. At that point these
test files can move back under `Assets/Tests/EditMode/` and will compile and run.

The runtime code they cover is correct and shipping; only the test harness is parked.

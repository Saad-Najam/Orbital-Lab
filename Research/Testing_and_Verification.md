# Testing and Verification

## Framework [V: N1 lists verification/validation as part of systems engineering; the handbook page fetched gave no definitions]
Definitions to cite from the full NASA/SP-6105 text before release: verification = "was it built right" (against requirements), validation = "was the right thing built" (against stakeholder needs) [T, to confirm wording].

## Verification methods taxonomy [T]
Analysis, inspection, demonstration, test. The product teaches which method suits which requirement.

## Environmental tests to teach (concept level) [T; cite NASA GEVS GSFC-STD-7000]
Vibration (sine/random), acoustic, shock, thermal vacuum / thermal cycling, EMC, structural static load, proof pressure (conceptual only).

## Simulated lab experiments
Each produces a SIMULATED result against an illustrative requirement:
1. Vibration sweep: resonance peak of a simple spring-mass model vs excitation band.
2. Structural static load: load vs allowable per section.
3. Thermal cycle: simple lumped-capacitance model temperature vs time.
4. Sensor calibration: bias/scale-factor fit from reference data.
5. Software verification: unit test on a flight-logic block (e.g. staging logic).
6. Fault detection: injected sensor/communication/power faults, with outcomes.

## Project's own verification
- `SimTests/` analytic and consistency checks; Python tests; planned: convergence test, cross-language check, regression snapshot of ascent results.

## Gaps
- Real test levels (e.g. GEVS qualification levels) must be cited if displayed; otherwise labelled "illustrative". [U]

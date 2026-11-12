# Electrical Systems Research (conceptual)

**Safety boundary:** abstract power/signal/data relationships only. No operational ignition or pyrotechnic wiring.

## Content
- Power chain: battery -> power distribution unit -> voltage regulation -> loads (avionics, comms, payload). Launch vehicles commonly use batteries rather than solar arrays for short missions [T; cite].
- Power budget: load (W) x time (h) = energy (Wh); battery capacity margin. Simple, fully calculable, a good student exercise [T].
- Harness and connectors: why connectors, shielding and strain relief matter (conceptual) [T].
- Electrical vs data interfaces; grounding concept [T].
- Electric-pump-fed engine as an example of battery-powered propulsion subsystem [S: Rocket Lab Rutherford].

## Simulation abstraction
Power budget solver with a battery state-of-charge curve over the mission timeline; brown-out fault: SOC falls under a threshold and downstream blocks go offline [E].

## Experiments
Size a battery for a mission timeline; add a load and find the failure time; add redundancy and compare mass vs reliability.

## Gaps
- Sourced typical mass/energy density for flight batteries (publicly documented ranges). [U]

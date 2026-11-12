# Structural Systems Research

Status: framework only. Concrete numbers need NASA structural design criteria (NASA SP-8xxx series on NTRS) and textbook sources.

## Educational content
- Load paths: thrust (axial compression of lower structure), aerodynamic loads peaking near Max-Q [V: G4 for Max-Q concept], lateral/bending loads from winds and manoeuvres, vibration/acoustic loads at liftoff [T].
- Structural elements: primary cylinder (skin + stringers/rings or isogrid/monocoque concepts), tank walls as structure (pressure-stabilised thin shells) [T], interstage, adapters, thrust structure, fairing.
- Materials (qualitative): aluminium-lithium alloys, stainless steel, carbon-fibre composites; trade-offs of density, stiffness, temperature, cost [T].
- Concepts: stress, strain, factor of safety, buckling, fatigue, thermal expansion, natural frequency [T].

## Simulation abstraction
- Axial load estimate = m_above * a_axial (from sim), compared against an illustrative allowable per section [E].
- Max-Q bending is a simple proxy: q * A * (angle of attack) [A], angle of attack is zero in the planar model, so bending is **not yet modelled**.

## Experiments
Load-vs-time chart from the ascent sim; structural margin check per section; mass vs margin trade.

## Gaps
- Sourced structural mass fractions and typical safety factors for launch vehicles: not yet fetched. [U]

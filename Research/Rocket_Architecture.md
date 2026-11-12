# Rocket Architecture and Subsystem Dependency Map

Source basis: general launch-vehicle systems practice [T]; process framing from NASA SE Handbook groupings [V: N1]. Component lists are the educational taxonomy requested in the project brief, not a claim about any specific vehicle's internals.

## Subsystems (14)
1 Structure; 2 Propulsion; 3 Propellant storage and feed; 4 Avionics; 5 Electrical power; 6 Flight computer; 7 Guidance, navigation and control (GNC); 8 Communications/telemetry; 9 Thermal protection and control; 10 Aerodynamic structures (fairing, nose, fins); 11 Payload accommodation; 12 Separation mechanisms; 13 Recovery (where applicable); 14 Ground-support interfaces.

## Dependency map (A -> B means "B depends on A")
- Structure -> everything (mounts, load paths). Loads (thrust, aero, vibration) -> Structure sizing.
- Propellant storage -> Propulsion (feed). Propulsion -> thrust -> Structure loads, GNC (control authority via gimbal/vanes).
- Electrical power -> Avionics, Flight computer, Comms, valves/actuators (abstracted), Payload.
- Sensors (IMU, pressure, temperature, GPS) -> Data acquisition -> Flight computer -> GNC commands -> Actuators; Flight computer -> Telemetry -> Ground.
- Thermal protection -> protects Structure, Avionics, Propellant (cryogenic boil-off) from ascent heating and internal heat.
- Separation mechanisms: depend on Flight computer events + Structure interfaces; govern staging and payload release (educational concept only).
- Payload accommodation: fairing + adapter -> mechanical interface to the payload; payload power/data interfaces to Avionics.
- Ground support: umbilicals supply power/data/propellant before launch.

## Educational interface types (used by the assembly system)
Mechanical (diameter, bolt-pattern class, load rating), Electrical power (bus class), Data (bus type), Fluid (propellant line, abstracted). Compatibility rules explain failures by naming the mismatched interface and the engineering reason.

## Configuration families
Single-stage, two-stage, with side boosters, fairing-enclosed payload vs crew/capsule-style payload, expendable vs recovery-equipped. The assembly workflow adapts to the selected family.

## Gaps
- Per-vehicle internal subsystem layouts are mostly proprietary: show generic layouts only, labelled "representative". [U]

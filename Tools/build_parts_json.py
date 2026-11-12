"""Generates Assets/Resources/ExplorerData/parts.json (learning content for every part of the Explorer rocket).
Content is conceptual and educational. Propulsion/avionics/electrical: NO manufacturing, ignition or pyrotechnic detail.
Source tags: [V] verified NASA Glenn (see Research/Scientific_References.md), [T] textbook-standard, citation pending.
Run: python Tools/build_parts_json.py
"""
import json, os, sys
OUT = os.path.join(os.path.dirname(__file__), "..", "OrbitalLabProject", "Assets", "Resources", "ExplorerData", "parts.json")

SUBSYSTEMS = [
    dict(id="structure", name="Structure", color="#8DA0B3", blurb="The skeleton: carries every load and holds everything in place."),
    dict(id="propellant", name="Propellant storage & feed", color="#E69F00", blurb="Tanks, pipes and gas bottles that store the propellant and deliver it to the engines."),
    dict(id="propulsion", name="Propulsion", color="#D55E00", blurb="Engines that turn stored propellant into thrust. Concept models only."),
    dict(id="avionics", name="Avionics & control", color="#56B4E9", blurb="The electronic brain: sensors, computer and telemetry."),
    dict(id="electrical", name="Electrical power & wiring", color="#F0E442", blurb="Batteries, power distribution and wiring that keep the electronics alive."),
    dict(id="aerodynamic", name="Aerodynamic & control surfaces", color="#CC79A7", blurb="Shapes and surfaces that manage airflow and steering."),
    dict(id="separation", name="Staging & separation", color="#0072B2", blurb="Mechanisms that let stages and covers separate at the right moment (concept only)."),
    dict(id="thermal", name="Thermal protection", color="#F4A582", blurb="Shields that protect parts from heat."),
    dict(id="payload", name="Payload", color="#009E73", blurb="The reason the rocket exists: the satellite and what holds it."),
    dict(id="recovery", name="Recovery", color="#7FBF7B", blurb="Hardware that lets a stage land and be reused."),
]
ASSEMBLIES = [
    dict(id="Assembly_Stage1", name="Stage 1 (first stage / booster)"),
    dict(id="Assembly_Interstage", name="Interstage"),
    dict(id="Assembly_Stage2", name="Stage 2 (upper stage)"),
    dict(id="Assembly_AvionicsBay", name="Avionics bay"),
    dict(id="Assembly_PayloadSection", name="Payload section"),
]
NOTE = "Representative educational design, not a model of any real rocket."

P = []
def part(id, name, sub, asm, summary, what, why, how, facts, interfaces, connects, quiz=None, source="T"):
    P.append(dict(id=id, name=name, subsystem=sub, assembly=asm, summary=summary, what=what, why=why, how=how, facts=facts,
                  interfaces=[dict(type=t, text=x) for t, x in interfaces], connects=connects, quiz=quiz, source=source, note=NOTE))
def Q(q, opts, ans, why): return dict(question=q, options=opts, answer=ans, explanation=why)

# ------------------------------------------------------------ STAGE 1
A1 = "Assembly_Stage1"
part("S1_Skin", "Stage 1 outer skin", "structure", A1,
     "The smooth outer wall of the first stage.",
     "A thin cylindrical shell wrapped around the tanks and engine section, with panel seams.",
     "It gives the rocket a smooth surface for the air to flow over and protects what is inside. On many real rockets the tank walls themselves carry the loads; this model shows the skin separately so you can see inside.",
     "Air rushes over it at high speed during ascent. A smooth shape keeps drag low. Panels are joined at seams.",
     ["Hide this part to see the tanks and engine inside.", "Real rockets often combine skin and tank wall into one structure to save weight."],
     [("Mechanical", "Attaches to the forward skirt, intertank and engine section rings.")],
     ["S1_ForwardSkirt", "S1_Intertank", "IS_InterstageShell"],
     Q("Why is the skin shown as a separate part here?", ["Real rockets have no tanks inside", "So you can see the parts inside it", "It is the engine"], 1, "This is a teaching model: separating the skin lets you inspect the internals."))
part("S1_FuelTank", "Stage 1 fuel tank", "propellant", A1,
     "Stores the fuel for the first-stage engines.",
     "A large thin-walled metal tank with domed ends and internal rings called baffles.",
     "The engines need a steady supply of fuel. Most of a rocket's launch mass is propellant, so the tanks are the biggest parts of the vehicle.",
     "Fuel flows out of the bottom of the tank into feed lines. The tank is pressurised with gas so it keeps its shape and fuel keeps flowing as it empties. Baffles stop the liquid sloshing, which could upset the rocket's steering.",
     ["Domed ends handle pressure better than flat ends.", "Tanks are often the main load-bearing structure."],
     [("Fluid", "Outlet to the fuel feed line."), ("Fluid", "Pressurant gas inlet from the pressurant bottles."), ("Mechanical", "Joins the thrust structure below and intertank above.")],
     ["S1_FuelFeedLine", "S1_PressurantBottles", "S1_Intertank", "S1_ThrustStructure"],
     Q("What do the rings inside the tank (baffles) do?", ["Heat the fuel", "Reduce sloshing", "Measure speed"], 1, "Baffles damp sloshing so moving liquid does not disturb steering."))
part("S1_OxidizerTank", "Stage 1 oxidizer tank", "propellant", A1,
     "Stores the oxidizer: the substance that lets the fuel burn.",
     "A large tank above the fuel tank, built like the fuel tank.",
     "Fuel needs oxygen to burn. There is no air in space, so a rocket carries its own oxidizer.",
     "Oxidizer flows down a feed line to the engines. Some oxidizers must be kept very cold, so the tank is insulated.",
     ["A rocket carries both fuel and oxidizer, unlike a jet engine which uses outside air."],
     [("Fluid", "Outlet to the oxidizer feed line."), ("Mechanical", "Joins the intertank below and forward skirt above.")],
     ["S1_OxidizerFeedLine", "S1_Intertank", "S1_ForwardSkirt"],
     Q("Why does a rocket carry an oxidizer?", ["There is no air in space", "To make it heavier", "To cool the engine"], 0, "Burning needs oxygen; in space there is none, so it must be carried."))
part("S1_Intertank", "Stage 1 intertank", "structure", A1,
     "The strong ring section between the two tanks.",
     "A short cylinder with vertical stiffeners that joins the fuel tank and oxidizer tank.",
     "It connects the tanks and carries the loads between them. Plumbing and wiring often pass through it.",
     "Loads from the stack above travel down through the stiffeners to the lower structure.",
     ["Structural parts like this are designed to be strong yet light."],
     [("Mechanical", "Rings at both ends bolt to the tanks.")], ["S1_FuelTank", "S1_OxidizerTank", "S1_OxidizerFeedLine"])
part("S1_ForwardSkirt", "Stage 1 forward skirt", "structure", A1,
     "The top ring of the first stage.",
     "A short structural ring at the top of the oxidizer tank.",
     "It is where the first stage connects to the interstage, so loads and separation hardware attach here.",
     "It spreads concentrated loads from the interstage into the tank structure.",
     ["Interfaces between stages are carefully designed because they must stay strong yet release on command."],
     [("Mechanical", "Connects to the interstage separation ring.")], ["IS_InterstageShell", "S1_OxidizerTank", "SEP_Ring_Pushers"])
part("S1_ThrustStructure", "Stage 1 thrust structure", "structure", A1,
     "Passes the engines' push into the rocket.",
     "A strong plate with radial beams and nine engine mounts at the bottom of the tank section.",
     "The engines produce enormous force. This structure spreads that force over the whole tank structure instead of one small spot.",
     "Each engine bolts to a mount; beams carry the load into the cylindrical walls.",
     ["Without a thrust structure the engines would tear through the tank bottom."],
     [("Mechanical", "Engine mounts and tank base ring."), ("Fluid", "Feed lines pass through to the engines.")],
     ["S1_Engine_1", "S1E_GimbalMount", "S1_FuelTank"],
     Q("What is the job of the thrust structure?", ["Store propellant", "Spread engine force into the rocket", "Steer with fins"], 1, "It turns concentrated engine force into a load the whole structure can carry."))
part("S1_BaseHeatShield", "Base heat shield", "thermal", A1,
     "Protects the base of the rocket from engine heat.",
     "An insulated plate across the bottom, with openings for the engine nozzles.",
     "Hot exhaust gas can curl back and heat the engine section. The shield keeps heat out of the structure and plumbing.",
     "Insulating material resists heat flowing into the parts behind it.",
     ["Thermal protection is a subsystem in its own right in rocket design."],
     [("Mechanical", "Mounts to the thrust structure and skirt.")], ["S1_ThrustStructure"])

part("S1E_Injector", "Engine injector (concept)", "propulsion", A1,
     "Feeds and mixes fuel and oxidizer at the top of the chamber.",
     "A plate with many small openings. Conceptual model only.",
     "Fuel and oxidizer must be mixed finely and evenly so they burn smoothly.",
     "Many small streams enter the chamber together and mix before burning.",
     ["Injector design strongly affects combustion stability.", "This model is a non-functional teaching visual."],
     [("Fluid", "Fuel and oxidizer inlets from the turbopump.")], ["S1E_Chamber", "S1E_Turbopump"])
part("S1E_Chamber", "Combustion chamber (concept)", "propulsion", A1,
     "Where propellants burn to make hot, high-pressure gas.",
     "A strong, heat-resistant vessel shown in copper colour. Conceptual model only.",
     "Burning creates hot gas at high pressure. That gas is what is accelerated out of the nozzle to make thrust.",
     "Propellant burns, the gas expands and rushes toward the throat. The wall is kept from melting by cooling; one common concept is to run propellant through channels around the wall first.",
     ["Chamber walls must survive temperatures above their own melting point by being cooled."],
     [("Fluid", "Receives propellant from the injector."), ("Mechanical", "Joins the nozzle at the throat.")],
     ["S1E_Injector", "S1E_Nozzle"],
     Q("Why is the chamber wall cooled?", ["To make the rocket quieter", "Combustion gas is hotter than the wall can survive", "To store fuel"], 1, "Flame temperature exceeds what the wall material can tolerate, so cooling is essential."))
part("S1E_Nozzle", "Engine nozzle (concept)", "propulsion", A1,
     "Shapes the hot gas into a fast, straight jet.",
     "A narrow throat followed by a flared bell.",
     "The nozzle converts the gas's heat and pressure into speed. A faster exhaust means more thrust for the same propellant.",
     "Gas speeds up through the narrow throat and keeps accelerating as the bell widens. NASA's thrust equation is F = mdot x Ve + (pe - p0) x Ae: mass flow times exhaust speed, plus a pressure term that depends on the exit area.",
     ["Nozzles for sea level are shorter; nozzles for vacuum are larger.", "Thrust is higher in vacuum than at sea level for the same engine."],
     [("Mechanical", "Attaches to the chamber at the throat.")], ["S1E_Chamber", "S1E_GimbalMount"],
     Q("In the thrust equation, what does the (pe - p0) x Ae term depend on?", ["Exit pressure vs outside pressure and exit area", "Only fuel mass", "Only rocket height"], 0, "It is the pressure thrust: exit pressure minus ambient pressure, times exit area."), source="V")
part("S1E_Turbopump", "Turbopump (concept)", "propulsion", A1,
     "Pumps propellant to the high pressure the chamber needs.",
     "A turbine-driven pump unit beside the chamber. Conceptual model only.",
     "Tank pressure is far too low for a big engine. Pumps raise propellant pressure so it can flow into the chamber.",
     "A turbine spins pumps that push fuel and oxidizer into the injector. Different engine designs power the turbine in different ways.",
     ["Pumps let tanks be thin and light instead of heavy pressure vessels."],
     [("Fluid", "Inlets from feed lines; outlets to the injector.")], ["S1_FuelFeedLine", "S1_OxidizerFeedLine", "S1E_Injector"])
part("S1E_GimbalMount", "Gimbal mount", "propulsion", A1,
     "Lets the engine swivel to steer the rocket.",
     "A ring joint with two actuators.",
     "Steering by tilting the engine's thrust is how large rockets are guided. NASA notes full-scale rockets usually use pivoting nozzles rather than fins.",
     "The flight computer commands the actuators; tilting the thrust turns the rocket.",
     ["This is called thrust vector control."],
     [("Mechanical", "Between thrust structure and engine."), ("Data", "Commands from the flight computer.")], ["AV_FlightComputer", "S1E_Nozzle", "S1_ThrustStructure"],
     Q("How do large rockets mostly steer?", ["Big wings", "Tilting engine thrust", "Parachutes"], 1, "Thrust vector control tilts the engine so thrust points slightly off-centre."), source="V")
for i in range(1, 9):
    part(f"S1_Engine_{i}", f"Stage 1 engine {i}", "propulsion", A1,
         "One of eight outer engines in the cluster.",
         "A simplified engine with chamber and bell, arranged in a ring around the centre engine.",
         "A cluster of engines gives high total thrust from engines of a manageable size, and lets one design be reused many times.",
         "Each engine works like the centre engine. Together they provide the force needed to lift the full rocket.",
         ["Liftoff thrust must exceed the rocket's weight (thrust-to-weight above 1)."],
         [("Fluid", "Fed from the feed lines."), ("Mechanical", "Mounted to the thrust structure.")], ["S1_ThrustStructure", "S1E_Nozzle"])
part("S1_OxidizerFeedLine", "Oxidizer feed line", "propellant", A1,
     "The pipe carrying oxidizer to the engines.",
     "A large insulated pipe that runs from the oxidizer tank down to the engine section.",
     "The oxidizer tank is high up and the engines are at the bottom. The pipe connects them.",
     "Gravity and tank pressure push oxidizer down the pipe to the turbopumps.",
     ["Some oxidizers are extremely cold, so pipes may be insulated."],
     [("Fluid", "Tank outlet to turbopump inlet.")], ["S1_OxidizerTank", "S1E_Turbopump"])
part("S1_FuelFeedLine", "Fuel feed line", "propellant", A1,
     "The pipe carrying fuel to the engines.",
     "A shorter pipe from the fuel tank outlet to the engines.",
     "Delivers fuel to the pumps.",
     "Same principle as the oxidizer line.",
     ["Valves in lines open and close flow (not shown)."],
     [("Fluid", "Tank outlet to turbopump inlet.")], ["S1_FuelTank", "S1E_Turbopump"])
part("S1_PressurantBottles", "Pressurant bottles", "propellant", A1,
     "Store inert gas to pressurise the tanks.",
     "Strong spherical bottles wrapped in carbon fibre.",
     "As propellant leaves a tank, something must fill the space; pressurant gas keeps tank pressure steady so the tank holds its shape and flow stays smooth.",
     "Gas flows from the bottles into the tops of the tanks through regulators.",
     ["Spheres are the best shape for holding high pressure."],
     [("Fluid", "Gas lines to tank tops.")], ["S1_FuelTank", "S1_OxidizerTank"])
part("S1_GridFins", "Grid fins", "aerodynamic", A1,
     "Lattice control surfaces used to steer during descent.",
     "Four lattice panels near the top of the first stage. Typical of reusable boosters.",
     "A booster that returns for landing needs to steer through the air after separation.",
     "Moving fins change airflow forces, nudging the stage toward the landing target.",
     ["Included to illustrate a reusable first-stage concept."],
     [("Mechanical", "Hinged on the upper skin."), ("Data", "Commanded by the flight computer.")], ["AV_FlightComputer", "S1_Skin"])
part("S1_LandingLegs", "Landing legs", "recovery", A1,
     "Fold out so the stage can land upright.",
     "Four angled struts with footpads.",
     "Landing the stage lets it be reused instead of discarded.",
     "Legs fold against the body during ascent and swing out before touchdown; they absorb landing loads.",
     ["Reusability trades payload capacity for recovery hardware mass."],
     [("Mechanical", "Hinged to the engine section.")], ["S1_ThrustStructure"],
     Q("Why add landing legs?", ["To launch faster", "To let the stage be recovered and reused", "To store propellant"], 1, "Recovery lets the expensive stage fly again."))

# ------------------------------------------------------------ INTERSTAGE
AI = "Assembly_Interstage"
part("IS_InterstageShell", "Interstage", "structure", AI,
     "Connects the first stage to the second stage.",
     "A short, dark cylinder between the stages.",
     "Stages are separate vehicles stacked together. The interstage holds them rigidly during ascent and gives the upper engine room for its nozzle.",
     "It carries loads between stages, then separates from the lower stage when it is time to part.",
     ["The upper stage's big nozzle sits partly inside this section during ascent."],
     [("Mechanical", "Lower ring: stage 1 forward skirt. Upper ring: stage 2.")], ["S1_ForwardSkirt", "S2_Skin", "SEP_Ring_Pushers"])
part("SEP_Ring_Pushers", "Separation ring & pushers (concept)", "separation", AI,
     "The joint that lets the two stages part.",
     "A ring and several pusher units. A conceptual view; release hardware details are deliberately not shown.",
     "The first stage is dead weight once its propellant is gone. Releasing it lets the upper stage keep accelerating with less mass.",
     "On command from the flight computer, the joint releases and pushers move the stages apart so they do not collide.",
     ["Staging follows from the rocket equation: dropping dead mass improves final speed."],
     [("Mechanical", "Between stage 1 and interstage."), ("Data", "Event command from the flight computer.")], ["AV_FlightComputer", "IS_InterstageShell"],
     Q("Why do rockets separate stages?", ["To drop dead mass and go faster", "To make noise", "To save paint"], 0, "Shedding empty structure improves the mass ratio of what remains."))

# ------------------------------------------------------------ STAGE 2
A2 = "Assembly_Stage2"
part("S2_Skin", "Stage 2 outer skin", "structure", A2, "Smooth outer wall of the upper stage.", "A thin cylindrical shell around the upper tanks.",
     "Same purpose as the first-stage skin: smooth airflow and protection.", "See Stage 1 skin.", ["The upper stage flies mostly above the atmosphere."], [("Mechanical", "Rings at both ends.")], ["S2_FuelTank", "S2_OxidizerTank"])
part("S2_FuelTank", "Stage 2 fuel tank", "propellant", A2, "Stores fuel for the upper stage.", "A smaller tank than stage 1.",
     "The upper stage finishes the climb and adds the sideways speed needed for orbit.", "Works like the first-stage tank.", ["Upper stages carry far less propellant than boosters."],
     [("Fluid", "Fuel line to the upper engine.")], ["S2_ThrustStructure", "S2_Intertank"])
part("S2_OxidizerTank", "Stage 2 oxidizer tank", "propellant", A2, "Stores oxidizer for the upper stage.", "A tank above the upper fuel tank.",
     "Supplies the oxygen that lets the upper stage's fuel burn.", "Oxidizer flows down a feed line.", [], [("Fluid", "Line to the upper engine.")], ["S2_OxidizerFeedLine", "S2_Intertank"])
part("S2_Intertank", "Stage 2 intertank", "structure", A2, "Ring between the upper tanks.", "Short structural ring.", "Joins the two tanks and passes loads.", "As for stage 1.", [], [("Mechanical", "Ring joints.")], ["S2_FuelTank", "S2_OxidizerTank"])
part("S2_ForwardSkirt", "Stage 2 forward skirt", "structure", A2, "Top ring of the upper stage.", "Ring that connects to the avionics bay.", "Provides the mounting surface for the avionics bay and payload section.", "Spreads loads into the tank wall.", [], [("Mechanical", "Connects to the avionics bay.")], ["AV_BayStructure", "S2_OxidizerTank"])
part("S2_ThrustStructure", "Stage 2 thrust structure", "structure", A2, "Carries the upper engine's force.", "Plate with radial beams under the fuel tank.",
     "Passes engine thrust into the upper stage structure.", "Same principle as stage 1.", [], [("Mechanical", "Engine mount.")], ["S2E_GimbalActuators", "S2_FuelTank"])
part("S2E_Injector", "Upper-stage injector (concept)", "propulsion", A2, "Mixes propellants in the upper engine.", "Conceptual injector plate.", "Same purpose as the first-stage injector.", "See stage 1 injector.", [], [("Fluid", "Propellant inlets.")], ["S2E_Chamber"])
part("S2E_Chamber", "Upper-stage chamber (concept)", "propulsion", A2, "Burns propellant in the upper engine.", "A smaller chamber.", "Creates hot gas for the vacuum nozzle.", "See stage 1 chamber.", [], [("Fluid", "Receives propellant.")], ["S2E_Injector", "S2E_NozzleExtension"])
part("S2E_NozzleExtension", "Vacuum nozzle (concept)", "propulsion", A2, "A very large bell designed for space.", "A wide bell that sits inside the interstage during ascent.",
     "In vacuum there is no outside air pressure holding the exhaust in, so a larger bell lets the gas expand more and gives better efficiency.",
     "As the bell widens, exhaust pressure drops toward zero and more energy becomes speed.",
     ["This is why upper stages look different from boosters.", "In the thrust equation the pressure term depends on exit area."],
     [("Mechanical", "Bolts to the chamber at the throat.")], ["S2E_Chamber", "IS_InterstageShell"],
     Q("Why is the upper-stage nozzle so large?", ["It works in vacuum and needs more expansion", "For looks", "To hold more fuel"], 0, "A bigger expansion ratio suits low outside pressure."), source="V")
part("S2E_Turbopump", "Upper-stage turbopump (concept)", "propulsion", A2, "Pumps propellant for the upper engine.", "Compact turbine-driven pump.", "Same job as in stage 1.", "See stage 1 turbopump.", [], [("Fluid", "Pump inlets and outlets.")], ["S2E_Injector", "S2_OxidizerFeedLine"])
part("S2E_GimbalActuators", "Upper-stage gimbal actuators", "propulsion", A2, "Two actuators that swivel the upper engine.", "Two rods attached between the structure and the engine.",
     "Steers the upper stage by tilting the engine's thrust.", "Commanded by the flight computer; each rod extends or retracts.", ["Two actuators in different directions allow tilt in any direction."],
     [("Mechanical", "Between thrust structure and engine."), ("Data", "Commands from the flight computer.")], ["AV_FlightComputer", "S2_ThrustStructure"])
part("S2_OxidizerFeedLine", "Stage 2 oxidizer feed line", "propellant", A2, "Carries oxidizer down to the upper engine.", "A pipe running from the top tank to the engine.", "Connects tank to pump.", "As stage 1.", [], [("Fluid", "Tank to pump.")], ["S2_OxidizerTank", "S2E_Turbopump"])
part("S2_PressurantBottles", "Stage 2 pressurant bottles", "propellant", A2, "Gas bottles for pressurising the upper tanks.", "Two spherical composite bottles.", "Same purpose as in stage 1.", "As stage 1.", [], [("Fluid", "Gas lines to tanks.")], ["S2_FuelTank", "S2_OxidizerTank"])

# ------------------------------------------------------------ AVIONICS BAY
AB = "Assembly_AvionicsBay"
part("AV_BayStructure", "Avionics bay", "structure", AB, "The protected compartment that holds the electronics.", "A short ring with a mounting deck.",
     "Electronics are delicate. The bay gives them a vibration-resistant mount and a clean place for wiring.", "Units are bolted to the deck; the bay is part of the load path.",
     ["Avionics are mounted high, away from engine heat and vibration."], [("Mechanical", "Between stage 2 forward skirt and payload adapter.")], ["AV_FlightComputer", "AV_IMU", "AV_BatteryPack"])
part("AV_FlightComputer", "Flight computer", "avionics", AB, "The rocket's brain.", "A rugged computer board in a metal chassis.",
     "Someone or something has to decide in real time how to steer. The flight computer does that.",
     "It reads sensor data, estimates where the rocket is and how it is pointing, decides how to steer, and triggers events such as stage separation. Critical vehicles use redundant computers so one failure does not end the mission.",
     ["Redundancy is a core avionics idea.", "In this model, all data shown is simulated."],
     [("Power", "From the power distribution unit."), ("Data", "Sensor inputs; commands to gimbal actuators and event signals.")], ["AV_IMU", "AV_DataAcquisition", "AV_TelemetryTransmitter", "S1E_GimbalMount", "S2E_GimbalActuators", "SEP_Ring_Pushers"],
     Q("What is redundancy in avionics?", ["Extra backup units so a failure doesn't end the mission", "Faster processors", "Longer cables"], 0, "Duplicate units and voting protect against single failures."))
part("AV_IMU", "Inertial measurement unit (IMU)", "avionics", AB, "Feels how the rocket moves and turns.", "A small sensor box with accelerometers and gyroscopes.",
     "The rocket needs to know its orientation and acceleration without looking at anything outside.",
     "Accelerometers measure acceleration; gyroscopes measure rotation rate. Combining them over time estimates attitude and velocity, though small errors grow (drift), so other sensors such as GPS may correct them.",
     ["An accelerometer in free fall reads zero.", "Drift is why aiding sensors are used."],
     [("Power", "Low-voltage supply."), ("Data", "Digital output to flight computer.")], ["AV_FlightComputer", "AV_GPSReceiverAntenna"],
     Q("What does a gyroscope measure?", ["Rotation rate", "Temperature", "Fuel level"], 0, "Gyros sense how fast the vehicle is rotating."))
part("AV_DataAcquisition", "Data acquisition unit", "avionics", AB, "Collects readings from many sensors.", "A board that gathers signals from pressure and temperature sensors around the vehicle.",
     "There can be hundreds of measurements. This unit collects and digitises them for the computer and for telemetry.", "It converts analogue sensor signals to digital data and packages them.",
     ["Pressure and temperature sensors tell engineers how the vehicle is behaving."], [("Data", "Sensor inputs; digital output to flight computer.")], ["AV_FlightComputer", "AV_TelemetryTransmitter"])
part("AV_BatteryPack", "Battery pack", "electrical", AB, "Stores the electrical energy for the vehicle.", "A battery module in a protective case.",
     "The rocket's electronics need power during the whole flight. For short missions, batteries are a simple source.", "Chemical energy converts to electrical energy on demand.",
     ["Power budget = how much energy each unit uses x time."], [("Power", "Output to power distribution unit.")], ["AV_PowerDistributionUnit"],
     Q("Why must the power system be sized carefully?", ["Batteries run out if the load is too large", "Batteries are magnetic", "Wires get lost"], 0, "Energy = power x time; exceed capacity and units lose power."))
part("AV_PowerDistributionUnit", "Power distribution unit", "electrical", AB, "Routes power to every unit safely.", "A box that takes battery power and feeds each subsystem.",
     "Each unit needs the right voltage and protection from overload.", "It regulates voltage and protects circuits; it can isolate a faulty branch.",
     ["A single short circuit should not take everything down."], [("Power", "Input from battery; outputs to each unit.")], ["AV_BatteryPack", "AV_FlightComputer", "AV_TelemetryTransmitter"])
part("AV_TelemetryTransmitter", "Telemetry transmitter", "avionics", AB, "Sends data to the ground by radio.", "A radio transmitter unit.",
     "Engineers need to see how the vehicle is doing while it flies.", "It packages sensor data and sends it to a ground station through the antenna.",
     ["Everything shown in this model is simulated, never real NASA telemetry."], [("Data", "Input from flight computer; output to antenna."), ("Power", "From PDU.")], ["AV_TelemetryAntenna", "AV_FlightComputer"])
part("AV_TelemetryAntenna", "Telemetry antenna", "avionics", AB, "Radiates the telemetry signal.", "A small antenna on the avionics bay skin.", "It sends the radio signal out of the vehicle.", "Radio waves carry the data to ground stations.", [], [("Data", "RF from transmitter.")], ["AV_TelemetryTransmitter"])
part("AV_GPSReceiverAntenna", "GPS antenna", "avionics", AB, "Receives position and time signals from navigation satellites.", "A small flat antenna on the skin.",
     "It gives the rocket an independent position check to correct inertial drift.", "It receives signals from navigation satellites and computes position.", [], [("Data", "To flight computer.")], ["AV_IMU", "AV_FlightComputer"])
part("AV_WiringHarness", "Wiring harness", "electrical", AB, "The bundled wiring that connects everything.", "A cable bundle running from the avionics bay down the side to the engine section.",
     "Electronics at the top must talk to actuators at the bottom. The harness carries power and signals.", "Wires are bundled, shielded and supported so vibration does not damage them.",
     ["Wiring adds mass; engineers minimise it.", "This model shows a simple path only."], [("Power", "Power lines."), ("Data", "Signal lines.")], ["AV_FlightComputer", "S1E_GimbalMount"])

# ------------------------------------------------------------ PAYLOAD
AP = "Assembly_PayloadSection"
part("PL_PayloadAdapter", "Payload adapter", "payload", AP, "The mounting cone for the satellite.", "A conical carbon-fibre ring.",
     "The satellite must attach rigidly during launch and release cleanly in orbit.", "It carries launch loads into the avionics bay and holds the satellite until release.",
     ["Adapters differ by payload size and bolt pattern."], [("Mechanical", "Bolts to the avionics bay; interface to satellite.")], ["PL_SatelliteBus", "AV_BayStructure"],
     Q("What is the payload adapter's job?", ["Hold the satellite and release it in orbit", "Steer the rocket", "Store fuel"], 0, "It is the mechanical interface between rocket and satellite."))
part("PL_SatelliteBus", "Satellite bus", "payload", AP, "The main body of the satellite.", "A box wrapped in gold-coloured thermal blankets.",
     "It houses the satellite's own computer, radio, power and instruments.", "The bus provides power, pointing, communications and housing for the mission instrument.",
     ["The 'bus' is the platform; the 'payload instrument' is what does the science or service."], [("Mechanical", "To payload adapter."), ("Power", "Own batteries and solar arrays.")], ["PL_PayloadAdapter", "PL_SolarArrays", "PL_Antenna"])
part("PL_SolarArrays", "Solar arrays (stowed)", "payload", AP, "Panels that make electricity from sunlight.", "Folded panels held against the sides.",
     "Satellites have to make power for years.", "Solar cells convert sunlight to electricity. They are folded for launch and deployed in orbit.", [], [("Power", "To satellite bus.")], ["PL_SatelliteBus"])
part("PL_Antenna", "Satellite antenna", "payload", AP, "The satellite's link to Earth.", "A small dish.", "Satellites must send data and receive commands.", "It focuses radio waves toward Earth.", [], [("Data", "Radio link.")], ["PL_SatelliteBus"])
for half in ("A", "B"):
    part(f"PL_FairingHalf_{half}", f"Payload fairing, half {half}", "aerodynamic", AP, "Half of the nose shell protecting the satellite.",
         "A two-piece rounded shell around the satellite.",
         "The satellite is delicate. In the thick air near the ground it would be damaged by pressure, heating and noise. The fairing shields it.",
         "The ogive shape cuts through air. Once the rocket is above most of the atmosphere, the halves split and fall away so the rocket carries less mass.",
         ["Fairing separation sheds mass the same way stage separation does."],
         [("Mechanical", "Hinged and latched to the avionics bay ring.")], ["PL_SatelliteBus", "AV_BayStructure"],
         Q("When does the fairing separate?", ["Above most of the atmosphere", "On the launch pad", "Just before landing"], 0, "Once outside dense air the satellite no longer needs it."))

if __name__ == "__main__":
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    ids = [p["id"] for p in P]; assert len(ids) == len(set(ids)), "duplicate ids"
    for p in P:
        for c in p["connects"]:
            if c not in ids: print("WARNING unresolved connection", p["id"], "->", c)
    json.dump(dict(subsystems=SUBSYSTEMS, assemblies=ASSEMBLIES, parts=P, note=NOTE), open(OUT, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    blender = [x["id"] for x in json.load(open(os.path.join(os.path.dirname(__file__), "..", "BlenderAssets", "Rockets", "explorer_rocket_parts.json")))]
    print(len(P), "parts written;", "missing content for:", sorted(set(blender) - set(ids)), "; content without model:", sorted(set(ids) - set(blender)))

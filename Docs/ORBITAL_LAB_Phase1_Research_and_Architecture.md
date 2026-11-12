# 🛰️ ORBITAL LAB: Scientific Research, Challenge Alignment & System Architecture Specification
### NASA Space Apps Challenge Solution | Codename: ORBITAL LAB
**Document Classification:** Technical Architecture & Scientific Foundation Document  
**Version:** 1.0.0-RELEASE  
**Target Platform:** Unity 2022/6 LTS / WebGL High-Performance Tier + Python Scientific Engine  

---

## 1. EXECUTIVE SUMMARY & RESEARCH OVERVIEW

### 1.1 Project Mission Statement
**ORBITAL LAB** is a high-immersion, scientifically grounded virtual aerospace engineering and orbital mission exploration platform. It fuses the physical rigor of aerospace computational software with the spatial immersion of an interactive 3D virtual aerospace research center.

Rather than presenting space flight as an arcade experience, **ORBITAL LAB** places students, educators, and researchers inside a functioning NASA-inspired aerospace complex. Users interact with physical laboratory terminals to design modular multistage rockets, test propulsion cycles, simulate atmospheric ascent trajectories governed by real aerodynamic and gravitational equations, and deploy payloads into orbits tracked against real NASA Near-Earth Object (NEO) and planetary ephemeris data.

---

## 2. NASA SPACE APPS CHALLENGE ALIGNMENT & SELECTION

### 2.1 Challenge Landscape Analysis
We evaluated historical and active NASA Space Apps Challenge domains related to space exploration, physics modeling, and planetary visualization:

| Challenge Domain | Core Focus | NASA Resources Cited | Alignment Score | Evaluation |
|---|---|---|---|---|
| **Create an Orrery Web App that Displays Near-Earth Objects** | Interactive 3D visualization of Solar System bodies and NEOs/PHAs | NASA Horizons API, JPL Small-Body Database (SBDB), Keplerian Elements | **98% (PRIMARY TARGET)** | Directly maps to our orbital observation room, trajectory planning, and orbital insertion mechanics. |
| **Space Mission Design Challenge** | Resource-constrained mission engineering & payload delivery | NASA Design Guidelines, Delta-V Budgets, Launch Vehicle Specs | **94% (SECONDARY CO-ALIGNMENT)** | Governs our rocket assembly laboratory, propulsion balancing, and stage separation. |
| **Planetary Defense: Intercepting Threat Asteroids** | Trajectory prediction and delta-v interception calculation | CNEOS (Center for Near-Earth Object Studies), Sentry Impact Risk | **89% (EXPEDITION MODE)** | Provides concrete problem scenarios (e.g., launching an interceptor payload to a Potentially Hazardous Asteroid). |
| **Visualizing Space Weather Impacts (DONKI)** | Solar wind, coronal mass ejections, and geomagnetic effects | NASA DONKI API, NOAA SWPC feeds | **82% (SUBSYSTEM FEED)** | Environmental telemetry display in Mission Control. |

### 2.2 Primary Selected Challenge: *“Create an Orrery Web App that Displays Near-Earth Objects (NEOs)”* + *“Space Mission Design”*

#### The Core Problem Statement:
> *How can an intuitive, interactive environment bridge the gap between abstract rocket propulsion engineering (getting into space) and real celestial orbital mechanics (navigating space among real Near-Earth Objects)?*

Most existing orrery applications treat spacecraft as magic points that appear in orbit without context. Conversely, most rocket simulators stop at low Earth orbit without connecting payloads to actual solar system bodies or hazard-monitoring datasets.

**ORBITAL LAB resolves this disconnection:**
1. **The Launch Phase:** The user designs a launch vehicle capable of generating sufficient $\Delta v$ (delta-v) to overcome atmospheric drag, gravity losses, and reach orbital insertion velocity.
2. **The Orbital & Orrery Phase:** Once in orbit, the mission transitions directly into the **Planetary Orrery & NEO Tracking Station**, where the user's spacecraft trajectory is propagated alongside real NASA-cataloged asteroids (Apophis, Bennu, Ryugu, Didymos) using true Keplerian elements derived from NASA JPL's Small-Body Database.

---

## 3. COMPETITIVE ANALYSIS: EXISTING AEROSPACE PLATFORMS

We analyzed the technical boundaries and limitations of existing simulators:

### Key Differentiators of ORBITAL LAB:
1. **Spatial Immersion ("Inside the Lab"):** Unlike OpenRocket (2D graphs) or poliastro (Python CLI), ORBITAL LAB features a walk-around first-person 3D aerospace research complex.
2. **True NASA Data Pipeline:** Unlike Kerbal Space Program's fictional Kerbol system, all celestial ephemerides, asteroid orbits, and planetary parameters use real NASA JPL SBDB data.
3. **Dual Educational Architecture:** Operates with both accessible visual instrumentation for students and exact numerical SI-unit telemetry for researchers.

---

## 4. MATHEMATICAL & PHYSICAL FOUNDATIONS

ORBITAL LAB’s physics engine is strictly deterministic and adheres to published aerospace engineering equations.

### 4.1 Tsiolkovsky Rocket Equation & Staging Mechanics
The ideal velocity increment $\Delta v$ for each rocket stage is governed by:

$$\Delta v = I_{sp} \cdot g_0 \cdot \ln\left(\frac{m_0}{m_f}\right)$$

Where:
- $I_{sp}$ = Engine Specific Impulse (seconds) in vacuum or atmospheric conditions
- $g_0$ = Standard Earth gravitational acceleration ($9.80665 \text{ m/s}^2$)
- $m_0$ = Initial wet mass ($m_{dry} + m_{propellant} + m_{payload}$)
- $m_f$ = Final dry burnout mass ($m_{dry} + m_{payload}$)

For an $N$-stage rocket vehicle:
$$\Delta v_{total} = \sum_{i=1}^{N} I_{sp,i} \cdot g_0 \cdot \ln\left(\frac{m_{0,i}}{m_{f,i}}\right) - \Delta v_{grav\_loss} - \Delta v_{aero\_loss}$$

### 4.2 US Standard Atmosphere 1976 Model
Atmospheric pressure $P(h)$ and density $\rho(h)$ are calculated through layer-by-layer barometric equations up to the exosphere.

Dynamic pressure ($q$), critical for Maximum Dynamic Pressure (Max-Q):
$$q = \frac{1}{2} \rho(h) v^2$$

Aerodynamic drag force vector:
$$\vec{F}_{drag} = -\frac{1}{2} \rho(h) \cdot C_d \cdot A \cdot \|\vec{v}\| \cdot \vec{v}$$

### 4.3 Two-Body Keplerian Orbital Mechanics & Ephemeris Propagation
Once out of significant atmospheric drag ($h > 120 \text{ km}$), gravitational motion follows Newton's universal law with altitude-dependent acceleration:

$$\vec{a}_g = -\frac{\mu}{\|\vec{r}\|^3} \vec{r}$$
Where $\mu = G M_{\oplus} = 3.986004418 \times 10^{14} \text{ m}^3/\text{s}^2$.

Orbital elements mapped from NASA JPL Horizons / SBDB:
- $a$: Semi-major axis
- $e$: Eccentricity
- $i$: Orbital inclination
- $\Omega$: Longitude of ascending node
- $\omega$: Argument of periapsis
- $M$: Mean anomaly at epoch $t_0$

---

## 5. SYSTEM ARCHITECTURE SPECIFICATION

The platform employs a decoupled, multi-tier architecture ensuring scientific testability is completely isolated from the graphics rendering pipeline:
- **Layer 1: Visual & Spatial Immersion (Unity 3D Engine)**
- **Layer 2: Engine Interop & State Bridge (C# Scripts)**
- **Layer 3: Scientific Computation & Validation (Python Core)**
- **Layer 4: NASA Open Data Ingestion & Cache (JPL SBDB, Horizons)**
- **Layer 5: AI Scientific Assistant (Gemini API Integration)**

---

## 6. MVP (MINIMUM VIABLE PRODUCT) SCOPE

| Subsystem | Phase 1: MVP Core (Demo Day) | Phase 2: Full Platform |
|---|---|---|
| **3D Environment** | Main Hangar + Mission Control + Orrery Room | Full 5-room research facility with exterior launch complex |
| **Rocket Construction** | 2-stage modular rocket with 4 propellant types & 3 nose cones | 5-stage custom clustering, cross-feed fuel, variable grid fins |
| **Physics Simulation** | RK4 2D planar ascent trajectory + US Standard Atmosphere 1976 + Gravity Turn | Full 6-DOF aerodynamic rotation, aero-heating & ablation |
| **Orrery & Asteroids** | 8 planets + 10 prominent NASA NEOs (Apophis, Bennu, Didymos, etc.) | Complete NASA CNEOS catalog query with 500+ asteroid bodies |
| **NASA APIs** | Cached JPL SBDB API + Horizons ephemeris data | Real-time live synchronization with offline fallback cache |
| **AI Flight Director** | Gemini 1.5 Flash assistant answering pre-launch & flight telemetry questions | Real-time voice telemetry synthesis & automated anomaly diagnosis |

---

*Document finalized for ORBITAL LAB implementation pipeline.*

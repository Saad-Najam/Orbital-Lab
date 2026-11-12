"""
ORBITAL LAB — US Standard Atmosphere 1976 Model
================================================
Implementation of the 1976 US Standard Atmosphere model defining:
- Ambient temperature T(h) in Kelvin
- Ambient pressure P(h) in Pascals
- Air density rho(h) in kg/m^3
- Speed of sound a(h) in m/s
- Dynamic pressure q = 0.5 * rho * v^2 in Pa
- Atmospheric drag force F_drag

Geopotential altitude layers from 0 to 86 km and exponential model beyond.
Reference: NOAA, NASA, USAF (1976), 'U.S. Standard Atmosphere, 1976', NASA-TM-X-74335.
"""

import math
from typing import Tuple

# Physical constants
R_SPECIFIC = 287.05287      # Specific gas constant for dry air (J/(kg·K))
GAMMA = 1.40                # Heat capacity ratio for diatomic air
G0 = 9.80665                # Standard acceleration of gravity (m/s^2)
EARTH_RADIUS = 6371000.0    # Mean Earth radius (meters)

# Atmospheric layers: (Base Altitude m, Base Temp K, Lapse Rate K/m, Base Pressure Pa)
ATMOSPHERE_LAYERS = [
    (0.0,      288.15, -0.0065,  101325.0),
    (11000.0,  216.65,  0.0,     22632.06),
    (20000.0,  216.65,  0.0010,  5474.889),
    (32000.0,  228.65,  0.0028,  868.0187),
    (47000.0,  270.65,  0.0,     110.9063),
    (51000.0,  270.65, -0.0028,  66.93887),
    (71000.0,  214.65, -0.0020,  3.956420)
]
MAX_TABULATED_ALTITUDE = 86000.0  # 86 km


def geometric_to_geopotential(geometric_altitude_m: float) -> float:
    """Converts geometric altitude (meters) to geopotential altitude."""
    h = max(0.0, geometric_altitude_m)
    return (EARTH_RADIUS * h) / (EARTH_RADIUS + h)


def get_atmosphere_state(geometric_altitude_m: float) -> Tuple[float, float, float, float]:
    """
    Computes (temperature_K, pressure_Pa, density_kg_m3, speed_of_sound_mps).
    """
    h_geopotential = geometric_to_geopotential(geometric_altitude_m)
    
    if h_geopotential >= MAX_TABULATED_ALTITUDE:
        # Exospheric exponential density decay model for h > 86 km
        # At 86 km: T ~ 186.87 K, P ~ 0.3734 Pa, rho ~ 6.96e-6 kg/m^3
        scale_height = 7200.0  # meters
        delta_h = h_geopotential - MAX_TABULATED_ALTITUDE
        decay = math.exp(-delta_h / scale_height)
        t = 186.87
        p = 0.3734 * decay
        rho = (p / (R_SPECIFIC * t)) if p > 1e-15 else 0.0
        speed_of_sound = math.sqrt(GAMMA * R_SPECIFIC * t)
        return (t, max(0.0, p), max(0.0, rho), speed_of_sound)

    # Locate atmospheric layer
    for i in range(len(ATMOSPHERE_LAYERS) - 1, -1, -1):
        base_h, base_t, lapse_rate, base_p = ATMOSPHERE_LAYERS[i]
        if h_geopotential >= base_h:
            dh = h_geopotential - base_h
            if lapse_rate != 0.0:
                t = base_t + lapse_rate * dh
                # Non-isothermal barometric equation
                exponent = -G0 / (lapse_rate * R_SPECIFIC)
                p = base_p * (t / base_t) ** exponent
            else:
                # Isothermal layer
                t = base_t
                p = base_p * math.exp(-G0 * dh / (R_SPECIFIC * t))
            
            rho = p / (R_SPECIFIC * t)
            speed_of_sound = math.sqrt(GAMMA * R_SPECIFIC * t)
            return (t, p, rho, speed_of_sound)

    # Fallback sea-level
    return (288.15, 101325.0, 1.225, 340.29)


def dynamic_pressure(density_kg_m3: float, velocity_mps: float) -> float:
    """Calculates aerodynamic dynamic pressure q = 0.5 * rho * v^2 in Pascals."""
    return 0.5 * density_kg_m3 * (velocity_mps ** 2)


def aerodynamic_drag_force(
    geometric_altitude_m: float,
    velocity_mps: float,
    drag_coefficient: float,
    reference_area_m2: float
) -> float:
    """
    Calculates total drag force in Newtons opposing velocity vector:
    F_drag = 0.5 * rho * C_d * A * v^2
    """
    _, _, rho, _ = get_atmosphere_state(geometric_altitude_m)
    q = dynamic_pressure(rho, velocity_mps)
    return q * drag_coefficient * reference_area_m2

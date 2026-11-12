"""
ORBITAL LAB — Keplerian Two-Body Orbital Mechanics Engine
==========================================================
Implementation of:
- Vis-Viva equation for instantaneous velocity
- Circular velocity, escape velocity, orbital period
- Newton-Raphson solver for Kepler's Equation M = E - e*sin(E)
- State vector conversion (Orbital Elements <-> 3D Cartesian Position & Velocity)
- Hohmann Transfer orbit delta-v calculator
- Gravitational parameter mu for Earth, Moon, Mars, Sun

SI Units: meters, seconds, radians, kg.
"""

import math
from typing import Tuple, Dict, Any

# Standard Gravitational Parameters (mu = G * M) in m^3 / s^2
MU_EARTH = 3.986004418e14
MU_MOON  = 4.9048695e12
MU_MARS  = 4.282837e13
MU_SUN   = 1.32712440018e20

EARTH_RADIUS = 6371000.0  # meters


def circular_orbital_velocity(altitude_m: float, mu: float = MU_EARTH, body_radius_m: float = EARTH_RADIUS) -> float:
    """Calculates circular orbit speed v_c = sqrt(mu / r)."""
    r = body_radius_m + altitude_m
    return math.sqrt(mu / r)


def escape_velocity(altitude_m: float, mu: float = MU_EARTH, body_radius_m: float = EARTH_RADIUS) -> float:
    """Calculates parabolic escape speed v_esc = sqrt(2 * mu / r)."""
    r = body_radius_m + altitude_m
    return math.sqrt(2.0 * mu / r)


def orbital_period(semi_major_axis_m: float, mu: float = MU_EARTH) -> float:
    """Calculates orbital period T = 2 * pi * sqrt(a^3 / mu) in seconds."""
    if semi_major_axis_m <= 0:
        raise ValueError("Semi-major axis must be positive for closed orbits.")
    return 2.0 * math.pi * math.sqrt((semi_major_axis_m ** 3) / mu)


def solve_kepler(mean_anomaly_rad: float, eccentricity: float, tolerance: float = 1e-10, max_iter: int = 100) -> float:
    """
    Solves Kepler's Equation for Eccentric Anomaly E:
    M = E - e * sin(E)
    Uses Newton-Raphson iteration.
    """
    m = mean_anomaly_rad % (2.0 * math.pi)
    # Initial estimate
    e_anom = m if eccentricity < 0.8 else math.pi
    
    for _ in range(max_iter):
        delta = e_anom - eccentricity * math.sin(e_anom) - m
        if abs(delta) < tolerance:
            return e_anom
        derivative = 1.0 - eccentricity * math.cos(e_anom)
        e_anom = e_anom - (delta / derivative)
        
    return e_anom


def eccentric_to_true_anomaly(eccentric_anomaly_rad: float, eccentricity: float) -> float:
    """Converts eccentric anomaly E to true anomaly nu."""
    cos_e = math.cos(eccentric_anomaly_rad)
    sin_e = math.sin(eccentric_anomaly_rad)
    
    nu = 2.0 * math.atan2(
        math.sqrt(1.0 + eccentricity) * math.sin(eccentric_anomaly_rad / 2.0),
        math.sqrt(1.0 - eccentricity) * math.cos(eccentric_anomaly_rad / 2.0)
    )
    return nu % (2.0 * math.pi)


def keplerian_to_cartesian(
    semi_major_axis_m: float,
    eccentricity: float,
    inclination_rad: float,
    raan_rad: float,            # Longitude of Ascending Node (Omega)
    arg_periapsis_rad: float,   # Argument of Periapsis (omega)
    true_anomaly_rad: float,    # True Anomaly (nu)
    mu: float = MU_EARTH
) -> Tuple[Tuple[float, float, float], Tuple[float, float, float]]:
    """
    Transforms Keplerian orbital elements to Cartesian 3D Position (x,y,z) and Velocity (vx,vy,vz) vectors.
    """
    p = semi_major_axis_m * (1.0 - eccentricity ** 2)
    r = p / (1.0 + eccentricity * math.cos(true_anomaly_rad))
    
    # Position in orbital plane (perifocal frame)
    x_peri = r * math.cos(true_anomaly_rad)
    y_peri = r * math.sin(true_anomaly_rad)
    
    # Velocity in orbital plane
    h = math.sqrt(mu * p)
    vx_peri = - (mu / h) * math.sin(true_anomaly_rad)
    vy_peri = (mu / h) * (eccentricity + math.cos(true_anomaly_rad))
    
    # 3D Euler rotation matrix components (perifocal to inertial)
    cos_o = math.cos(raan_rad)
    sin_o = math.sin(raan_rad)
    cos_w = math.cos(arg_periapsis_rad)
    sin_w = math.sin(arg_periapsis_rad)
    cos_i = math.cos(inclination_rad)
    sin_i = math.sin(inclination_rad)
    
    px = cos_o * cos_w - sin_o * sin_w * cos_i
    py = sin_o * cos_w + cos_o * sin_w * cos_i
    pz = sin_w * sin_i
    
    qx = -cos_o * sin_w - sin_o * cos_w * cos_i
    qy = -sin_o * sin_w + cos_o * cos_w * cos_i
    qz = cos_w * sin_i
    
    pos_x = x_peri * px + y_peri * qx
    pos_y = x_peri * py + y_peri * qy
    pos_z = x_peri * pz + y_peri * qz
    
    vel_x = vx_peri * px + vy_peri * qx
    vel_y = vx_peri * py + vy_peri * qy
    vel_z = vx_peri * pz + vy_peri * qz
    
    return ((pos_x, pos_y, pos_z), (vel_x, vel_y, vel_z))


def hohmann_transfer(
    r1_altitude_m: float,
    r2_altitude_m: float,
    mu: float = MU_EARTH,
    body_radius_m: float = EARTH_RADIUS
) -> Dict[str, float]:
    """
    Computes Hohmann transfer orbit parameters between two co-planar circular orbits:
    - delta_v1: Burn 1 at periapsis to enter transfer ellipse
    - delta_v2: Burn 2 at apoapsis to circularize
    - total_delta_v: Sum of both burns
    - transfer_time_s: Half the period of the transfer ellipse
    """
    r1 = body_radius_m + r1_altitude_m
    r2 = body_radius_m + r2_altitude_m
    
    v1_circ = math.sqrt(mu / r1)
    v2_circ = math.sqrt(mu / r2)
    
    a_trans = (r1 + r2) / 2.0
    
    v_trans_peri = math.sqrt(mu * (2.0 / r1 - 1.0 / a_trans))
    v_trans_apo  = math.sqrt(mu * (2.0 / r2 - 1.0 / a_trans))
    
    dv1 = abs(v_trans_peri - v1_circ)
    dv2 = abs(v2_circ - v_trans_apo)
    total_dv = dv1 + dv2
    transfer_time = math.pi * math.sqrt((a_trans ** 3) / mu)
    
    return {
        "r1_altitude_km": r1_altitude_m / 1000.0,
        "r2_altitude_km": r2_altitude_m / 1000.0,
        "delta_v1_mps": round(dv1, 2),
        "delta_v2_mps": round(dv2, 2),
        "total_delta_v_mps": round(total_dv, 2),
        "transfer_time_minutes": round(transfer_time / 60.0, 2)
    }

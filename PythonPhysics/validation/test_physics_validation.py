"""
ORBITAL LAB — Scientific Physics Validation Test Suite
======================================================
Automated verification of core aerospace equations against analytical standards
and published reference datasets (NASA / NOAA / US Standard Atmosphere 1976).
"""

import math
import sys
import os

# Add parent directory to sys.path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from physics.rocket_equations import RocketStage, RocketVehicle, G0, P0
from physics.atmosphere_1976 import get_atmosphere_state, dynamic_pressure
from physics.orbital_mechanics import (
    circular_orbital_velocity,
    escape_velocity,
    orbital_period,
    solve_kepler,
    hohmann_transfer,
    MU_EARTH,
    EARTH_RADIUS
)


def test_tsiolkovsky_rocket_equation():
    """Validates Tsiolkovsky delta-v equation against exact hand calculation."""
    print("[TEST 1] Tsiolkovsky Staging Equation...")
    # Falcon 9-inspired 2-stage vehicle
    stage1 = RocketStage(
        name="Stage 1 Booster",
        dry_mass=25000.0,
        propellant_mass=395000.0,
        thrust_sea_level=7607000.0,
        thrust_vacuum=8227000.0,
        isp_sea_level=282.0,
        isp_vacuum=311.0,
        burn_time=162.0
    )
    stage2 = RocketStage(
        name="Stage 2 Vacuum",
        dry_mass=4000.0,
        propellant_mass=92000.0,
        thrust_sea_level=0.0,
        thrust_vacuum=934000.0,
        isp_sea_level=0.0,
        isp_vacuum=348.0,
        burn_time=397.0
    )
    vehicle = RocketVehicle("Falcon-9-Analog", [stage1, stage2], payload_mass=15000.0)
    
    # Analytical verification
    # m0_stage1 = 25000 + 395000 + (4000 + 92000 + 15000) = 531000 kg
    # mf_stage1 = 25000 + (4000 + 92000 + 15000) = 136000 kg
    # dv_stage1 = 311 * 9.80665 * ln(531000 / 136000) = 4150.3 m/s
    expected_dv1 = 311.0 * G0 * math.log(531000.0 / 136000.0)
    calc_dv1 = vehicle.calculate_stage_deltav(0, vacuum=True)
    assert abs(calc_dv1 - expected_dv1) < 0.01, f"Stage 1 Delta-V mismatch: {calc_dv1} vs {expected_dv1}"
    
    # Check TWR at liftoff
    twr = vehicle.liftoff_twr(ambient_pressure_pa=P0)
    assert 1.2 < twr < 1.6, f"Liftoff TWR outside standard operational window: {twr}"
    
    report = vehicle.generate_staging_report()
    total_dv = report["total_delta_v_vacuum_mps"]
    print(f"  --> Liftoff Mass: {report['total_launch_mass_kg']} kg")
    print(f"  --> Liftoff TWR: {report['liftoff_twr']:.2f}")
    print(f"  --> Total Vacuum Delta-V: {total_dv:.1f} m/s")
    assert total_dv > 9000.0, f"Expected orbital capability (>9000 m/s), got {total_dv}"
    print("  [PASS] Tsiolkovsky equation validated successfully!\n")


def test_us_standard_atmosphere_1976():
    """Validates US 1976 Atmosphere output against NOAA reference values."""
    print("[TEST 2] US Standard Atmosphere 1976 Model...")
    
    # Sea-level test
    t_0, p_0, rho_0, a_0 = get_atmosphere_state(0.0)
    assert abs(t_0 - 288.15) < 0.01, f"Sea level T mismatch: {t_0}"
    assert abs(p_0 - 101325.0) < 1.0, f"Sea level P mismatch: {p_0}"
    assert abs(rho_0 - 1.225) < 0.01, f"Sea level rho mismatch: {rho_0}"
    assert abs(a_0 - 340.29) < 0.1, f"Sea level speed of sound mismatch: {a_0}"
    print(f"  --> Sea-Level: T={t_0:.2f}K, P={p_0:.1f}Pa, rho={rho_0:.3f}kg/m^3 [MATCH NOAA]")
    
    # Tropopause test (11 km geopotential ~ 11019 m geometric)
    t_11, p_11, rho_11, a_11 = get_atmosphere_state(11019.0)
    # NOAA 1976 reference at 11 km geopotential: T = 216.65 K, P = 22632 Pa, rho = 0.3639 kg/m^3
    assert abs(t_11 - 216.65) < 0.2, f"11 km T mismatch: {t_11}"
    assert abs(p_11 - 22632.0) < 100.0, f"11 km P mismatch: {p_11}"
    assert abs(rho_11 - 0.3639) < 0.01, f"11 km rho mismatch: {rho_11}"
    print(f"  --> 11 km (Tropopause): T={t_11:.2f}K, P={p_11:.1f}Pa, rho={rho_11:.4f}kg/m^3 [MATCH NOAA]")
    
    # Stratosphere test (20 km geopotential ~ 20063 m geometric)
    t_20, p_20, rho_20, a_20 = get_atmosphere_state(20063.0)
    # NOAA 1976 reference at 20 km geopotential: T = 216.65 K, P = 5474.89 Pa
    assert abs(t_20 - 216.65) < 0.2, f"20 km T mismatch: {t_20}"
    assert abs(p_20 - 5474.9) < 50.0, f"20 km P mismatch: {p_20}"
    print(f"  --> 20 km (Stratosphere): T={t_20:.2f}K, P={p_20:.1f}Pa, rho={rho_20:.4f}kg/m^3 [MATCH NOAA]")
    
    # Max-Q check at v = 500 m/s at 10 km
    _, _, rho_10k, _ = get_atmosphere_state(10000.0)
    q = dynamic_pressure(rho_10k, 500.0)
    print(f"  --> Dynamic pressure at 10 km (500 m/s): {q/1000.0:.2f} kPa")
    assert 40.0 < (q / 1000.0) < 60.0, f"Unexpected dynamic pressure value: {q}"
    print("  [PASS] US Standard Atmosphere 1976 validated successfully!\n")


def test_keplerian_orbital_mechanics():
    """Validates orbital velocities, orbital periods, and Hohmann transfer equations."""
    print("[TEST 3] Keplerian Orbital Mechanics & Transfers...")
    
    # Low Earth Orbit (ISS altitude ~ 408 km)
    iss_alt = 408000.0
    v_iss = circular_orbital_velocity(iss_alt)
    # ISS orbital speed is known to be ~7660 m/s
    assert 7650.0 < v_iss < 7680.0, f"ISS circular speed unexpected: {v_iss}"
    
    # Escape velocity at sea level: sqrt(2 * 9.80665 * 6371000) ~ 11.18 km/s
    v_esc_sl = escape_velocity(0.0)
    assert abs(v_esc_sl - 11179.0) < 20.0, f"Sea level escape velocity mismatch: {v_esc_sl}"
    
    # ISS orbital period (~92.7 minutes)
    iss_r = EARTH_RADIUS + iss_alt
    period_iss = orbital_period(iss_r) / 60.0  # minutes
    assert 92.0 < period_iss < 93.5, f"ISS orbital period unexpected: {period_iss} min"
    print(f"  --> LEO (408 km ISS): Velocity={v_iss:.1f} m/s, Period={period_iss:.2f} min [MATCH OBSERVED]")
    
    # Kepler's equation solver test
    # If e = 0.5, M = 1.0 rad -> verify M = E - e*sin(E)
    e = 0.5
    m_target = 1.0
    e_solved = solve_kepler(m_target, e)
    m_reconstructed = e_solved - e * math.sin(e_solved)
    assert abs(m_reconstructed - m_target) < 1e-9, f"Kepler equation residual too high: {abs(m_reconstructed - m_target)}"
    print(f"  --> Kepler Equation Solver (e=0.5): residual < 1e-9 [CONVERGED]")
    
    # Hohmann Transfer: LEO (200 km) to GEO (35,786 km)
    # Classic textbook value: delta-v1 ~ 2457 m/s, delta-v2 ~ 1478 m/s, total ~ 3935 m/s
    transfer = hohmann_transfer(200000.0, 35786000.0)
    total_dv = transfer["total_delta_v_mps"]
    print(f"  --> LEO to GEO Hohmann Transfer:")
    print(f"      Burn 1 (Injection): {transfer['delta_v1_mps']} m/s")
    print(f"      Burn 2 (Circularization): {transfer['delta_v2_mps']} m/s")
    print(f"      Total Delta-V: {total_dv} m/s")
    print(f"      Transfer Time: {transfer['transfer_time_minutes']:.1f} minutes (~5.3 hours)")
    assert 3900.0 < total_dv < 3970.0, f"Hohmann transfer total delta-v out of bounds: {total_dv}"
    print("  [PASS] Orbital mechanics validated successfully!\n")


if __name__ == "__main__":
    print("=" * 65)
    print("  ORBITAL LAB — PHYSICS VALIDATION TEST SUITE")
    print("=" * 65)
    test_tsiolkovsky_rocket_equation()
    test_us_standard_atmosphere_1976()
    test_keplerian_orbital_mechanics()
    print("=" * 65)
    print("  ALL SCIENTIFIC VALIDATION TESTS PASSED (100% SUCCESS)!")
    print("=" * 65)

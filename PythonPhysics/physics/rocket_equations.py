"""
ORBITAL LAB — Rocket Propulsion & Staging Physics Engine
========================================================
Scientific Module implementing:
- Tsiolkovsky Rocket Equation (ideal and loss-corrected)
- Altitude-dependent specific impulse Isp(h)
- Propellant mass depletion rates dm/dt
- Multi-stage delta-V budgeting and structural mass fractions
- Thrust-to-Weight Ratio (TWR) across burn duration

All calculations in standard SI units (m, kg, s, N, Pa).
"""

import math
from typing import List, Dict, Any, Tuple

# Physical Constants
G0 = 9.80665              # Standard Earth gravity at sea level (m/s^2)
P0 = 101325.0             # Standard sea level atmospheric pressure (Pa)

class RocketStage:
    """Represents an individual rocket stage."""
    def __init__(
        self,
        name: str,
        dry_mass: float,          # kg (engines, tanks, avionics, interstage)
        propellant_mass: float,   # kg (usable fuel + oxidizer)
        thrust_sea_level: float,  # N
        thrust_vacuum: float,     # N
        isp_sea_level: float,     # seconds
        isp_vacuum: float,        # seconds
        burn_time: float,         # seconds
        diameter: float = 3.7     # meters (for frontal area calculation)
    ):
        self.name = name
        self.dry_mass = float(dry_mass)
        self.propellant_mass = float(propellant_mass)
        self.thrust_sea_level = float(thrust_sea_level)
        self.thrust_vacuum = float(thrust_vacuum)
        self.isp_sea_level = float(isp_sea_level)
        self.isp_vacuum = float(isp_vacuum)
        self.burn_time = float(burn_time)
        self.diameter = float(diameter)
        
        # Derived parameters
        self.initial_mass = self.dry_mass + self.propellant_mass
        self.mass_flow_rate = self.propellant_mass / self.burn_time if self.burn_time > 0 else 0.0
        self.cross_sectional_area = math.pi * (self.diameter / 2.0) ** 2

    def get_isp(self, ambient_pressure_pa: float) -> float:
        """
        Calculates specific impulse at given ambient atmospheric pressure.
        Linear interpolation between sea-level and vacuum pressure.
        """
        pressure_ratio = max(0.0, min(1.0, ambient_pressure_pa / P0))
        return self.isp_vacuum - (self.isp_vacuum - self.isp_sea_level) * pressure_ratio

    def get_thrust(self, ambient_pressure_pa: float) -> float:
        """
        Calculates engine thrust based on specific impulse variation.
        Thrust F = dm/dt * Isp * g0
        """
        current_isp = self.get_isp(ambient_pressure_pa)
        return self.mass_flow_rate * current_isp * G0


class RocketVehicle:
    """
    Multistage Launch Vehicle integrating stages in serial configuration.
    """
    def __init__(self, name: str, stages: List[RocketStage], payload_mass: float):
        self.name = name
        self.stages = stages
        self.payload_mass = float(payload_mass)

    def total_initial_mass(self) -> float:
        """Total launch mass including all stages and payload."""
        return sum(s.initial_mass for s in self.stages) + self.payload_mass

    def calculate_stage_deltav(self, stage_index: int, vacuum: bool = True) -> float:
        """
        Calculates delta-V for a specific stage using Tsiolkovsky equation:
        Delta_V = Isp * g0 * ln(m0 / mf)
        Where:
        m0 = mass of this stage + all upper stages + payload
        mf = dry mass of this stage + all upper stages + payload
        """
        if stage_index < 0 or stage_index >= len(self.stages):
            raise IndexError("Invalid stage index")

        stage = self.stages[stage_index]
        
        # Mass of all upper stages + payload
        upper_mass = sum(self.stages[i].initial_mass for i in range(stage_index + 1, len(self.stages))) + self.payload_mass
        
        m0 = stage.initial_mass + upper_mass
        mf = stage.dry_mass + upper_mass
        
        isp = stage.isp_vacuum if vacuum else stage.isp_sea_level
        return isp * G0 * math.log(m0 / mf)

    def calculate_total_deltav(self, vacuum: bool = True) -> float:
        """Sum of delta-V across all sequential stages."""
        return sum(self.calculate_stage_deltav(i, vacuum=vacuum) for i in range(len(self.stages)))

    def liftoff_twr(self, ambient_pressure_pa: float = P0) -> float:
        """
        Thrust-to-Weight Ratio (TWR) at liftoff.
        Must be > 1.0 for liftoff, typically 1.2 to 1.6 for orbital rockets.
        """
        stage1 = self.stages[0]
        liftoff_thrust = stage1.get_thrust(ambient_pressure_pa)
        total_weight = self.total_initial_mass() * G0
        return liftoff_thrust / total_weight if total_weight > 0 else 0.0

    def generate_staging_report(self) -> Dict[str, Any]:
        """Comprehensive scientific staging summary."""
        report = {
            "vehicle_name": self.name,
            "total_launch_mass_kg": self.total_initial_mass(),
            "payload_mass_kg": self.payload_mass,
            "liftoff_twr": self.liftoff_twr(),
            "stages": []
        }
        
        running_deltav = 0.0
        for i, stage in enumerate(self.stages):
            dv_vac = self.calculate_stage_deltav(i, vacuum=True)
            dv_sl = self.calculate_stage_deltav(i, vacuum=False)
            running_deltav += dv_vac
            
            stage_info = {
                "stage_number": i + 1,
                "name": stage.name,
                "dry_mass_kg": stage.dry_mass,
                "propellant_mass_kg": stage.propellant_mass,
                "burn_time_s": stage.burn_time,
                "isp_sea_level_s": stage.isp_sea_level,
                "isp_vac_s": stage.isp_vacuum,
                "delta_v_vacuum_mps": round(dv_vac, 2),
                "delta_v_sea_level_mps": round(dv_sl, 2),
                "cumulative_delta_v_mps": round(running_deltav, 2)
            }
            report["stages"].append(stage_info)
            
        report["total_delta_v_vacuum_mps"] = round(running_deltav, 2)
        return report

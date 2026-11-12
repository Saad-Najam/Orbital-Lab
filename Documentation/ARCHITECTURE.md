# Architecture

## Current (implemented)
```
Assets/Scripts/
  Simulation/   Atmosphere1976, RocketModel, AscentSimulator   (pure C#, no UnityEngine)
  Player/       FirstPersonController
  Interaction/  Interactable, InteractionSystem, RocketWorkstation, RocketWorkstationProxy
  Dev/          ReviewCapture
Assets/Editor/  FacilityBuilder, ReviewBridge
SimTests/       standalone .NET project compiling Simulation/*.cs and checking analytic values
PythonPhysics/  independent Python implementation + NASA API client
```

## Target layering
```
Presentation (scenes, UI Toolkit, interaction, Blender-sourced art)
      |
Domain (ScriptableObjects: ComponentDefinition, VehicleConfig, MissionDefinition, LessonDefinition;
        assembly graph, compatibility rules, curriculum state)
      |
Simulation (engine-free C#: atmosphere, aero, mass properties, propulsion abstraction,
            integrators, guidance, orbital mechanics, fault models)   <- tested with dotnet test
```
Rules: Simulation never references UnityEngine. Presentation reads simulation output; it never fabricates telemetry. Data labelled REAL / SIMULATED / HYPOTHETICAL end to end.

## Rendering
URP retained. Quality tiers (Low/Medium/High/Ultra) as URP asset variants. See GAP_ANALYSIS.md section I.

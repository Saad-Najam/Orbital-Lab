# ?? ORBITAL LAB
## NASA Space Engineering Simulator
### Project Codename: ORBITAL LAB

An immersive 3D space engineering simulation platform for the NASA Space Apps Challenge.

## Stack
- **3D Engine:** Unity 2022 LTS (URP)
- **3D Modeling:** Blender 4.x
- **Physics Engine:** Python 3.12 + NumPy + SciPy
- **Backend API:** Python FastAPI
- **Database:** SQLite (local) / Supabase (cloud optional)
- **AI Assistant:** Google Gemini API (free tier)
- **NASA Data:** NASA Open APIs

## Project Structure
```
OrbitalLab/
+-- UnityProject/        # Unity 2022 LTS project
+-- BlenderAssets/       # Blender source files
¦   +-- Rockets/
¦   +-- Facility/
¦   +-- Equipment/
+-- PythonPhysics/       # Scientific simulation engine
¦   +-- physics/         # Rocket equations, atmospheric model
¦   +-- nasa_api/        # NASA data integration
¦   +-- validation/      # Physics validation tests
+-- Docs/                # Documentation
+-- NASAData/            # Cached NASA datasets
+-- Audio/               # Sound assets
+-- Textures/            # Texture assets
+-- .env                 # API keys (never commit!)
```

## Setup
See INSTALL.md for setup instructions.

## NASA Space Apps Challenge
This project is an independent student-developed solution using publicly available NASA resources.

## License
MIT License

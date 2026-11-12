# ORBITAL LAB — Team Git & GitHub Collaboration Guide

This guide explains how you and your teammates can collaborate on the **Orbital Lab** project using Git, GitHub, Unity 6, and Blender without merge conflicts or missing asset errors.

---

## 1. How It Works (The Core Concept)

```
       [ YOUR COMPUTER ]                                [ TEAMMATE COMPUTER ]
   Unity / Blender / Code                             Unity / Blender / Code
            │                                                    ▲
      (git commit)                                         (git pull)
      (git push)                                                 │
            ▼                                                    │
     ═════════════════════════════════════════════════════════════════
                       [ GITHUB REPOSITORY ]
                       orbital-lab (main)
     ═════════════════════════════════════════════════════════════════
```

- **You push code & assets:** When you create a 3D model, write physics code, or update a scene, you commit and push to GitHub.
- **Teammate pulls:** Your teammate runs `git pull`. Unity automatically reloads the project, imports the new models and scripts, and everything stays in sync.

---

## 2. Step 1: Push Your Project to GitHub (You Do This First)

1. Open **[GitHub.com](https://github.com)** in your browser and log in.
2. Click the green **"New"** button to create a new repository:
   - **Repository Name:** `OrbitalLab` (or any name you prefer).
   - **Visibility:** Public or Private.
   - **Initialize with README / .gitignore:** **LEAVE UNCHECKED** (we already created them locally).
   - Click **Create repository**.
3. GitHub will show you your repository URL (e.g., `https://github.com/YourUsername/OrbitalLab.git`).
4. Open your terminal (PowerShell or Command Prompt) and run:
   ```powershell
   cd D:\Projects\OrbitalLab
   git remote add origin https://github.com/YourUsername/OrbitalLab.git
   git branch -M main
   git push -u origin main
   ```
5. Your entire project is now live on GitHub!

---

## 3. Step 2: How Your Teammate Starts Working

Send these steps to your teammate:

### A. Prerequisites for Your Teammate:
- Install **Git** ([git-scm.com](https://git-scm.com/)).
- Install **Unity Hub** and download the matching Unity version: **Unity 6 (`6000.6.9f1`)**.
- Install **Blender** (if they are doing 3D modeling).
- Install **VS Code** with the C# extension.

### B. Cloning the Repository:
1. Open terminal on their computer and run:
   ```bash
   git clone https://github.com/YourUsername/OrbitalLab.git
   ```
2. Open **Unity Hub**:
   - Click the **"Add"** button (Add project from disk).
   - Navigate into the cloned folder and select `OrbitalLab\UnityProject`.
   - Click **Open**.
3. Unity will open, download packages, and build the cache. They are ready to develop!

---

## 4. Daily Development Routine (The 3-Step Rhythm)

### Start of the day (Teammate or You):
Always get the latest work before you touch any files:
```bash
git pull origin main
```

### During work:
- Create your models in Blender, save or export `.fbx` into `Assets/Models/`.
- Write C# scripts in VS Code.
- Test in Unity Editor.

### End of task / Ready to share:
Commit and push your work:
```bash
git status
git add .
git commit -m "feat: added modular fuel tank mesh and fuel burn script"
git push origin main
```

When your teammate runs `git pull`, Unity will detect the new files, show a brief progress bar importing them, and display them in the Project panel!

---

## 5. The 4 Golden Rules for Unity Game Dev with Git

### Rule 1: NEVER Forget `.meta` Files!
Every single asset in Unity (a 3D model, script, texture, or audio clip) has a paired `.meta` file:
- `Falcon9.fbx` ➔ `Falcon9.fbx.meta`
- `RocketEngine.cs` ➔ `RocketEngine.cs.meta`

The `.meta` file holds Unity's internal GUID (unique ID). If you commit an asset without its `.meta` file, your teammate's Unity will generate a *different* ID, breaking all inspector references and script attachments!
- **How to ensure safety:** Always use `git add .` so both the asset and its `.meta` file are staged together.

### Rule 2: Never Push `Library/` or `Temp/`
The `Library/` folder is Unity's internal cache (several gigabytes). Our `.gitignore` is already configured to ignore it. When your teammate opens the project, Unity rebuilds `Library/` automatically from `Assets/` and `Packages/`.

### Rule 3: Avoid Scene Merge Conflicts with "Prefabs"
In Unity, if two people edit `MainHangarScene.unity` at the same time, Git cannot easily merge the binary/YAML scene changes.
- **The Pro Solution: Work in Prefabs!**
  - Person A edits `HangarEnvironment.prefab`.
  - Person B edits `RocketAssembly.prefab`.
  - Both prefabs live in the scene, but because they are separate files, both teammates can commit and push simultaneously with zero conflicts!

### Rule 4: Use Feature Branches
For major features, don't work directly on `main`:
1. Create a branch: `git checkout -b feature/rocket-staging`
2. Work, commit, and push: `git push origin feature/rocket-staging`
3. On GitHub, create a **Pull Request (PR)** to merge into `main`.

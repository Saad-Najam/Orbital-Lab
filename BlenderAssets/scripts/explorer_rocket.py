"""
ORBITAL LAB - Explorer Rocket ("ORBITAL-1 educational vehicle").
Generic, representative two-stage launch vehicle (NOT a model of any real rocket).
Run inside Blender:  exec(open(path).read())
Units: metres, +Z up, base of Stage 1 skirt at z=0. Every part is its own object (name = part id used by parts.json).
Propulsion/electrical parts are NON-FUNCTIONAL concept visuals.
"""
import bpy, bmesh, math, json
from mathutils import Vector, Matrix, Euler

# ------------------------------------------------------------------ reset
for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
for d in (bpy.data.meshes, bpy.data.materials, bpy.data.curves):
    for x in list(d): d.remove(x)

# ------------------------------------------------------------------ materials
def mat(name, color, metal=0.0, rough=0.5, emit=None):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Metallic"].default_value = metal
    b.inputs["Roughness"].default_value = rough
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1); b.inputs["Emission Strength"].default_value = 0.6
    return m
M = dict(
    white=mat("PaintWhite", (0.86, 0.87, 0.89), 0.05, 0.38),
    black=mat("PaintBlack", (0.02, 0.02, 0.022), 0.15, 0.5),
    alu=mat("BrushedAluminium", (0.66, 0.68, 0.71), 1.0, 0.32),
    steel=mat("StainlessSteel", (0.5, 0.51, 0.53), 1.0, 0.25),
    darksteel=mat("DarkSteel", (0.12, 0.125, 0.14), 0.95, 0.4),
    foam=mat("InsulationFoam", (0.78, 0.4, 0.1), 0.0, 0.85),
    copper=mat("CopperChamber", (0.72, 0.38, 0.2), 1.0, 0.3),
    nozzle=mat("NozzleAlloy", (0.2, 0.19, 0.19), 0.9, 0.35),
    carbon=mat("CarbonComposite", (0.025, 0.027, 0.03), 0.3, 0.35),
    gold=mat("GoldFoil", (0.85, 0.62, 0.18), 1.0, 0.22),
    solar=mat("SolarCell", (0.03, 0.07, 0.25), 0.4, 0.2),
    pcb=mat("PCBGreen", (0.02, 0.28, 0.12), 0.2, 0.45),
    chip=mat("Chip", (0.03, 0.03, 0.035), 0.1, 0.5),
    battery=mat("BatteryBlue", (0.08, 0.2, 0.45), 0.3, 0.45),
    harness=mat("HarnessBlack", (0.01, 0.01, 0.012), 0.0, 0.6),
    orange=mat("SafetyOrange", (0.9, 0.38, 0.05), 0.1, 0.4),
    glass=mat("Radome", (0.8, 0.82, 0.85), 0.0, 0.3),
    cop2=mat("CopperWire", (0.8, 0.45, 0.25), 1.0, 0.3),
    pipeC=mat("CryoPipe", (0.75, 0.8, 0.85), 1.0, 0.2),
    pipeF=mat("FuelPipe", (0.55, 0.45, 0.25), 1.0, 0.3),
)
MATLIST = list(M.values())

# ------------------------------------------------------------------ mesh builder
class B:
    def __init__(s): s.bm = bmesh.new(); s.mi = {}
    def m(s, key):
        if key not in s.mi: s.mi[key] = len(s.mi)
        return s.mi[key]
    def _tag(s, verts, key, smooth=False):
        for v in verts:
            for f in v.link_faces: f.material_index = s.m(key); f.smooth = smooth
    def lathe(s, prof, key, seg=40, closed=False, a0=0.0, a1=2 * math.pi, off=(0, 0, 0)):
        bm = s.bm; full = abs((a1 - a0) - 2 * math.pi) < 1e-6
        rings = []
        for r, z in prof:
            if r < 1e-5: rings.append([bm.verts.new((off[0], off[1], z + off[2]))])
            else:
                n = seg if full else seg + 1
                ring = []
                for i in range(n):
                    a = a0 + (a1 - a0) * i / (seg if not full else seg)
                    ring.append(bm.verts.new((off[0] + r * math.cos(a), off[1] + r * math.sin(a), z + off[2])))
                rings.append(ring)
        pairs = list(zip(rings, rings[1:])) + ([(rings[-1], rings[0])] if closed else [])
        made = []
        for a, b in pairs:
            n = seg if full else seg
            for i in range(n):
                j = (i + 1) % len(a) if full else i + 1
                if len(a) == 1 and len(b) == 1: continue
                try:
                    if len(a) == 1: f = bm.faces.new((a[0], b[j], b[i]))
                    elif len(b) == 1: f = bm.faces.new((a[i], a[j], b[0]))
                    else: f = bm.faces.new((a[i], a[j], b[j], b[i]))
                except ValueError: continue
                f.material_index = s.m(key); f.smooth = True; made.append(f)
        return made
    def box(s, c, size, key, rot=(0, 0, 0)):
        M4 = Matrix.Translation(c) @ Euler(rot).to_matrix().to_4x4() @ Matrix.Diagonal((*size, 1))
        r = bmesh.ops.create_cube(s.bm, size=1.0, matrix=M4); s._tag(r["verts"], key)
    def cyl(s, c, r, h, key, axis="Z", seg=24, r2=None):
        rot = {"Z": (0, 0, 0), "X": (0, math.pi / 2, 0), "Y": (-math.pi / 2, 0, 0)}[axis]
        M4 = Matrix.Translation(c) @ Euler(rot).to_matrix().to_4x4()
        res = bmesh.ops.create_cone(s.bm, cap_ends=True, segments=seg, radius1=r, radius2=r if r2 is None else r2, depth=h, matrix=M4)
        s._tag(res["verts"], key, smooth=True)
    def sphere(s, c, r, key, seg=16):
        res = bmesh.ops.create_uvsphere(s.bm, u_segments=seg, v_segments=max(6, seg // 2), radius=r, matrix=Matrix.Translation(c))
        s._tag(res["verts"], key, smooth=True)
    def tube(s, pts, r, key, seg=8):
        for p, q in zip(pts, pts[1:]):
            p, q = Vector(p), Vector(q); d = q - p; L = d.length
            if L < 1e-6: continue
            rot = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
            res = bmesh.ops.create_cone(s.bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=L, matrix=Matrix.Translation((p + q) / 2) @ rot)
            s._tag(res["verts"], key, smooth=True)
    def finish(s, name, parent=None, recalc=True):
        if recalc: bmesh.ops.recalc_face_normals(s.bm, faces=s.bm.faces)
        me = bpy.data.meshes.new(name); s.bm.to_mesh(me); s.bm.free()
        for key, i in sorted(s.mi.items(), key=lambda kv: kv[1]): me.materials.append(M[key])
        ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
        if parent: ob.parent = parent
        return ob

def empty(name, parent=None):
    e = bpy.data.objects.new(name, None); bpy.context.collection.objects.link(e); e.empty_display_size = 0.5
    if parent: e.parent = parent
    return e

# ------------------------------------------------------------------ profile helpers
R = 1.5        # outer skin radius
RT = 1.40      # tank radius
def shell(r, t, z0, z1, seams=0.0, seam_every=3.5):
    """closed hollow cylinder profile (outer up, inner down) with optional panel seams"""
    outer = [(r, z0)]
    if seams:
        z = z0 + seam_every
        while z < z1 - 0.2:
            outer += [(r, z - 0.025), (r - 0.012, z - 0.015), (r - 0.012, z + 0.015), (r, z + 0.025)]; z += seam_every
    outer.append((r, z1))
    inner = [(r - t, z1), (r - t, z0)]
    return outer + inner
def tank_profile(r, z0, z1, dome, n=10):
    p = []
    for i in range(n + 1):
        a = (math.pi / 2) * i / n; p.append((r * math.sin(a), z0 + dome * (1 - math.cos(a))))
    p.append((r, z1 - dome))
    for i in range(1, n + 1):
        a = (math.pi / 2) * i / n; p.append((r * math.cos(a), z1 - dome + dome * math.sin(a)))
    p[0] = (0, p[0][1]); p[-1] = (0, p[-1][1])
    return p

# ------------------------------------------------------------------ hierarchy
root = empty("ORBITAL1_Rocket")
st1 = empty("Assembly_Stage1", root); isg = empty("Assembly_Interstage", root); st2 = empty("Assembly_Stage2", root)
avb = empty("Assembly_AvionicsBay", root); pls = empty("Assembly_PayloadSection", root)
S1_ENG_TOP = 2.2
parts = []   # (name, assembly)
def reg(ob, asm): parts.append((ob.name, asm.name))

# ================================================================== STAGE 1  (z 0 .. 22)
b = B(); b.lathe(shell(R, 0.035, 0.0, 22.0, seams=1), "white"); reg(b.finish("S1_Skin", st1), st1)
b = B(); b.lathe(tank_profile(RT, 2.4, 9.0, 0.9), "alu"); b.lathe([(RT, 3.3), (RT - 0.0, 3.3)], "alu", seg=4)
for z in (4.5, 6.0, 7.5):   # slosh baffles (rings)
    b.lathe([(RT, z), (RT - 0.35, z), (RT - 0.35, z + 0.03), (RT, z + 0.03)], "steel", closed=False)
reg(b.finish("S1_FuelTank", st1), st1)
b = B(); b.lathe(tank_profile(RT, 10.0, 21.0, 0.9), "alu")
for z in (12.0, 14.5, 17.0, 19.5):
    b.lathe([(RT, z), (RT - 0.35, z), (RT - 0.35, z + 0.03), (RT, z + 0.03)], "steel")
reg(b.finish("S1_OxidizerTank", st1), st1)
b = B(); b.lathe(shell(R - 0.06, 0.05, 9.0, 10.0), "darksteel")
for i in range(12):
    a = 2 * math.pi * i / 12; b.box((1.3 * math.cos(a), 1.3 * math.sin(a), 9.5), (0.07, 0.07, 1.0), "steel", (0, 0, a))
reg(b.finish("S1_Intertank", st1), st1)
b = B(); b.lathe(shell(R - 0.06, 0.05, 21.0, 22.0), "darksteel"); reg(b.finish("S1_ForwardSkirt", st1), st1)
# Thrust structure: plate + radial beams
b = B(); b.cyl((0, 0, S1_ENG_TOP), 1.38, 0.1, "darksteel", seg=48)
for i in range(9):
    a = 2 * math.pi * i / 9 if i else 0
    p = (0.95 * math.cos(a), 0.95 * math.sin(a)) if i else (0, 0)
    b.cyl((p[0], p[1], S1_ENG_TOP - 0.1), 0.3, 0.2, "steel", seg=20)
for i in range(8):
    a = math.pi * i / 4; b.box((0.7 * math.cos(a), 0.7 * math.sin(a), S1_ENG_TOP + 0.1), (1.4, 0.08, 0.2), "steel", (0, 0, a))
reg(b.finish("S1_ThrustStructure", st1), st1)
b = B(); b.cyl((0, 0, 0.9), 1.42, 0.06, "foam", seg=48); reg(b.finish("S1_BaseHeatShield", st1), st1)

def engine_profile(ez, scale=1.0):
    return [(0.26 * scale, ez - 0.1), (0.26 * scale, ez - 0.95), (0.13 * scale, ez - 1.15), (0.17 * scale, ez - 1.35), (0.26 * scale, ez - 1.7),
            (0.38 * scale, ez - 2.15), (0.47 * scale, ez - 2.6)]
# Center engine - anatomy split into labelled sub-parts
ez = S1_ENG_TOP - 0.1
b = B(); b.cyl((0, 0, ez - 0.02), 0.32, 0.1, "steel", seg=32)
for i in range(10):
    a = 2 * math.pi * i / 10; b.cyl((0.18 * math.cos(a), 0.18 * math.sin(a), ez + 0.06), 0.035, 0.1, "alu", seg=10)
reg(b.finish("S1E_Injector", st1), st1)
b = B(); b.lathe([(0.27, ez - 0.07), (0.27, ez - 0.95), (0.14, ez - 1.15)], "copper"); reg(b.finish("S1E_Chamber", st1), st1)
b = B(); b.lathe([(0.14, ez - 1.15), (0.17, ez - 1.35), (0.26, ez - 1.7), (0.38, ez - 2.15), (0.47, ez - 2.6)], "nozzle"); reg(b.finish("S1E_Nozzle", st1), st1)
b = B(); b.cyl((0.5, 0, ez - 0.25), 0.17, 0.5, "darksteel", seg=24); b.cyl((0.5, 0, ez - 0.55), 0.22, 0.12, "steel", seg=24)
b.tube([(0.5, 0, ez - 0.5), (0.5, 0, ez - 0.9), (0.2, 0, ez - 1.0)], 0.04, "pipeF"); b.tube([(0.5, 0.1, ez), (0.5, 0.1, ez + 0.4)], 0.05, "pipeC")
reg(b.finish("S1E_Turbopump", st1), st1)
b = B(); b.lathe([(0.34, ez + 0.0), (0.34, ez + 0.06), (0.4, ez + 0.06), (0.4, ez + 0.0)], "steel")
for a in (0, math.pi / 2):
    b.tube([(0.4 * math.cos(a), 0.4 * math.sin(a), ez + 0.0), (0.7 * math.cos(a), 0.7 * math.sin(a), ez + 0.2)], 0.035, "darksteel")
reg(b.finish("S1E_GimbalMount", st1), st1)
# 8 ring engines (simplified)
for i in range(8):
    a = 2 * math.pi * i / 8; b = B(); b.lathe(engine_profile(ez), "nozzle", seg=28, off=(0.95 * math.cos(a), 0.95 * math.sin(a), 0))
    b.cyl((0.95 * math.cos(a), 0.95 * math.sin(a), ez - 0.55), 0.28, 0.9, "copper", seg=20)
    reg(b.finish(f"S1_Engine_{i + 1}", st1), st1)
# Feed lines (generic, concept)
b = B(); b.tube([(0.9, 0.0, 10.05), (0.9, 0.0, 9.0), (0.9, 0.0, 3.0), (0.65, 0.0, S1_ENG_TOP - 0.15)], 0.1, "pipeC"); reg(b.finish("S1_OxidizerFeedLine", st1), st1)
b = B(); b.tube([(-0.5, -0.6, 2.9), (-0.5, -0.6, 2.4), (-0.3, -0.3, S1_ENG_TOP - 0.1)], 0.07, "pipeF"); reg(b.finish("S1_FuelFeedLine", st1), st1)
b = B();
for i, (x, y) in enumerate(((-0.8, 0.5), (-0.8, -0.1))): b.sphere((x, y, 1.6), 0.28, "carbon")
reg(b.finish("S1_PressurantBottles", st1), st1)
# Grid fins (4)
b = B()
for k in range(4):
    a = math.pi / 4 + k * math.pi / 2; c = Vector(((R + 0.12) * math.cos(a), (R + 0.12) * math.sin(a), 21.2))
    for i in range(5):
        for j in range(4):
            pass
    fr = Matrix.Rotation(a, 3, "Z")
    def loc(x, y, z): return c + fr @ Vector((x, y, z))
    b.box(loc(0, 0, 0), (0.05, 0.9, 0.8), "darksteel", (0, 0, a)) if False else None
    for i in range(-2, 3):
        b.box(loc(0, i * 0.2, 0), (0.04, 0.025, 0.8), "darksteel", (0, 0, a))
    for j in range(-2, 3):
        b.box(loc(0, 0, j * 0.2), (0.04, 0.9, 0.025), "darksteel", (0, 0, a))
    b.box(loc(0, 0, 0.41), (0.05, 0.92, 0.04), "steel", (0, 0, a)); b.box(loc(0, 0, -0.41), (0.05, 0.92, 0.04), "steel", (0, 0, a))
reg(b.finish("S1_GridFins", st1), st1)
# Landing legs (4)
b = B()
for k in range(4):
    a = k * math.pi / 2; top = Vector((1.55 * math.cos(a), 1.55 * math.sin(a), 4.5)); bot = Vector((3.2 * math.cos(a), 3.2 * math.sin(a), -0.2))
    b.tube([top, bot], 0.07, "carbon", seg=10); b.tube([Vector((1.55 * math.cos(a), 1.55 * math.sin(a), 1.6)), Vector((2.6 * math.cos(a), 2.6 * math.sin(a), 1.2))], 0.04, "darksteel")
    b.cyl(bot, 0.22, 0.05, "darksteel", seg=16)
reg(b.finish("S1_LandingLegs", st1), st1)

# ================================================================== INTERSTAGE + SEPARATION (z 22 .. 25)
b = B(); b.lathe(shell(R, 0.03, 22.0, 25.0), "black"); reg(b.finish("IS_InterstageShell", isg), isg)
b = B(); b.lathe([(R - 0.05, 21.95), (R - 0.05, 22.1), (R - 0.2, 22.1), (R - 0.2, 21.95)], "orange")
for i in range(8):
    a = 2 * math.pi * i / 8; b.cyl(((R - 0.3) * math.cos(a), (R - 0.3) * math.sin(a), 22.2), 0.07, 0.25, "steel", seg=12)
reg(b.finish("SEP_Ring_Pushers", isg), isg)

# ================================================================== STAGE 2  (z 25 .. 37)
b = B(); b.lathe(shell(R, 0.03, 25.0, 37.0, seams=1, seam_every=3.0), "white"); reg(b.finish("S2_Skin", st2), st2)
b = B(); b.lathe(tank_profile(RT, 26.6, 30.6, 0.8), "alu"); b.lathe([(RT, 27.8), (RT - 0.3, 27.8), (RT - 0.3, 27.83), (RT, 27.83)], "steel"); reg(b.finish("S2_FuelTank", st2), st2)
b = B(); b.lathe(tank_profile(RT, 31.2, 36.2, 0.8), "alu"); b.lathe([(RT, 33.2), (RT - 0.3, 33.2), (RT - 0.3, 33.23), (RT, 33.23)], "steel"); reg(b.finish("S2_OxidizerTank", st2), st2)
b = B(); b.lathe(shell(R - 0.06, 0.05, 30.6, 31.2), "darksteel"); reg(b.finish("S2_Intertank", st2), st2)
b = B(); b.lathe(shell(R - 0.06, 0.05, 36.2, 37.0), "darksteel"); reg(b.finish("S2_ForwardSkirt", st2), st2)
b = B(); b.cyl((0, 0, 26.3), 1.35, 0.08, "darksteel", seg=40)
for i in range(6):
    a = math.pi * i / 3; b.box((0.6 * math.cos(a), 0.6 * math.sin(a), 26.45), (1.2, 0.07, 0.25), "steel", (0, 0, a))
reg(b.finish("S2_ThrustStructure", st2), st2)
E2 = 26.2     # vacuum engine injector plane
b = B(); b.cyl((0, 0, E2 - 0.02), 0.28, 0.08, "steel", seg=32)
for i in range(8):
    a = 2 * math.pi * i / 8; b.cyl((0.15 * math.cos(a), 0.15 * math.sin(a), E2 + 0.05), 0.03, 0.08, "alu", seg=10)
reg(b.finish("S2E_Injector", st2), st2)
b = B(); b.lathe([(0.24, E2 - 0.06), (0.24, E2 - 0.8), (0.12, E2 - 1.0)], "copper"); reg(b.finish("S2E_Chamber", st2), st2)
b = B()
prof = [(0.12, E2 - 1.0), (0.15, E2 - 1.2), (0.24, E2 - 1.5), (0.4, E2 - 1.9), (0.62, E2 - 2.3), (0.9, E2 - 2.7), (1.1, E2 - 2.95), (1.22, E2 - 3.1)]
b.lathe(prof, "nozzle", seg=48); reg(b.finish("S2E_NozzleExtension", st2), st2)
b = B(); b.cyl((-0.45, 0, E2 - 0.2), 0.14, 0.45, "darksteel", seg=20); b.cyl((-0.45, 0, E2 - 0.5), 0.19, 0.1, "steel", seg=20)
b.tube([(-0.45, 0, E2 - 0.45), (-0.3, 0, E2 - 0.8)], 0.035, "pipeF"); reg(b.finish("S2E_Turbopump", st2), st2)
b = B()
for a in (0, math.pi / 2):
    p = Vector((0.9 * math.cos(a), 0.9 * math.sin(a), E2 + 0.25)); q = Vector((0.3 * math.cos(a), 0.3 * math.sin(a), E2 - 0.05))
    b.tube([p, q], 0.045, "steel"); b.sphere(p, 0.07, "darksteel"); b.sphere(q, 0.06, "darksteel")
reg(b.finish("S2E_GimbalActuators", st2), st2)
b = B(); b.tube([(0.6, 0.5, 31.25), (0.6, 0.5, 30.6), (0.6, 0.5, 27.0), (0.35, 0.1, E2 + 0.1)], 0.07, "pipeC"); reg(b.finish("S2_OxidizerFeedLine", st2), st2)
b = B(); b.sphere((0.9, -0.5, 36.55), 0.3, "carbon"); b.sphere((0.9, 0.5, 36.55), 0.3, "carbon"); reg(b.finish("S2_PressurantBottles", st2), st2)

# ================================================================== AVIONICS BAY (z 37 .. 38.2)
b = B(); b.lathe(shell(R, 0.04, 37.0, 38.2), "white"); b.cyl((0, 0, 37.15), R - 0.06, 0.06, "darksteel", seg=48); reg(b.finish("AV_BayStructure", avb), avb)
def pcb_unit(name, c, size, key="pcb", extra=None):
    b = B(); b.box(c, size, "darksteel")                           # chassis
    b.box((c[0], c[1], c[2] + size[2] / 2 + 0.005), (size[0] * 0.9, size[1] * 0.9, 0.01), "pcb")
    for i in range(3):
        for j in range(2):
            b.box((c[0] - size[0] * 0.25 + i * size[0] * 0.25, c[1] - size[1] * 0.2 + j * size[1] * 0.35, c[2] + size[2] / 2 + 0.02), (size[0] * 0.14, size[1] * 0.2, 0.025), "chip")
    if extra: extra(b)
    reg(b.finish(name, avb), avb)
pcb_unit("AV_FlightComputer", (0.55, 0.35, 37.32), (0.55, 0.38, 0.16))
pcb_unit("AV_IMU", (0.0, 0.0, 37.3), (0.22, 0.22, 0.14))
pcb_unit("AV_DataAcquisition", (-0.1, 0.65, 37.3), (0.4, 0.28, 0.12))
b = B(); b.box((-0.65, 0.45, 37.33), (0.55, 0.32, 0.22), "battery")
for i in range(4): b.box((-0.65 - 0.2 + i * 0.13, 0.45, 37.46), (0.1, 0.28, 0.01), "alu")
reg(b.finish("AV_BatteryPack", avb), avb)
pcb_unit("AV_PowerDistributionUnit", (-0.55, -0.55, 37.3), (0.42, 0.3, 0.14))
pcb_unit("AV_TelemetryTransmitter", (0.55, -0.5, 37.28), (0.36, 0.26, 0.12))
b = B(); b.cyl((1.0 * math.cos(0.5), 1.0 * math.sin(0.5), 37.5), 0.01, 0.01, "alu")  # placeholder-free: antennas
a = 0.6; b.cyl((R * math.cos(a), R * math.sin(a), 37.7), 0.1, 0.05, "glass", seg=20); b.box(((R + 0.02) * math.cos(a), (R + 0.02) * math.sin(a), 37.9), (0.03, 0.03, 0.4), "alu", (0, 0, a))
reg(b.finish("AV_TelemetryAntenna", avb), avb)
b = B(); a = 2.4; b.cyl(((R + 0.01) * math.cos(a), (R + 0.01) * math.sin(a), 37.6), 0.12, 0.04, "carbon", axis="Z", seg=24); reg(b.finish("AV_GPSReceiverAntenna", avb), avb)
# Harness: bay -> down the side to engine section
b = B(); pts = [(0.4, 0.3, 37.3), (0.9, 0.9, 37.25), (R + 0.04, 0.2, 37.0), (R + 0.04, 0.2, 22.0), (R + 0.04, 0.2, 3.0), (1.2, 0.2, 2.4)]
b.tube(pts, 0.035, "harness"); b.tube([(p[0] + 0.0, p[1] + 0.07, p[2]) for p in pts[2:]], 0.02, "cop2")
reg(b.finish("AV_WiringHarness", avb), avb)

# ================================================================== PAYLOAD SECTION (z 37 .. 48)
b = B(); b.lathe(shell(0.95, 0.03, 38.2, 38.9), "carbon", closed=True)
b.lathe([(0.95, 38.2), (0.6, 38.9), (0.57, 38.9), (0.92, 38.2)], "carbon", closed=False); reg(b.finish("PL_PayloadAdapter", pls), pls)
b = B(); b.box((0, 0, 40.0), (1.1, 1.1, 2.2), "gold")
b.box((0, 0, 41.15), (1.15, 1.15, 0.06), "darksteel"); b.box((0, 0, 38.95), (0.8, 0.8, 0.06), "darksteel")
reg(b.finish("PL_SatelliteBus", pls), pls)
b = B()
for sx in (-1, 1):
    for k in range(3):
        b.box((sx * (0.6 + 0.02 * k), 0, 40.0), (0.025, 0.95, 1.9), "solar", (0, 0, 0))
reg(b.finish("PL_SolarArrays", pls), pls)
b = B(); b.lathe([(0.0, 41.3), (0.18, 41.32), (0.36, 41.4), (0.0, 41.55)], "alu", seg=32, off=(0, 0, 0)); b.tube([(0, 0, 41.15), (0, 0, 41.4)], 0.03, "darksteel")
reg(b.finish("PL_Antenna", pls), pls)
# Fairing halves: barrel 37 -> 42, ogive 42 -> 48
def fairing(name, a0, a1):
    outer = [(R, 37.0), (R, 42.0)]
    for i in range(1, 25):
        t = i / 24; outer.append((max(0.0, R * (1 - t ** 2.2) ** 0.55), 42.0 + 6.0 * t))
    outer[-1] = (0.03, 48.0)
    inner = [(max(0.0, r - 0.04), z - (0.04 if r < 0.1 else 0)) for r, z in reversed(outer)]
    b = B(); b.lathe(outer + inner, "white", seg=24, closed=True, a0=a0, a1=a1)
    b.lathe([(R, 37.0), (R, 37.05)], "black", seg=2, a0=a0, a1=a0 + 0.001) if False else None
    return b.finish(name, pls, recalc=False)
reg(fairing("PL_FairingHalf_A", -math.pi / 2, math.pi / 2), pls); reg(fairing("PL_FairingHalf_B", math.pi / 2, 3 * math.pi / 2), pls)

# ------------------------------------------------------------------ export part registry
info = []
for name, asm in parts:
    ob = bpy.data.objects[name]; bb = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb))); mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
    info.append(dict(id=name, assembly=asm, center=[round(x, 3) for x in (mn + mx) / 2], size=[round(x, 3) for x in (mx - mn)], tris=sum(len(p.vertices) - 2 for p in ob.data.polygons)))
json.dump(info, open(r"D:\Projects\OrbitalLab\BlenderAssets\Rockets\explorer_rocket_parts.json", "w"), indent=1)
print(len(parts), "parts;", sum(i["tris"] for i in info), "triangles")

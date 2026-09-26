"""
Generates the portfolio workshop: an industrial-design loft with steel-framed (Belgian profile)
grid windows, a mezzanine, and warm wood furniture.

Run inside Blender (Text Editor -> Run Script, or via the Blender MCP):
    exec(open(r"<repo>/Art/Blender/build_workshop.py").read())

Conventions (so the export drops straight into Unity):
  * 1 unit = 1 m, Z up. Room spans x [-9, 9], y [-7, 7]; floor top at z = 0.
  * The back walls are at +Y and -X; the front and right sides are open (diorama cutaway).
  * Each station is an empty named ST_<Name> whose children are its furniture. Its front
    faces local -Y, and ST_<Name>_Approach marks where the player stands.
  * Everything lives in the "Workshop" collection; only that collection is exported.
"""
import math
import os

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

COLLECTION = "Workshop"
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) if "__file__" in globals() else None

# Colors are authored in sRGB (what you see in Unity) and converted to linear for Blender.
PALETTE = {
    "Concrete": (0.62, 0.62, 0.60),
    "ConcreteEdge": (0.45, 0.45, 0.44),
    "WallWhite": (0.93, 0.92, 0.89),
    "Steel": (0.10, 0.10, 0.11),
    "Glass": (0.74, 0.86, 0.93),
    "Wood": (0.82, 0.60, 0.38),
    "WoodDark": (0.55, 0.36, 0.22),
    "Plywood": (0.92, 0.79, 0.58),
    "Paper": (0.98, 0.97, 0.93),
    "Accent": (0.96, 0.46, 0.18),
    "Blue": (0.20, 0.44, 0.86),
    "Plant": (0.33, 0.58, 0.34),
    "Terracotta": (0.80, 0.47, 0.34),
    "Screen": (0.14, 0.18, 0.25),
    "Cork": (0.76, 0.58, 0.40),
    "CabinetWhite": (0.96, 0.96, 0.95),
    "Bulb": (1.00, 0.94, 0.76),
}


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def material(name):
    mat = bpy.data.materials.get("M_" + name) or bpy.data.materials.new("M_" + name)
    lin = tuple(srgb_to_linear(c) for c in PALETTE[name]) + (1.0,)
    mat.diffuse_color = lin
    try:
        if not mat.use_nodes:
            mat.use_nodes = True
    except AttributeError:
        pass
    if mat.node_tree:
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = lin
            bsdf.inputs["Roughness"].default_value = 0.85
            if name == "Glass":  # clear glass in Blender; Unity swaps in Portfolio/ToonGlass
                bsdf.inputs["Roughness"].default_value = 0.05
                bsdf.inputs["Transmission Weight"].default_value = 1.0
    if name == "Glass":
        mat.diffuse_color = lin[:3] + (0.25,)
        mat.surface_render_method = "BLENDED"
    return mat


# ---------------------------------------------------------------- scene plumbing

def reset_collection():
    col = bpy.data.collections.get(COLLECTION)
    if col:
        for obj in list(col.all_objects):
            data = obj.data
            bpy.data.objects.remove(obj, do_unlink=True)
            if data is not None and data.users == 0 and isinstance(data, bpy.types.Mesh):
                bpy.data.meshes.remove(data)
    else:
        col = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(col)
    for obj in [o for o in bpy.data.objects if not o.users_collection]:  # orphans from a failed run
        bpy.data.objects.remove(obj)
    for name in ("Cube",):
        obj = bpy.data.objects.get(name)
        if obj and obj.type == "MESH" and not obj.users_collection[0] == col:
            bpy.data.objects.remove(obj, do_unlink=True)
    return col


def empty(name, loc=(0, 0, 0), rot_z=0.0, parent=None, size=0.5):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = size
    obj.location = (*loc, 0.0) if len(loc) == 2 else loc
    obj.rotation_euler = (0, 0, math.radians(rot_z))
    bpy.data.collections[COLLECTION].objects.link(obj)
    if parent:
        obj.parent = parent
    return obj


class MeshBuilder:
    """Accumulates boxes / cylinders / blobs into one mesh object with multiple materials."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []

    def _mi(self, mat_name):
        if mat_name not in self.mats:
            self.mats.append(mat_name)
        return self.mats.index(mat_name)

    def _tag(self, verts, mat_name):
        idx = self._mi(mat_name)
        for f in {f for v in verts for f in v.link_faces}:
            f.material_index = idx

    @staticmethod
    def _matrix(center, size, rot):
        rot = Euler(tuple(math.radians(a) for a in rot)).to_matrix().to_4x4()
        return Matrix.Translation(Vector(center)) @ rot @ Matrix.Diagonal((*size, 1.0))

    def box(self, center, size, mat, rot=(0, 0, 0)):
        res = bmesh.ops.create_cube(self.bm, size=1.0, matrix=self._matrix(center, size, rot))
        self._tag(res["verts"], mat)

    def box_mm(self, lo, hi, mat):
        """Axis-aligned box from min/max corners."""
        c = [(a + b) / 2 for a, b in zip(lo, hi)]
        s = [abs(b - a) for a, b in zip(lo, hi)]
        self.box(c, s, mat)

    def cyl(self, center, radius, height, mat, segs=10, rot=(0, 0, 0), radius2=None):
        m = self._matrix(center, (1, 1, 1), rot)
        res = bmesh.ops.create_cone(
            self.bm, cap_ends=True, cap_tris=False, segments=segs,
            radius1=radius, radius2=radius if radius2 is None else radius2, depth=height, matrix=m)
        self._tag(res["verts"], mat)

    def blob(self, center, radius, mat, squash=(1, 1, 1)):
        m = Matrix.Translation(Vector(center)) @ Matrix.Diagonal((*squash, 1.0))
        res = bmesh.ops.create_icosphere(self.bm, subdivisions=1, radius=radius, matrix=m)
        self._tag(res["verts"], mat)

    def build(self, parent=None):
        mesh = bpy.data.meshes.new(self.name)
        self.bm.normal_update()
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.data.collections[COLLECTION].objects.link(obj)
        if parent:
            obj.parent = parent
        return obj


# ---------------------------------------------------------------- architecture

def wall_with_holes(mb, axis, fixed, u0, u1, height, thick, holes, mat="WallWhite"):
    """A straight wall along `axis` ('x' or 'y') with rectangular openings.
    holes: list of (u_center, bottom, width, height)."""

    def piece(ua, ub, za, zb):
        if ub - ua < 1e-3 or zb - za < 1e-3:
            return
        if axis == "x":
            mb.box_mm((ua, fixed - thick / 2, za), (ub, fixed + thick / 2, zb), mat)
        else:
            mb.box_mm((fixed - thick / 2, ua, za), (fixed + thick / 2, ub, zb), mat)

    cursor = u0
    for uc, bottom, w, h in sorted(holes):
        left, right = uc - w / 2, uc + w / 2
        piece(cursor, left, 0, height)
        piece(left, right, 0, bottom)
        piece(left, right, bottom + h, height)
        cursor = right
    piece(cursor, u1, 0, height)


def steel_window(mb, axis, fixed, uc, bottom, w, h, cols, rows):
    """Belgian-profile steel window: slim black frame, grid of mullions, glass."""
    frame, mull, depth = 0.07, 0.035, 0.14

    def bar(u_lo, u_hi, z_lo, z_hi, d=depth):
        if axis == "x":
            mb.box_mm((u_lo, fixed - d / 2, z_lo), (u_hi, fixed + d / 2, z_hi), "Steel")
        else:
            mb.box_mm((fixed - d / 2, u_lo, z_lo), (fixed + d / 2, u_hi, z_hi), "Steel")

    l, r, b, t = uc - w / 2, uc + w / 2, bottom, bottom + h
    bar(l, r, b, b + frame)            # sill
    bar(l, r, t - frame, t)            # head
    bar(l, l + frame, b, t)            # jambs
    bar(r - frame, r, b, t)
    for i in range(1, cols):
        u = l + w * i / cols
        bar(u - mull / 2, u + mull / 2, b, t, depth * 0.7)
    for j in range(1, rows):
        z = b + h * j / rows
        bar(l, r, z - mull / 2, z + mull / 2, depth * 0.7)
    # glass
    if axis == "x":
        mb.box_mm((l, fixed - 0.01, b), (r, fixed + 0.01, t), "Glass")
    else:
        mb.box_mm((fixed - 0.01, l, b), (fixed + 0.01, r, t), "Glass")


def build_shell(root):
    mb = MeshBuilder("Floor")
    mb.box_mm((-9.0, -7.0, -0.4), (9.0, 7.0, 0.0), "Concrete")
    mb.build(root)

    # Apron: the loft floor continuing past the cutaway on the open sides. Not walkable (no collider in Unity);
    # it gives the camera room to follow the player near the open edges without showing the void.
    apron = MeshBuilder("FloorApron")
    apron.box_mm((-9.0, -10.5, -0.4), (12.5, -7.0, -0.01), "ConcreteEdge")
    apron.box_mm((9.0, -7.0, -0.4), (12.5, 7.0, -0.01), "ConcreteEdge")
    apron.build(root)

    back_holes = [(-4.6, 0.9, 3.4, 3.8), (-0.3, 0.9, 3.4, 3.8), (4.0, 0.9, 3.4, 3.8)]
    left_holes = [(3.3, 0.9, 3.0, 3.8), (-3.3, 3.9, 3.4, 1.6)]

    walls = MeshBuilder("Walls")
    wall_with_holes(walls, "x", 7.15, -9.3, 9.0, 6.0, 0.3, back_holes)
    wall_with_holes(walls, "y", -9.15, -7.0, 7.3, 6.0, 0.3, left_holes)
    # radiators under the back windows
    for uc, _, w, _ in back_holes:
        walls.box((uc, 6.9, 0.45), (1.6, 0.12, 0.55), "CabinetWhite")
    walls.build(root)

    win = MeshBuilder("Windows")
    for uc, b, w, h in back_holes:
        steel_window(win, "x", 7.15, uc, b, w, h, cols=4, rows=5)
    steel_window(win, "y", -9.15, left_holes[0][0], left_holes[0][1], left_holes[0][2], left_holes[0][3], cols=4, rows=5)
    steel_window(win, "y", -9.15, left_holes[1][0], left_holes[1][1], left_holes[1][2], left_holes[1][3], cols=4, rows=2)
    win.build(root)


def build_mezzanine(root):
    """Steel-and-wood mezzanine along the left wall, stair along the open front edge."""
    deck_top = 3.2
    mb = MeshBuilder("Mezzanine")
    mb.box_mm((-9.0, -7.0, deck_top - 0.12), (-5.8, 0.4, deck_top), "Wood")
    mb.box_mm((-9.0, -7.0, deck_top - 0.4), (-5.8, 0.4, deck_top - 0.12), "Steel")  # deck structure
    for y in (-6.85, -3.3, 0.25):
        mb.box((-5.9, y, (deck_top - 0.4) / 2), (0.16, 0.16, deck_top - 0.4), "Steel")

    # railing: front edge (leaves a gap where the stair lands) and the open end
    rail_h = 1.05
    for y in [-5.7 + i * 1.22 for i in range(6)]:
        mb.box((-5.86, y, deck_top + rail_h / 2), (0.05, 0.05, rail_h), "Steel")
    mb.box_mm((-5.89, -5.7, deck_top + rail_h - 0.05), (-5.83, 0.4, deck_top + rail_h), "Steel")
    mb.box_mm((-5.88, -5.7, deck_top + 0.5), (-5.84, 0.4, deck_top + 0.53), "Steel")
    for x in (-8.9, -7.4):
        mb.box((x, 0.37, deck_top + rail_h / 2), (0.05, 0.05, rail_h), "Steel")
    mb.box_mm((-9.0, 0.34, deck_top + rail_h - 0.05), (-5.83, 0.4, deck_top + rail_h), "Steel")

    # stair: 16 risers from the floor at x=-1.8 up to the deck at x=-5.8
    steps, run = 16, 4.0 / 16
    rise = deck_top / steps
    y0, y1 = -6.98, -5.8
    for i in range(steps):
        x = -1.8 - (i + 0.5) * run
        mb.box((x, (y0 + y1) / 2, (i + 1) * rise - 0.02), (run + 0.03, y1 - y0 - 0.1, 0.04), "Wood")
    ang = math.degrees(math.atan2(deck_top, 4.0))
    length = math.hypot(4.0, deck_top)
    for y in (y0, y1):
        mb.box((-3.8, y, deck_top / 2 - 0.05), (length, 0.05, 0.22), "Steel", rot=(0, ang, 0))
        mb.box((-3.8, y, deck_top / 2 + 0.95), (length, 0.04, 0.04), "Steel", rot=(0, ang, 0))
        for k in range(4):
            t = (k + 0.5) / 4
            mb.box((-1.8 - 4.0 * t, y, deck_top * t + 0.47), (0.04, 0.04, 0.95), "Steel")

    # a few archive boxes and a plant up top (the career shelf station also lives up here)
    for i, y in enumerate((-6.4, -0.2)):
        mb.box((-8.6, y, deck_top + 0.2), (0.5, 0.6, 0.4), "Cork" if i else "CabinetWhite")
        mb.box((-8.6, y + 0.05, deck_top + 0.52), (0.44, 0.5, 0.24), "Paper")
    pot(mb, (-6.4, -0.1, deck_top), 0.9)
    mb.build(root)


# ---------------------------------------------------------------- furniture helpers

def stool(mb, x, y, h=0.65):
    mb.cyl((x, y, h - 0.03), 0.18, 0.06, "Wood", segs=12)
    for k in range(3):
        a = k * 2 * math.pi / 3
        mb.box((x + math.cos(a) * 0.11, y + math.sin(a) * 0.11, (h - 0.06) / 2), (0.035, 0.035, h - 0.06), "Steel",
               rot=(math.degrees(-math.sin(a) * 0.12), math.degrees(math.cos(a) * 0.12), 0))


def chair(mb, x, y, facing=0.0):
    mb.box((x, y, 0.47), (0.48, 0.46, 0.07), "Screen")
    bx = x - math.sin(math.radians(facing)) * 0.22
    by = y + math.cos(math.radians(facing)) * 0.22
    mb.box((bx, by, 0.8), (0.44, 0.06, 0.5), "Screen", rot=(0, 0, facing))
    mb.cyl((x, y, 0.25), 0.03, 0.42, "Steel", segs=6)
    for k in range(5):
        a = k * 72
        mb.box((x + math.cos(math.radians(a)) * 0.15, y + math.sin(math.radians(a)) * 0.15, 0.05),
               (0.3, 0.04, 0.04), "Steel", rot=(0, 0, a))


def pot(mb, loc, scale=1.0):
    x, y, z = loc
    mb.cyl((x, y, z + 0.2 * scale), 0.2 * scale, 0.4 * scale, "Terracotta", segs=10, radius2=0.25 * scale)
    mb.blob((x, y, z + 0.75 * scale), 0.38 * scale, "Plant", squash=(1, 1, 1.3))
    mb.blob((x + 0.15 * scale, y - 0.1 * scale, z + 1.1 * scale), 0.25 * scale, "Plant")


def pendant(mb, x, y, z=2.7, top=6.0):
    mb.cyl((x, y, (z + top) / 2), 0.008, top - z, "Steel", segs=4)
    mb.cyl((x, y, z - 0.15), 0.25, 0.3, "Steel", segs=12, radius2=0.07)  # radius1 is the bottom rim
    mb.blob((x, y, z - 0.3), 0.07, "Bulb")


def monitor(mb, x, y, z):
    mb.box((x, y, z + 0.42), (0.64, 0.04, 0.4), "Steel")
    mb.box((x, y - 0.022, z + 0.42), (0.6, 0.01, 0.36), "Screen")
    mb.box((x, y + 0.08, z + 0.15), (0.05, 0.05, 0.3), "Steel")
    mb.box((x, y + 0.05, z + 0.01), (0.25, 0.18, 0.02), "Steel")


# ---------------------------------------------------------------- stations

def station(root, name, loc, rot_z, approach=1.4):
    st = empty("ST_" + name, loc, rot_z, root, size=0.8)
    empty("ST_" + name + "_Approach", (0, -approach, 0), 0, st, size=0.3)
    return st


def build_about(root):
    """Drafting table: sketches, ideas — who I am and how I think."""
    st = station(root, "About", (-5.0, 4.9), 0)
    mb = MeshBuilder("DraftingTable")
    for x in (-0.6, 0.6):  # A-frame steel legs
        mb.box((x, -0.25, 0.45), (0.05, 0.05, 0.95), "Steel", rot=(-12, 0, 0))
        mb.box((x, 0.25, 0.45), (0.05, 0.05, 0.95), "Steel", rot=(12, 0, 0))
        mb.box((x, 0, 0.12), (0.05, 0.6, 0.04), "Steel")
    mb.box((0, 0, 0.85), (1.3, 0.05, 0.04), "Steel")
    mb.box((0, 0.05, 0.98), (1.5, 1.0, 0.04), "Plywood", rot=(18, 0, 0))
    mb.box((-0.15, 0.0, 1.0), (0.84, 0.6, 0.01), "Paper", rot=(18, 0, 0))
    mb.box((0.45, -0.12, 0.96), (0.3, 0.42, 0.01), "Paper", rot=(18, 0, 18))
    mb.box((0, -0.45, 0.86), (1.4, 0.04, 0.04), "WoodDark", rot=(18, 0, 0))   # pencil ledge
    mb.box((0.62, 0.15, 1.35), (0.03, 0.03, 0.7), "Steel", rot=(0, -25, 0))   # lamp arm
    mb.cyl((0.45, 0.15, 1.62), 0.05, 0.14, "Accent", segs=10, radius2=0.1)
    # side cabinet with paper rolls
    mb.box_mm((0.95, -0.35, 0), (1.55, 0.35, 0.75), "Plywood")
    for i in range(5):
        mb.cyl((1.1 + (i % 3) * 0.15, -0.1 + (i // 3) * 0.2, 0.95), 0.05, 0.4, "Paper", segs=8, rot=(0, 0, 0))
    stool(mb, 0.0, -0.75, 0.7)
    mb.build(st)
    return st


def build_projects(root):
    """Central workbench (like the island in the reference photo): prototypes and process."""
    st = station(root, "Projects", (1.0, -0.6), 0, approach=1.5)
    mb = MeshBuilder("Workbench")
    w, d, h = 3.4, 1.6, 0.95
    mb.box_mm((-w / 2, -d / 2, h - 0.06), (w / 2, d / 2, h), "Wood")
    # cabinet base: plywood boxes with open shelves facing the front
    mb.box_mm((-w / 2 + 0.05, -d / 2 + 0.05, 0.05), (w / 2 - 0.05, d / 2 - 0.05, h - 0.06), "Plywood")
    for x in (-1.1, 0.0, 1.1):
        mb.box_mm((x - 0.5, -d / 2 + 0.03, 0.12), (x + 0.5, -d / 2 + 0.06, h - 0.14), "WoodDark")
        mb.box((x - 0.2, -d / 2 + 0.2, 0.3), (0.35, 0.3, 0.25), "CabinetWhite")
        mb.box((x + 0.2, -d / 2 + 0.2, 0.6), (0.3, 0.3, 0.2), "Paper")
    # on top: sketches, models, organizer, 3D printer
    mb.box((-0.9, -0.2, h + 0.005), (0.6, 0.42, 0.01), "Paper", rot=(0, 0, 8))
    mb.box((-0.3, 0.25, h + 0.005), (0.42, 0.3, 0.01), "Paper", rot=(0, 0, -12))
    mb.box((0.25, -0.25, h + 0.1), (0.3, 0.2, 0.2), "Accent")
    mb.cyl((0.55, 0.1, h + 0.12), 0.1, 0.24, "Blue", segs=10)
    mb.blob((-0.45, -0.3, h + 0.08), 0.09, "CabinetWhite", squash=(1.4, 1, 0.8))
    for i in range(4):  # blue organizer (like the photo)
        mb.box((0.95, 0.4 - i * 0.07, h + 0.1 + (i % 2) * 0.03), (0.25, 0.05, 0.2 + (i % 2) * 0.06), "Blue")
    # 3D printer
    px, py = 1.35, -0.25
    mb.box_mm((px - 0.25, py - 0.25, h), (px + 0.25, py + 0.25, h + 0.05), "Steel")
    for dx in (-0.23, 0.23):
        for dy in (-0.23, 0.23):
            mb.box((px + dx, py + dy, h + 0.3), (0.03, 0.03, 0.5), "Steel")
    mb.box_mm((px - 0.25, py - 0.25, h + 0.52), (px + 0.25, py + 0.25, h + 0.56), "Steel")
    mb.box((px, py, h + 0.1), (0.14, 0.14, 0.1), "Accent")
    for sx in (-1.3, -0.3, 0.7):  # stools on the back side keep the front approach clear
        stool(mb, sx, 1.15)
    mb.build(st)
    return st


def build_games(root):
    """Computer desk by the window: playable builds."""
    st = station(root, "Games", (3.4, 5.85), 0)
    mb = MeshBuilder("ComputerDesk")
    w, d, h = 3.0, 0.9, 0.75
    mb.box_mm((-w / 2, -d / 2, h - 0.05), (w / 2, d / 2, h), "Wood")
    for x in (-w / 2 + 0.06, w / 2 - 0.06):
        mb.box_mm((x - 0.03, -d / 2 + 0.05, 0), (x + 0.03, d / 2 - 0.05, h - 0.05), "Steel")
    monitor(mb, -0.35, 0.15, h)
    monitor(mb, 0.35, 0.15, h)
    mb.box((0, -0.15, h + 0.015), (0.45, 0.15, 0.02), "CabinetWhite")    # keyboard
    mb.box((0.35, -0.15, h + 0.01), (0.07, 0.11, 0.02), "CabinetWhite")  # mouse
    mb.box_mm((1.0, -0.3, 0), (1.25, 0.3, 0.48), "Steel")                # PC tower
    mb.box((-1.05, 0, h + 0.12), (0.28, 0.2, 0.22), "Accent")             # game controller box
    mb.box((-1.1, 0.1, h + 0.28), (0.18, 0.12, 0.1), "Blue")
    chair(mb, 0.0, -0.75, facing=0)
    pot(mb, (1.9, 0.1, 0), 1.1)
    mb.build(st)
    return st


def build_career(root):
    """Tall shelf of past prototypes up on the mezzanine: climb the stair to see the path so far."""
    st = station(root, "Career", (-8.6, -3.3, 3.2), 90, approach=1.4)  # front (local -Y) faces +X
    mb = MeshBuilder("PrototypeShelf")
    w, d, h = 3.2, 0.5, 2.5
    for x in (-w / 2, -w / 6, w / 6, w / 2):
        mb.box_mm((x - 0.03, -d / 2, 0), (x + 0.03, d / 2, h), "Wood")
    levels = [0.05, 0.55, 1.05, 1.55, 2.05, h - 0.04]
    for z in levels:
        mb.box_mm((-w / 2, -d / 2, z), (w / 2, d / 2, z + 0.04), "Wood")
    mb.box_mm((-w / 2, d / 2 - 0.02, 0), (w / 2, d / 2, h), "WoodDark")
    colors = ["Accent", "Blue", "CabinetWhite", "Cork", "Paper", "Plant"]
    k = 0
    for li, z in enumerate(levels[:-1]):
        for bay in range(3):
            cx = -w / 3 + bay * w / 3
            kind = (li + bay) % 3
            c = colors[k % len(colors)]
            k += 1
            if kind == 0:
                mb.box((cx - 0.15, 0, z + 0.16), (0.3, 0.25, 0.24), c)
                mb.cyl((cx + 0.25, 0, z + 0.14), 0.08, 0.2, colors[(k + 2) % len(colors)], segs=10)
            elif kind == 1:
                mb.blob((cx, 0, z + 0.15), 0.12, c, squash=(1.2, 1, 0.9))
                mb.box((cx + 0.3, 0.05, z + 0.2), (0.05, 0.3, 0.32), "Paper", rot=(0, -10, 0))
            else:
                for i in range(4):
                    mb.box((cx - 0.25 + i * 0.06, 0, z + 0.17), (0.04, 0.3, 0.28 + (i % 2) * 0.04), colors[(k + i) % len(colors)])
    mb.build(st)
    return st


def build_contact(root):
    """Cork board on the wall + a small counter: where to find me."""
    st = station(root, "Contact", (7.35, 6.3), 0, approach=1.4)
    mb = MeshBuilder("PinBoard")
    mb.box_mm((-1.1, 0.62, 1.05), (1.1, 0.7, 2.55), "WoodDark")
    mb.box_mm((-1.02, 0.6, 1.12), (1.02, 0.63, 2.48), "Cork")
    notes = [(-0.7, 2.1, "Paper"), (-0.25, 2.2, "Accent"), (0.3, 2.05, "Paper"), (0.75, 2.2, "Blue"),
             (-0.6, 1.55, "Blue"), (0.0, 1.6, "Paper"), (0.6, 1.5, "Accent")]
    for i, (x, z, c) in enumerate(notes):
        mb.box((x, 0.59, z), (0.34, 0.01, 0.26), c, rot=(0, (i % 3 - 1) * 6, 0))
    mb.box_mm((-0.8, -0.1, 0), (0.8, 0.35, 0.9), "Plywood")   # counter
    mb.box_mm((-0.85, -0.12, 0.9), (0.85, 0.37, 0.95), "Wood")
    mb.box((0.2, 0.1, 0.99), (0.22, 0.14, 0.06), "CabinetWhite")   # business cards
    mb.box((-0.35, 0.05, 0.96), (0.3, 0.22, 0.01), "Paper")
    pot(mb, (1.3, 0.15, 0), 1.2)
    mb.build(st)
    return st


def build_props(root):
    mb = MeshBuilder("Props")
    # white flat-file cabinet on the right (from the reference photo)
    mb.box_mm((7.4, -0.2, 0), (8.8, 2.4, 0.95), "CabinetWhite")
    for z in (0.25, 0.5, 0.75):
        mb.box_mm((7.37, -0.1, z - 0.01), (7.4, 2.3, z + 0.01), "Steel")
    mb.box((8.0, 0.4, 1.05), (0.4, 0.4, 0.2), "Accent")
    mb.blob((8.2, 1.4, 1.1), 0.15, "Blue")
    # sawhorse table
    for x in (4.9, 6.6):
        for dy in (-0.35, 0.35):
            mb.box((x, -3.8 + dy, 0.38), (0.05, 0.05, 0.8), "WoodDark", rot=(dy * -30, 0, 0))
        mb.box((x, -3.8, 0.76), (0.08, 0.9, 0.06), "WoodDark")
    mb.box_mm((4.6, -4.35, 0.79), (6.9, -3.25, 0.83), "Plywood")
    mb.box((5.3, -3.8, 0.9), (0.5, 0.35, 0.14), "CabinetWhite")
    mb.box((6.2, -3.7, 0.88), (0.6, 0.4, 0.01), "Paper")
    # maker corner under the mezzanine: pegboard tool wall + low workbench
    mb.box_mm((-8.99, -6.6, 0.95), (-8.95, -1.2, 2.45), "Plywood")
    tools = ["Steel", "Accent", "Steel", "Blue", "WoodDark", "Steel", "Accent", "Steel"]
    for i, c in enumerate(tools):
        y = -6.2 + i * 0.65
        mb.box((-8.9, y, 1.9 - (i % 2) * 0.35), (0.06, 0.12 + (i % 3) * 0.05, 0.35 + (i % 2) * 0.15), c)
    mb.box_mm((-8.95, -6.6, 0.85), (-8.2, -1.2, 0.9), "Wood")
    for y in (-6.5, -3.9, -1.3):
        mb.box_mm((-8.9, y - 0.03, 0), (-8.25, y + 0.03, 0.85), "Steel")
    mb.box((-8.55, -5.5, 0.97), (0.3, 0.2, 0.14), "Steel")      # bench vise
    mb.box((-8.5, -2.4, 0.95), (0.4, 0.5, 0.1), "Plywood")
    # studio desks: two facing pairs with laptops (like the reference photo)
    cx, cy = 4.4, 1.6
    for sx in (-0.85, 0.85):
        mb.box_mm((cx + sx - 0.8, cy - 0.8, 0.72), (cx + sx + 0.8, cy + 0.8, 0.76), "Wood")
        for lx in (sx - 0.75, sx + 0.75):
            mb.box_mm((cx + lx - 0.03, cy - 0.75, 0), (cx + lx + 0.03, cy + 0.75, 0.72), "Steel")
        for sy in (-0.4, 0.4):
            face = 180 if sy > 0 else 0
            mb.box((cx + sx, cy + sy * 0.5, 0.77), (0.34, 0.24, 0.02), "Steel", rot=(0, 0, face))
            mb.box((cx + sx, cy + sy * 0.5 + (0.12 if sy > 0 else -0.12) * -1, 0.9), (0.34, 0.02, 0.24), "Screen")
            chair(mb, cx + sx, cy + sy * 2.3, facing=face)
    mb.box_mm((cx - 0.05, cy - 0.8, 0.76), (cx + 0.05, cy + 0.8, 0.92), "Plywood")  # divider
    pot(mb, (cx + 2.0, cy + 1.2, 0), 0.9)
    # pendant lamps
    pendant(mb, cx - 0.85, cy, z=2.5)
    pendant(mb, cx + 0.85, cy, z=2.5)
    for x in (0.0, 2.0):
        pendant(mb, x, -0.6)
    pendant(mb, -5.0, 4.5, z=2.4)
    pendant(mb, 3.4, 5.6, z=2.4)
    # plants
    pot(mb, (8.3, -5.8, 0), 1.3)
    pot(mb, (-2.4, 6.4, 0), 1.0)
    mb.build(root)


def build():
    reset_collection()
    root = empty("Workshop", (0, 0, 0), 0, None, size=1.0)
    build_shell(root)
    build_mezzanine(root)
    build_about(root)
    build_projects(root)
    build_games(root)
    build_career(root)
    build_contact(root)
    build_props(root)
    return root


def export_fbx(path):
    col = bpy.data.collections[COLLECTION]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.all_objects:
        obj.select_set(True)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,  # "Apply Transform" corrupts empties; Unity bakes the axis conversion instead
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )
    bpy.ops.object.select_all(action="DESELECT")


if __name__ == "__main__":
    build()

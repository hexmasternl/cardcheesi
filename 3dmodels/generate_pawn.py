#!/usr/bin/env python3
"""
Generate a pawn 3D model in GLB (glTF 2.0 binary) format.

Pawn dimensions:
  - Diameter: 2.2 cm  (0.022 m)
  - Height:   4.0 cm  (0.040 m)

The shape is produced by revolving a chess-pawn silhouette profile around
the Y axis (Y = up).  Geometry units are metres, matching Babylon.js / glTF
conventions.
"""

import json
import math
import struct

# ---------------------------------------------------------------------------
# Geometry helpers
# ---------------------------------------------------------------------------

def build_pawn_profile(r: float, h: float) -> list[tuple[float, float]]:
    """
    Return the 2-D silhouette profile of the pawn as (radius, y) pairs.

    r  – outer radius at widest point (m)
    h  – total height (m)
    """
    return [
        # bottom centre
        (0.000 * r,  0.000 * h),
        # base rim
        (1.000 * r,  0.000 * h),
        (0.960 * r,  0.060 * h),
        (0.880 * r,  0.150 * h),
        # body taper
        (0.760 * r,  0.300 * h),
        (0.600 * r,  0.420 * h),
        # neck
        (0.440 * r,  0.480 * h),
        (0.400 * r,  0.530 * h),
        (0.430 * r,  0.580 * h),
        # head bulge
        (0.620 * r,  0.660 * h),
        (0.740 * r,  0.750 * h),
        (0.760 * r,  0.830 * h),
        (0.720 * r,  0.900 * h),
        (0.560 * r,  0.960 * h),
        # top centre
        (0.000 * r,  1.000 * h),
    ]


def _profile_normals(profile: list[tuple[float, float]]) -> list[tuple[float, float]]:
    """
    Compute 2-D outward normals for each profile vertex.
    For interior points the normal is the average of the two edge normals.
    For end-cap points (radius == 0) the normal points straight up or down.
    """
    n = len(profile)
    normals = []
    for i in range(n):
        r_i, y_i = profile[i]

        if r_i == 0.0:
            # axis point: cap normal points along Y (down for bottom, up for top)
            ny = -1.0 if i == 0 else 1.0
            normals.append((0.0, ny))
            continue

        # edge vectors from previous and next profile points
        vecs = []
        if i > 0:
            dr = r_i - profile[i - 1][0]
            dy = y_i - profile[i - 1][1]
            L = math.hypot(dr, dy)
            if L > 0:
                vecs.append(( dy / L, -dr / L))   # rotate 90° outward
        if i < n - 1:
            dr = profile[i + 1][0] - r_i
            dy = profile[i + 1][1] - y_i
            L = math.hypot(dr, dy)
            if L > 0:
                vecs.append(( dy / L, -dr / L))

        if vecs:
            nr = sum(v[0] for v in vecs) / len(vecs)
            ny = sum(v[1] for v in vecs) / len(vecs)
            L = math.hypot(nr, ny)
            normals.append((nr / L if L > 0 else 0.0, ny / L if L > 0 else 0.0))
        else:
            normals.append((1.0, 0.0))

    return normals


def revolve_profile(
    profile: list[tuple[float, float]],
    segments: int = 48,
) -> tuple[list, list, list]:
    """
    Revolve the profile around the Y axis.

    Returns (positions, normals, indices) where every element is a flat list
    of floats / ints suitable for packing into a glTF binary buffer.
    """
    profile_normals = _profile_normals(profile)
    n_profile = len(profile)

    positions = []
    normals   = []
    indices   = []

    def add_vertex(p_idx: int, seg_idx: int):
        r, y = profile[p_idx]
        nr, ny = profile_normals[p_idx]
        angle = 2.0 * math.pi * seg_idx / segments
        cos_a = math.cos(angle)
        sin_a = math.sin(angle)
        positions.extend([r * cos_a, y, r * sin_a])
        normals.extend(  [nr * cos_a, ny, nr * sin_a])

    # Build a vertex grid: profile_index × segment_index
    # Bottom axis point (profile[0], r==0): one ring of coincident vertices so
    # normals vary, but we can share with a fan.
    # For simplicity, emit all combinations and deduplicate via a grid.

    grid: list[list[int]] = []     # grid[p][s] = vertex index

    for p in range(n_profile):
        row = []
        for s in range(segments):
            idx = len(positions) // 3
            add_vertex(p, s)
            row.append(idx)
        grid.append(row)

    # Stitch quads between adjacent profile rings
    for p in range(n_profile - 1):
        r_cur  = profile[p][0]
        r_next = profile[p + 1][0]

        for s in range(segments):
            s_next = (s + 1) % segments

            v00 = grid[p    ][s     ]
            v01 = grid[p    ][s_next]
            v10 = grid[p + 1][s     ]
            v11 = grid[p + 1][s_next]

            if r_cur == 0.0:
                # bottom cap: triangle fan
                indices.extend([v00, v10, v11])
            elif r_next == 0.0:
                # top cap: triangle fan
                indices.extend([v00, v10, v01])
            else:
                # regular quad → 2 triangles
                indices.extend([v00, v10, v01])
                indices.extend([v01, v10, v11])

    return positions, normals, indices


# ---------------------------------------------------------------------------
# GLB writer
# ---------------------------------------------------------------------------

def _pack_float32(values: list[float]) -> bytes:
    return struct.pack(f"<{len(values)}f", *values)


def _pack_uint32(values: list[int]) -> bytes:
    return struct.pack(f"<{len(values)}I", *values)


def _pad4(data: bytes) -> bytes:
    rem = len(data) % 4
    return data + b"\x00" * ((4 - rem) % 4)


def build_glb(positions: list[float], normals: list[float], indices: list[int]) -> bytes:
    """Pack geometry into a glTF 2.0 GLB binary blob."""

    pos_bytes  = _pack_float32(positions)
    norm_bytes = _pack_float32(normals)
    idx_bytes  = _pack_uint32(indices)   # uint32 indices

    bin_data = pos_bytes + norm_bytes + idx_bytes
    bin_data = _pad4(bin_data)

    n_verts = len(positions) // 3
    n_tris  = len(indices) // 3

    # Bounding box for POSITION accessor
    xs = positions[0::3]
    ys = positions[1::3]
    zs = positions[2::3]
    pos_min = [min(xs), min(ys), min(zs)]
    pos_max = [max(xs), max(ys), max(zs)]

    bv_pos_offset  = 0
    bv_pos_len     = len(pos_bytes)
    bv_norm_offset = bv_pos_len
    bv_norm_len    = len(norm_bytes)
    bv_idx_offset  = bv_norm_offset + bv_norm_len
    bv_idx_len     = len(idx_bytes)

    gltf = {
        "asset": {"generator": "CardCheesi pawn generator", "version": "2.0"},
        "scene": 0,
        "scenes": [{"nodes": [0], "name": "Scene"}],
        "nodes": [{"mesh": 0, "name": "Pawn"}],
        "meshes": [{
            "name": "Pawn",
            "primitives": [{
                "attributes": {"POSITION": 0, "NORMAL": 1},
                "indices": 2,
                "mode": 4,   # TRIANGLES
            }]
        }],
        "accessors": [
            {   # 0: POSITION
                "bufferView": 0,
                "componentType": 5126,  # FLOAT
                "count": n_verts,
                "type": "VEC3",
                "min": pos_min,
                "max": pos_max,
            },
            {   # 1: NORMAL
                "bufferView": 1,
                "componentType": 5126,
                "count": n_verts,
                "type": "VEC3",
            },
            {   # 2: indices
                "bufferView": 2,
                "componentType": 5125,  # UNSIGNED_INT
                "count": len(indices),
                "type": "SCALAR",
            },
        ],
        "bufferViews": [
            {"buffer": 0, "byteOffset": bv_pos_offset,  "byteLength": bv_pos_len,  "target": 34962},
            {"buffer": 0, "byteOffset": bv_norm_offset, "byteLength": bv_norm_len, "target": 34962},
            {"buffer": 0, "byteOffset": bv_idx_offset,  "byteLength": bv_idx_len,  "target": 34963},
        ],
        "buffers": [{"byteLength": len(bin_data)}],
    }

    json_bytes = _pad4(json.dumps(gltf, separators=(",", ":")).encode("utf-8"))

    # GLB chunks
    json_chunk = struct.pack("<II", len(json_bytes), 0x4E4F534A) + json_bytes
    bin_chunk  = struct.pack("<II", len(bin_data),   0x004E4942) + bin_data

    total_len = 12 + len(json_chunk) + len(bin_chunk)
    header = struct.pack("<III", 0x46546C67, 2, total_len)   # magic, version, length

    return header + json_chunk + bin_chunk


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    import os

    DIAMETER_M = 0.022   # 2.2 cm
    HEIGHT_M   = 0.040   # 4.0 cm

    profile  = build_pawn_profile(r=DIAMETER_M / 2, h=HEIGHT_M)
    pos, nrm, idx = revolve_profile(profile, segments=48)
    glb_bytes = build_glb(pos, nrm, idx)

    out_dir  = os.path.dirname(os.path.abspath(__file__))
    out_path = os.path.join(out_dir, "pawn.glb")
    with open(out_path, "wb") as f:
        f.write(glb_bytes)

    n_verts = len(pos) // 3
    n_tris  = len(idx) // 3
    print(f"Written: {out_path}")
    print(f"  Vertices : {n_verts}")
    print(f"  Triangles: {n_tris}")
    print(f"  File size: {len(glb_bytes)} bytes")

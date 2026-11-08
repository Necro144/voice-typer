"""Draws the Voice Typer icons (a microphone on a rounded tile) and writes them as .ico files.

    python tools/make_icons.py

Writes icons/app.ico (purple, used for the .exe) and icons/tray-off.ico, icons/tray-on.ico.
Plain Python, no extra packages.
"""
import os
import struct
import zlib

PURPLE = (91, 75, 219)
RED = (214, 48, 49)
WHITE = (255, 255, 255)


def inside_round(x, y, x0, y0, x1, y1, r):
    cx = min(max(x, x0 + r), x1 - r)
    cy = min(max(y, y0 + r), y1 - r)
    return (x - cx) ** 2 + (y - cy) ** 2 <= r * r


def pixel(x, y, colour):
    """Colour of one point on a 0..1 canvas, as (r, g, b, a)."""
    if not inside_round(x, y, 0.02, 0.02, 0.98, 0.98, 0.22):
        return (0, 0, 0, 0)
    capsule = inside_round(x, y, 0.37, 0.15, 0.63, 0.57, 0.13)
    d = ((x - 0.5) ** 2 + (y - 0.47) ** 2) ** 0.5
    holder = 0.22 <= d <= 0.29 and y >= 0.47
    stem = 0.47 <= x <= 0.53 and 0.74 <= y <= 0.81
    base = inside_round(x, y, 0.33, 0.79, 0.67, 0.86, 0.035)
    if capsule or holder or stem or base:
        return WHITE + (255,)
    return colour + (255,)


def render(size, colour, ss=4):
    """Returns rows of (r, g, b, a), top to bottom, smoothed by supersampling."""
    rows = []
    for py in range(size):
        row = []
        for px in range(size):
            acc = [0, 0, 0, 0]
            for sy in range(ss):
                for sx in range(ss):
                    r, g, b, a = pixel((px * ss + sx + 0.5) / (size * ss), (py * ss + sy + 0.5) / (size * ss), colour)
                    acc[0] += r * a
                    acc[1] += g * a
                    acc[2] += b * a
                    acc[3] += a
            n = ss * ss
            a = acc[3] // n
            if acc[3]:
                row.append((acc[0] // acc[3], acc[1] // acc[3], acc[2] // acc[3], a))
            else:
                row.append((0, 0, 0, 0))
        rows.append(row)
    return rows


def png_bytes(size, rows):
    raw = b''.join(b'\x00' + bytes(v for px in row for v in px) for row in rows)

    def chunk(tag, data):
        body = tag + data
        return struct.pack('>I', len(data)) + body + struct.pack('>I', zlib.crc32(body) & 0xFFFFFFFF)

    return (b'\x89PNG\r\n\x1a\n'
            + chunk(b'IHDR', struct.pack('>IIBBBBB', size, size, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(raw, 9))
            + chunk(b'IEND', b''))


def dib_bytes(size, rows):
    """An icon image stored as a classic 32-bit bitmap (the safest format for the tray)."""
    header = struct.pack('<IiiHHIIiiII', 40, size, size * 2, 1, 32, 0, 0, 0, 0, 0, 0)
    pixels = b''.join(bytes(v for (r, g, b, a) in row for v in (b, g, r, a)) for row in reversed(rows))
    mask = b'\x00' * (((size + 31) // 32) * 4 * size)
    return header + pixels + mask


def write_ico(path, images):
    """images: list of (size, data)."""
    head = struct.pack('<HHH', 0, 1, len(images))
    offset = 6 + 16 * len(images)
    entries, blobs = b'', b''
    for size, data in images:
        entries += struct.pack('<BBBBHHII', 0 if size >= 256 else size, 0 if size >= 256 else size, 0, 0, 1, 32, len(data), offset)
        blobs += data
        offset += len(data)
    with open(path, 'wb') as f:
        f.write(head + entries + blobs)


def main():
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'icons')
    os.makedirs(out, exist_ok=True)
    small = (16, 24, 32, 48)
    write_ico(os.path.join(out, 'tray-off.ico'), [(s, dib_bytes(s, render(s, PURPLE))) for s in small])
    write_ico(os.path.join(out, 'tray-on.ico'), [(s, dib_bytes(s, render(s, RED))) for s in small])
    app = [(s, dib_bytes(s, render(s, PURPLE))) for s in small]
    app.append((256, png_bytes(256, render(256, PURPLE, ss=3))))
    write_ico(os.path.join(out, 'app.ico'), app)
    with open(os.path.join(out, 'preview.png'), 'wb') as f:
        f.write(png_bytes(128, render(128, PURPLE)))
    print('wrote icons to', os.path.normpath(out))


if __name__ == '__main__':
    main()

"""Generate procedural geometric face card character art for cyberpunk deck.
Each face card gets a geometric stylized portrait using PIL primitives."""

from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageOps
import os, math, random

random.seed(42)

OUT = "Assets/Sprites/Cards"
SRC = f"{OUT}/_src/faces"
os.makedirs(SRC, exist_ok=True)

# ── Palette ──
CYAN  = (0, 230, 255)
MAGENTA = (255, 20, 147)
GOLD  = (255, 200, 50)
WHITE = (230, 240, 255)
FACE_BG = (8, 6, 22)
DIM    = (60, 50, 100)

FACE_COLORS = {
    "S": (0, 200, 255),  # cyan spade
    "H": (255, 40, 150), # magenta heart
    "D": (255, 60, 160), # magenta diamond
    "C": (0, 200, 220),  # cyan club
}

def draw_star(draw, cx, cy, r, color, points=8, fill=None):
    """Draw a star polygon."""
    coords = []
    for i in range(points * 2):
        angle = math.pi * i / points - math.pi / 2
        radius = r if i % 2 == 0 else r * 0.4
        coords.append((cx + radius * math.cos(angle), cy + radius * math.sin(angle)))
    draw.polygon(coords, fill=fill or color, outline=color)

def draw_crown(draw, cx, y, w, h, color):
    """Draw a geometric crown."""
    pts = [
        (cx - w//2, y + h),
        (cx - w//2 + 15, y + h//3),
        (cx - w//4, y + h),
        (cx, y),
        (cx + w//4, y + h),
        (cx + w//2 - 15, y + h//3),
        (cx + w//2, y + h),
    ]
    draw.polygon(pts, fill=color+(30,), outline=color, width=2)
    # Jewels
    for jx in [cx - w//4, cx, cx + w//4]:
        draw.ellipse([(jx-4, y+4), (jx+4, y+12)], fill=(255, 200, 50, 200))

def draw_geometric_face(draw, cx, cy, size, style="king", color=CYAN):
    """Draw a geometric stylized face."""
    s = size
    glow_alpha = 60

    if style == "king":
        # Bearded king with crown
        draw_crown(draw, cx, cy - int(s*0.7), int(s*0.7), int(s*0.25), color)
        # Face shape (hexagon)
        face_pts = [
            (cx, cy - int(s*0.4)),
            (cx + int(s*0.35), cy - int(s*0.15)),
            (cx + int(s*0.35), cy + int(s*0.15)),
            (cx, cy + int(s*0.35)),
            (cx - int(s*0.35), cy + int(s*0.15)),
            (cx - int(s*0.35), cy - int(s*0.15)),
        ]
        draw.polygon(face_pts, outline=color, width=2, fill=color+(15,))
        # Eyes (two horizontal lines)
        draw.line([(cx - int(s*0.15), cy - int(s*0.1)), (cx - int(s*0.05), cy - int(s*0.1))], fill=WHITE+(200,), width=2)
        draw.line([(cx + int(s*0.05), cy - int(s*0.1)), (cx + int(s*0.15), cy - int(s*0.1))], fill=WHITE+(200,), width=2)
        # Beard (triangle)
        beard = [(cx, cy + int(s*0.2)), (cx - int(s*0.2), cy + int(s*0.55)), (cx + int(s*0.2), cy + int(s*0.55))]
        draw.polygon(beard, outline=color+(100,), fill=color+(20,), width=1)
        # Shoulder lines
        draw.line([(cx - int(s*0.45), cy + int(s*0.35)), (cx - int(s*0.2), cy + int(s*0.2))], fill=color, width=2)
        draw.line([(cx + int(s*0.45), cy + int(s*0.35)), (cx + int(s*0.2), cy + int(s*0.2))], fill=color, width=2)

    elif style == "queen":
        # Crown
        draw_crown(draw, cx, cy - int(s*0.65), int(s*0.55), int(s*0.15), GOLD)
        # Face (oval)
        draw.ellipse([
            (cx - int(s*0.25), cy - int(s*0.4)),
            (cx + int(s*0.25), cy + int(s*0.25))
        ], outline=color, width=2, fill=color+(15,))
        # Eyes
        draw.ellipse([(cx - int(s*0.12), cy - int(s*0.1)), (cx - int(s*0.04), cy - int(s*0.02))], fill=WHITE+(200,))
        draw.ellipse([(cx + int(s*0.04), cy - int(s*0.1)), (cx + int(s*0.12), cy - int(s*0.02))], fill=WHITE+(200,))
        # Long hair (side curves)
        for side in [-1, 1]:
            for i in range(4):
                hy = cy - int(s*0.3) + i * int(s*0.18)
                hx = cx + side * (int(s*0.25) + int(s*0.08) * i)
                draw.rectangle([(hx-2, hy-2), (hx+2, hy+2)], fill=color+(100,))
        # Necklace / collar
        draw.line([(cx - int(s*0.2), cy + int(s*0.3)), (cx, cy + int(s*0.4)), (cx + int(s*0.2), cy + int(s*0.3))],
                  fill=GOLD, width=2)
        # Shoulder line
        draw.line([(cx - int(s*0.45), cy + int(s*0.4)), (cx + int(s*0.45), cy + int(s*0.4))], fill=color, width=2)

    elif style == "jack":
        # Hair with geometric spikes
        for i in range(7):
            angle = math.pi * (0.5 + (i-3) * 0.12)
            hx = cx + int(math.cos(angle) * s * 0.3)
            hy = cy - int(s*0.5) + abs(int(math.sin(angle) * s * 0.15))
            draw.line([(cx, cy - int(s*0.35)), (hx, hy)], fill=color+(80,), width=3)
        # Face (diamond-ish)
        face_pts = [
            (cx, cy - int(s*0.35)),
            (cx + int(s*0.25), cy - int(s*0.05)),
            (cx, cy + int(s*0.3)),
            (cx - int(s*0.25), cy - int(s*0.05)),
        ]
        draw.polygon(face_pts, outline=color, width=2, fill=color+(15,))
        # Eyes (one visible, roguish)
        draw.ellipse([(cx - int(s*0.1), cy - int(s*0.08)), (cx - int(s*0.02), cy)], fill=WHITE+(200,))
        # Roguish smirk
        draw.line([(cx - int(s*0.08), cy + int(s*0.12)), (cx + int(s*0.08), cy + int(s*0.15))], fill=WHITE+(150,), width=2)
        # Collar
        draw.line([(cx - int(s*0.25), cy + int(s*0.3)), (cx + int(s*0.25), cy + int(s*0.3))], fill=color, width=2)

    return draw


def make_face_art(rank, suit, size=(300, 420)):
    """Generate a procedural geometric face card character portrait."""
    w, h = size
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    color = FACE_COLORS[suit]
    cx, cy = w//2, h//2 - 15

    style_map = {"K": "king", "Q": "queen", "J": "jack"}
    style = style_map.get(rank, "jack")

    # Full body glow halo
    glow = Image.new("RGBA", (w+40, h+40), (0,0,0,0))
    gdraw = ImageDraw.Draw(glow)
    gdraw.ellipse([(20, 20), (w+20, h+20)], fill=color+(30,))
    glow = glow.filter(ImageFilter.GaussianBlur(20))
    img.alpha_composite(glow, (-20, -20))

    # Background geometric circle
    draw.ellipse([(cx-120, cy-120), (cx+120, cy+120)], outline=color+(40,), width=1)
    draw.ellipse([(cx-80, cy-80), (cx+80, cy+80)], outline=color+(20,), width=1)

    # Small decorative diamonds
    for i in range(8):
        angle = math.pi * 2 * i / 8
        dx = cx + int(math.cos(angle) * 140)
        dy = cy + int(math.sin(angle) * 140)
        dp = [(dx, dy-6), (dx+4, dy), (dx, dy+6), (dx-4, dy)]
        draw.polygon(dp, fill=color+(50,))

    # Draw the character
    draw_geometric_face(draw, cx, cy, 180, style, color)

    # Suit symbol watermark at bottom
    suit_symbols = {
        "S": [(cx-15, h-35), (cx+15, h-35), (cx, h-15)],
        "H": [(cx-18, h-45), (cx-5, h-25), (cx+5, h-25), (cx+18, h-45), (cx, h-10)],
        "D": [(cx, h-45), (cx+20, h-25), (cx, h-5), (cx-20, h-25)],
        "C": [(cx-10, h-40), (cx+10, h-40), (cx+15, h-20), (cx, h-10), (cx-15, h-20)],
    }
    if suit in suit_symbols and suit == "S":
        pts = suit_symbols[suit]
        draw.polygon(pts, fill=color+(100,))
    elif suit == "H":
        draw.ellipse([(cx-18, h-50), (cx, h-30)], fill=color+(100,))
        draw.ellipse([(cx, h-50), (cx+18, h-30)], fill=color+(100,))
        draw.polygon([(cx-15, h-32), (cx+15, h-32), (cx, h-8)], fill=color+(100,))
    elif suit == "D":
        draw.polygon([(cx, h-48), (cx+18, h-24), (cx, h), (cx-18, h-24)], fill=color+(100,))
    elif suit == "C":
        draw.ellipse([(cx-12, h-48), (cx+12, h-22)], fill=color+(100,))
        draw.ellipse([(cx-20, h-28), (cx, h-10)], fill=color+(100,))
        draw.ellipse([(cx, h-28), (cx+20, h-10)], fill=color+(100,))

    return img


if __name__ == "__main__":
    RANKS = ["J", "Q", "K"]
    SUITS = ["S", "H", "D", "C"]
    for s in SUITS:
        for r in RANKS:
            img = make_face_art(r, s)
            img.save(f"{SRC}/{r}{s}.png")
            print(f"  {r}{s}.png saved")
    print("Done! 12 face card portraits generated.")

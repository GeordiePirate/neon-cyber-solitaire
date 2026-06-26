"""Build holographic neon playing cards — bold, readable, cyberpunk aesthetic.
Fully self-contained: generates frame, pips, and back images from scratch,
then composites all 52 cards + back."""

from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageOps
import os, math

# ── Paths ──────────────────────────────────────────────────────────────
OUT = "Assets/Sprites/Cards"
SRC = f"{OUT}/_src"
os.makedirs(SRC, exist_ok=True)
os.makedirs(OUT, exist_ok=True)

CARD_W, CARD_H = 576, 1024

# ── Neon Palette ───────────────────────────────────────────────────────
CYAN       = (0, 235, 255)
CYAN_DARK  = (0, 100, 140)
MAGENTA    = (255, 20, 147)
MAGENTA_DARK = (160, 0, 80)
WHITE      = (230, 240, 255)
DARK_BG    = (10, 8, 20)       # near-black with blue tint
PANEL_DARK = (15, 12, 30, 220) # RGBA semi-trans card body
PURPLE_GLOW = (120, 40, 200)

SUIT_COLORS = {
    "S": CYAN,     # spade
    "H": MAGENTA,  # heart
    "D": MAGENTA,  # diamond
    "C": CYAN,     # club
}
SUIT_DARK = {"S": CYAN_DARK, "H": MAGENTA_DARK, "D": MAGENTA_DARK, "C": CYAN_DARK}

# ── Font ───────────────────────────────────────────────────────────────
font_paths = [
    "/c/Windows/Fonts/bahnschrift.ttf",
    "C:/Windows/Fonts/bahnschrift.ttf",
    "/c/Windows/Fonts/arialbd.ttf",
    "/c/Windows/Fonts/consolab.ttf",
]
font_path = next((p for p in font_paths if os.path.exists(p)), None)
if not font_path:
    raise RuntimeError("No suitable font found. Install Bahnschrift or Arial Bold.")

# ── 1. Generate Frame (thick neon border + card body + subtle panel) ──
def make_frame():
    """Generate a card frame with thick neon border and filled dark body."""
    img = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))

    # Card body — dark panel with subtle gradient (lighter at top)
    body = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    draw_body = ImageDraw.Draw(body)
    for y in range(CARD_H):
        t = y / CARD_H
        r = int(8 + 4 * (1 - t))
        g = int(6 + 3 * (1 - t))
        b = int(16 + 8 * (1 - t))
        a = 200 + int(30 * (1 - t))
        draw_body.line([(0, y), (CARD_W, y)], fill=(r, g, b, a))

    # Rounded rect mask for the card panel
    mask = Image.new("L", (CARD_W, CARD_H), 0)
    draw_mask = ImageDraw.Draw(mask)
    draw_mask.rounded_rectangle([(8, 8), (CARD_W - 8, CARD_H - 8)], radius=28, fill=255)

    # Apply the rounded mask to the body
    card_body = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    card_body.paste(body, (0, 0), mask)

    # Outer glow — large blurry cyan border
    glow = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow)
    for r in range(16, 30, 2):
        a = max(0, 90 - r * 3)
        draw_glow.rounded_rectangle(
            [(r, r), (CARD_W - r, CARD_H - r)], radius=max(28 - r + 8, 4),
            outline=(0, 200, 255, a), width=2
        )

    # Main neon border — thick bright cyan
    border_pass = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    draw_border = ImageDraw.Draw(border_pass)
    draw_border.rounded_rectangle(
        [(10, 10), (CARD_W - 10, CARD_H - 10)], radius=24,
        outline=CYAN, width=5
    )

    # Inner border highlight (lighter)
    inner_pass = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    draw_inner = ImageDraw.Draw(inner_pass)
    draw_inner.rounded_rectangle(
        [(16, 16), (CARD_W - 16, CARD_H - 16)], radius=20,
        outline=(80, 240, 255, 120), width=1
    )

    # Subtle corner accents
    corner_accent = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    draw_corner = ImageDraw.Draw(corner_accent)
    accent_color = (0, 180, 255, 60)
    for (cx, cy) in [(22, 22), (CARD_W - 22, 22), (22, CARD_H - 22), (CARD_W - 22, CARD_H - 22)]:
        draw_corner.ellipse([(cx - 8, cy - 8), (cx + 8, cy + 8)], fill=accent_color)

    # Composite: glow → body → border → inner → corners
    result = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    result.alpha_composite(glow)
    result.alpha_composite(card_body)
    result.alpha_composite(border_pass)
    result.alpha_composite(inner_pass)
    result.alpha_composite(corner_accent)
    return result


# ── 2. Generate suit pips (as clean RGBA with glow) ──────────────────
def make_pips():
    """Generate each suit symbol as a glowing RGBA image on transparent bg."""
    size = 240  # big enough for the largest pip
    symbols = {}

    for suit, color in [("S", CYAN), ("H", MAGENTA), ("D", MAGENTA), ("C", CYAN)]:
        img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        cx, cy = size // 2, size // 2

        if suit == "S":    # Spade
            # Point
            draw.polygon([(cx, cy - 100), (cx - 70, cy + 20), (cx + 70, cy + 20)], fill=color)
            # Curved sides (approximate with arcs)
            draw.ellipse([(cx - 70, cy - 40), (cx, cy + 40)], fill=color)
            draw.ellipse([(cx, cy - 40), (cx + 70, cy + 40)], fill=color)
            # Stem
            draw.rectangle([(cx - 14, cy + 20), (cx + 14, cy + 60)], fill=color)
            # Base
            draw.ellipse([(cx - 20, cy + 50), (cx + 20, cy + 80)], fill=color)

        elif suit == "H":  # Heart
            draw.ellipse([(cx - 75, cy - 70), (cx + 10, cy + 20)], fill=color)
            draw.ellipse([(cx - 10, cy - 70), (cx + 75, cy + 20)], fill=color)
            draw.polygon([(cx - 60, cy), (cx + 60, cy), (cx, cy + 85)], fill=color)

        elif suit == "D":  # Diamond
            draw.polygon([(cx, cy - 90), (cx + 65, cy), (cx, cy + 90), (cx - 65, cy)], fill=color)

        elif suit == "C":  # Club
            # Three circles
            draw.ellipse([(cx - 40, cy - 80), (cx + 40, cy)], fill=color)
            draw.ellipse([(cx - 70, cy - 30), (cx, cy + 30)], fill=color)
            draw.ellipse([(cx, cy - 30), (cx + 70, cy + 30)], fill=color)
            # Stem
            draw.rectangle([(cx - 12, cy + 10), (cx + 12, cy + 55)], fill=color)
            # Base
            draw.ellipse([(cx - 18, cy + 45), (cx + 18, cy + 75)], fill=color)

        # Create glow by blurring a bright version
        glow_layer = Image.new("RGBA", (size + 60, size + 60), (0, 0, 0, 0))
        gx, gy = 30, 30
        glow_layer.paste(img, (gx, gy), img.split()[3])
        glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(16))
        # Tint glow toward purple for extra pop
        glow_px = glow_layer.load()
        gw, gh = glow_layer.size
        for y in range(gh):
            for x in range(gw):
                r, g, b, a = glow_px[x, y]
                if a > 0:
                    # Blend toward purple
                    r = int(r * 0.6 + 100 * 0.4)
                    g = int(g * 0.6 + 40 * 0.4)
                    b = int(b * 0.6 + 200 * 0.4)
                    a = min(180, a)
                    glow_px[x, y] = (r, g, b, a)

        # Composite glow behind pip
        final = Image.new("RGBA", (size + 60, size + 60), (0, 0, 0, 0))
        final.alpha_composite(glow_layer)
        final.paste(img, (gx, gy), img.split()[3])

        # White inner highlight
        highlight = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        h_draw = ImageDraw.Draw(highlight)
        h_draw.ellipse([(cx - 30, cy - 75), (cx - 5, cy - 50)], fill=(255, 255, 255, 80))
        final.paste(highlight, (gx, gy), highlight.split()[3])

        symbols[suit] = final

    return symbols


# ── 3. Generate card back ────────────────────────────────────────────
def make_back():
    """Generate a dramatic neon holographic card back."""
    img = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Dark gradient body
    for y in range(CARD_H):
        t = y / CARD_H
        r = int(12 + 6 * (1 - t))
        g = int(8 + 4 * (1 - t))
        b = int(22 + 8 * (1 - t))
        draw.line([(0, y), (CARD_W, y)], fill=(r, g, b, 230))

    # Border — thick neon with glow
    for r in range(18, 32, 2):
        a = max(0, 100 - r * 3)
        draw.rounded_rectangle(
            [(r, r), (CARD_W - r, CARD_H - r)], radius=max(28 - r + 8, 4),
            outline=(0, 200, 255, a), width=2
        )
    draw.rounded_rectangle(
        [(10, 10), (CARD_W - 10, CARD_H - 10)], radius=24,
        outline=CYAN, width=5
    )
    draw.rounded_rectangle(
        [(16, 16), (CARD_W - 16, CARD_H - 16)], radius=20,
        outline=(80, 240, 255, 120), width=1
    )

    # Central diamond pattern
    cx, cy = CARD_W // 2, CARD_H // 2
    for mult, col, width in [(1.0, CYAN, 3), (0.7, (0, 180, 255, 100), 2), (0.4, (80, 240, 255, 60), 1)]:
        sz = int(180 * mult)
        draw.polygon(
            [(cx, cy - sz), (cx + sz, cy), (cx, cy + sz), (cx - sz, cy)],
            outline=col, width=width
        )

    # Corner ornaments
    for (ox, oy) in [(40, 40), (CARD_W - 40, 40), (40, CARD_H - 40), (CARD_W - 40, CARD_H - 40)]:
        draw.ellipse([(ox - 15, oy - 15), (ox + 15, oy + 15)], outline=CYAN, width=2)
        draw.ellipse([(ox - 6, oy - 6), (ox + 6, oy + 6)], fill=(0, 200, 255, 120))

    # Holographic shimmer lines (subtle angled dashes)
    for i in range(8):
        y = 120 + i * 100
        for x in range(60, CARD_W - 60, 4):
            brightness = 20 + 15 * math.sin(x * 0.1 + i * 1.5)
            if brightness > 30:
                draw.point((x, y), fill=(0, 200, 255, int(brightness)))
                draw.point((x, y + 1), fill=(100, 200, 255, int(brightness * 0.5)))

    return img


# ── 4. Card layouts ─────────────────────────────────────────────────
def rank_pip_positions(rank):
    """Return (x_frac, y_frac) for center pips."""
    L, R, C = 0.30, 0.70, 0.50
    layouts = {
        1:  [(C, 0.50)],
        2:  [(C, 0.22), (C, 0.78)],
        3:  [(C, 0.22), (C, 0.50), (C, 0.78)],
        4:  [(L, 0.22), (R, 0.22), (L, 0.78), (R, 0.78)],
        5:  [(L, 0.22), (R, 0.22), (C, 0.50), (L, 0.78), (R, 0.78)],
        6:  [(L, 0.22), (R, 0.22), (L, 0.50), (R, 0.50), (L, 0.78), (R, 0.78)],
        7:  [(L, 0.22), (R, 0.22), (C, 0.35), (L, 0.50), (R, 0.50), (L, 0.78), (R, 0.78)],
        8:  [(L, 0.22), (R, 0.22), (C, 0.35), (L, 0.50), (R, 0.50), (C, 0.65), (L, 0.78), (R, 0.78)],
        9:  [(L, 0.20), (R, 0.20), (L, 0.38), (R, 0.38), (C, 0.50), (L, 0.62), (R, 0.62), (L, 0.80), (R, 0.80)],
        10: [(L, 0.20), (R, 0.20), (C, 0.30), (L, 0.40), (R, 0.40), (L, 0.60), (R, 0.60), (C, 0.70), (L, 0.80), (R, 0.80)],
    }
    return layouts.get(rank, [(C, 0.50)])


def draw_glow_text(draw, xy, text, font, color, anchor="mm", glow_radius=3):
    """Draw text with a multi-layered outer glow."""
    x, y = xy
    # Outer glow layers
    for radius in range(glow_radius, 0, -1):
        for dx, dy in [(radius, 0), (-radius, 0), (0, radius), (0, -radius),
                       (radius, radius), (-radius, radius), (radius, -radius), (-radius, -radius)]:
            alpha = max(30, 100 - radius * 20)
            draw.text((x + dx, y + dy), text, font=font,
                      fill=(color[0], color[1], color[2], alpha), anchor=anchor)
    # Main text
    draw.text((x, y), text, font=font, fill=WHITE, anchor=anchor)


# ── 5. Build cards ──────────────────────────────────────────────────
def build_card(rank_str, rank_val, suit, frame_img, pips, font_rank, font_pip):
    """Build a single holographic neon playing card."""
    card = frame_img.copy()
    color = SUIT_COLORS[suit]

    # Get the suit pip image
    pip_img = pips[suit]

    # ── Corner indices ──
    # Rank text in top-left and bottom-right
    rank_size = 120
    draw_glow_text(
        ImageDraw.Draw(card),
        (68, 68), rank_str, font_rank, color, "mm", glow_radius=4
    )

    # Small suit pip next to rank in corner
    corner_pip = pip_img.resize((60, 60), Image.LANCZOS)
    card.alpha_composite(corner_pip, (68 - corner_pip.size[0] // 2, 128 - corner_pip.size[1] // 2))

    # Bottom-right (rotate 180)
    corner_tile = Image.new("RGBA", (200, 220), (0, 0, 0, 0))
    ct_draw = ImageDraw.Draw(corner_tile)
    draw_glow_text(ct_draw, (100, 50), rank_str, font_rank, color, "mm", glow_radius=4)
    corner_tile.alpha_composite(corner_pip, (100 - corner_pip.size[0] // 2, 110 - corner_pip.size[1] // 2))
    corner_tile = corner_tile.rotate(180, expand=False)
    card.alpha_composite(corner_tile, (CARD_W - 200 - 14, CARD_H - 220 - 18))

    # Center pips
    pip_h = 180 if rank_val == 1 else 110
    big_pip = pip_img.resize((int(pip_img.size[0] * pip_h / pip_img.size[1]), pip_h), Image.LANCZOS)
    pip_w = big_pip.size[0]
    pip_h_actual = big_pip.size[1]

    for fx, fy in rank_pip_positions(rank_val):
        x = int(fx * CARD_W - pip_w / 2)
        y = int(fy * CARD_H - pip_h_actual / 2)
        card.alpha_composite(big_pip, (x, y))

    return card


# ── Main ───────────────────────────────────────────────────────────────
print("Generating frame...")
frame_img = make_frame()
frame_img.save(f"{SRC}/frame.png")
print("  frame.png saved")

print("Generating suit pips...")
pips = make_pips()
for s in ["S", "H", "D", "C"]:
    pips[s].save(f"{SRC}/pip_{s}.png")
print("  pips saved")

print("Generating card back...")
back_img = make_back()
back_img.save(f"{OUT}/back.png")
print("  back.png saved")

# Fonts
font_rank = ImageFont.truetype(font_path, 110)
font_pip = ImageFont.truetype(font_path, 60)

# Build all 40 number cards
RANKS = [("A", 1), ("2", 2), ("3", 3), ("4", 4), ("5", 5),
         ("6", 6), ("7", 7), ("8", 8), ("9", 9), ("10", 10)]
SUITS = ["S", "H", "D", "C"]

count = 0
for suit in SUITS:
    for rank_str, rank_val in RANKS:
        card = build_card(rank_str, rank_val, suit, frame_img, pips, font_rank, font_pip)
        card.save(f"{OUT}/{rank_str}{suit}.png")
        count += 1
        if count % 10 == 0:
            print(f"  built {count} cards...")

print(f"Done! Built {count} cards + back")

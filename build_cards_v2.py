"""Holographic neon playing cards v2 — ornate geometric style matching the reference image.
Generates ornate suit symbols, circuit-board card faces, and composites all 52 cards."""

from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageOps
import os, math, random

random.seed(42)

OUT = "Assets/Sprites/Cards"
SRC = f"{OUT}/_src"
os.makedirs(SRC, exist_ok=True)
os.makedirs(OUT, exist_ok=True)

CARD_W, CARD_H = 576, 1024

# ── Palette ──
CYAN       = (0, 230, 255)
CYAN_DIM   = (0, 180, 220)
MAGENTA    = (255, 20, 147)
MAGENTA_DIM = (200, 10, 110)
WHITE      = (230, 240, 255)
PURPLE     = (160, 40, 220)
DARK_BG    = (8, 6, 18)
PANEL      = (12, 10, 24, 235)

COLORS = {"S": CYAN, "H": MAGENTA, "D": MAGENTA, "C": CYAN}
DIMS    = {"S": CYAN_DIM, "H": MAGENTA_DIM, "D": MAGENTA_DIM, "C": CYAN_DIM}

# Font
FP = "/c/Windows/Fonts/bahnschrift.ttf"
if not os.path.exists(FP): FP = "C:/Windows/Fonts/bahnschrift.ttf"

# ─────────────────────────────────────────────
# 1. Card body with circuit-board fill
# ─────────────────────────────────────────────
def circuit_pattern(w, h, density=120):
    """Generate a circuit-board trace pattern as an RGBA layer."""
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    nodes = {}
    # Place nodes
    gs = 40
    for y in range(gs, h-gs, gs*2):
        for x in range(gs, w-gs, gs*2):
            if random.random() < 0.55:
                nodes[(x, y)] = (x, y)
    # Connect nearby nodes
    pts = list(nodes.keys())
    for i, (x1, y1) in enumerate(pts):
        for j in range(i+1, len(pts)):
            x2, y2 = pts[j]
            dx, dy = abs(x2-x1), abs(y2-y1)
            if dx < 120 and dy < 120 and (dx + dy) > 20 and random.random() < 0.3:
                color = (0, 180, 255, 20 + random.randint(0, 15))
                # L-shaped trace
                mid = (x2, y1) if random.random() < 0.5 else (x1, y2)
                draw.line([(x1, y1), mid, (x2, y2)], fill=color, width=1)
                # Tiny node dot
                draw.ellipse([(x1-2, y1-2), (x1+2, y1+2)], fill=(0, 200, 255, 30))
                draw.ellipse([(x2-2, y2-2), (x2+2, y2+2)], fill=(0, 200, 255, 30))
    return img

def make_card_body():
    """Dark card body with subtle gradient and circuit traces."""
    body = Image.new("RGBA", (CARD_W, CARD_H), (0,0,0,0))
    draw = ImageDraw.Draw(body)

    # Gradient fill
    for y in range(CARD_H):
        t = y / CARD_H
        r = int(10 + 6 * (1-t))
        g = int(8 + 4 * (1-t))
        b = int(22 + 10 * (1-t))
        a = 215 + int(30 * (1-t))
        draw.line([(0, y), (CARD_W, y)], fill=(r, g, b, a))

    # Circuit pattern overlay
    circuit = circuit_pattern(CARD_W, CARD_H)
    body.alpha_composite(circuit)

    # Rounded mask
    mask = Image.new("L", (CARD_W, CARD_H), 0)
    ImageDraw.Draw(mask).rounded_rectangle([(10,10),(CARD_W-10,CARD_H-10)], radius=26, fill=255)
    result = Image.new("RGBA", (CARD_W, CARD_H), (0,0,0,0))
    result.paste(body, (0,0), mask)
    return result

# ─────────────────────────────────────────────
# 2. Multi-layered neon border
# ─────────────────────────────────────────────
def make_border():
    """Thick neon border with inner/outer glow layers."""
    layers = Image.new("RGBA", (CARD_W, CARD_H), (0,0,0,0))

    # Outer spread glow
    for r in range(18, 35, 2):
        a = max(0, 80 - r * 2)
        img = Image.new("RGBA", (CARD_W, CARD_H), (0,0,0,0))
        ImageDraw.Draw(img).rounded_rectangle(
            [(r,r),(CARD_W-r,CARD_H-r)], radius=max(24-r+8,4),
            outline=CYAN, width=2)
        img = img.filter(ImageFilter.GaussianBlur(2))
        layers.alpha_composite(img)

    # Main bright border
    main = Image.new("RGBA", (CARD_W, CARD_H), (0,0,0,0))
    ImageDraw.Draw(main).rounded_rectangle(
        [(10,10),(CARD_W-10,CARD_H-10)], radius=24, outline=CYAN, width=5)
    layers.alpha_composite(main)

    # Inner accent
    inner = Image.new("RGBA", (CARD_W, CARD_H), (0,0,0,0))
    ImageDraw.Draw(inner).rounded_rectangle(
        [(18,18),(CARD_W-18,CARD_H-18)], radius=18,
        outline=(60, 240, 255, 100), width=1)
    layers.alpha_composite(inner)

    # Corner brackets
    for (cx, cy) in [(16,16),(CARD_W-16,16),(16,CARD_H-16),(CARD_W-16,CARD_H-16)]:
        br = Image.new("RGBA", (30,30), (0,0,0,0))
        bd = ImageDraw.Draw(br)
        bd.line([(25,10),(25,0),(0,0),(0,25),(10,25)], fill=(80, 240, 255, 100), width=2)
        layers.alpha_composite(br, (cx-15, cy-15))

    return layers

# ─────────────────────────────────────────────
# 3. Ornate suit symbols
# ─────────────────────────────────────────────
def make_ornate_pip(suit, color, size=300):
    """Generate an ornate geometric suit symbol with inner glow."""
    img = Image.new("RGBA", (size, size), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    cx, cy = size//2, size//2

    # Outer glow layer (larger, blurred)
    glow = Image.new("RGBA", (size+60, size+60), (0,0,0,0))
    gdraw = ImageDraw.Draw(glow)

    def draw_shape(d, x, y, s, c, fill=True):
        """Draw the suit symbol at position (x,y) with half-size s."""
        if suit == "S":  # Spade — angular geometric
            d.polygon([(x, y-int(s*0.85)), (x-int(s*0.55), y+int(s*0.15)),
                       (x-int(s*0.15), y-int(s*0.05)), (x-int(s*0.35), y+int(s*0.55)),
                       (x, y+int(s*0.3)), (x+int(s*0.35), y+int(s*0.55)),
                       (x+int(s*0.15), y-int(s*0.05)), (x+int(s*0.55), y+int(s*0.15))],
                      fill=c if fill else None, outline=c if not fill else None)
            d.rectangle([(x-int(s*0.08), y+int(s*0.25)), (x+int(s*0.08), y+int(s*0.5))], fill=c)
            if not fill:
                d.ellipse([(x-int(s*0.3), y-int(s*0.75)), (x+int(s*0.3), y-int(s*0.15))], outline=c, width=2)

        elif suit == "H":  # Heart — curved with inner geometry
            d.ellipse([(x-int(s*0.5), y-int(s*0.65)), (x+int(s*0.05), y+int(s*0.1))], fill=c)
            d.ellipse([(x-int(s*0.05), y-int(s*0.65)), (x+int(s*0.5), y+int(s*0.1))], fill=c)
            d.polygon([(x-int(s*0.4), y-int(s*0.05)), (x+int(s*0.4), y-int(s*0.05)), (x, y+int(s*0.6))], fill=c)
            if isinstance(c, tuple) and len(c) == 4:
                d.ellipse([(x-int(s*0.12), y-int(s*0.35)), (x+int(s*0.12), y-int(s*0.05))], fill=(255,255,255,int(min(255, c[3]*0.4))))

        elif suit == "D":  # Diamond — faceted
            d.polygon([(x, y-int(s*0.85)), (x+int(s*0.5), y), (x, y+int(s*0.85)), (x-int(s*0.5), y)], fill=c)
            # Inner facets
            d.polygon([(x, y-int(s*0.45)), (x+int(s*0.25), y), (x, y+int(s*0.45)), (x-int(s*0.25), y)], 
                      fill=(255,255,255,int(min(255, c[3]*0.3) if len(c)==4 else 60)))

        elif suit == "C":  # Club — clustered circles
            d.ellipse([(x-int(s*0.28), y-int(s*0.62)), (x+int(s*0.28), y+int(s*0.05))], fill=c)
            d.ellipse([(x-int(s*0.45), y-int(s*0.2)), (x-int(s*0.05), y+int(s*0.25))], fill=c)
            d.ellipse([(x+int(s*0.05), y-int(s*0.2)), (x+int(s*0.45), y+int(s*0.25))], fill=c)
            d.rectangle([(x-int(s*0.08), y+int(s*0.15)), (x+int(s*0.08), y+int(s*0.5))], fill=c)

    # Draw glow version (larger, blurred)
    draw_shape(gdraw, size//2+30, size//2+30, int(size*0.38), color+(60,))
    glow = glow.filter(ImageFilter.GaussianBlur(12))

    # Main shape
    draw_shape(draw, cx, cy, int(size*0.35), color+(255,))

    # Inner highlight
    hl = Image.new("RGBA", (size, size), (0,0,0,0))
    hdraw = ImageDraw.Draw(hl)
    draw_shape(hdraw, cx-8, cy-12, int(size*0.2), (255,255,255,60))
    img = Image.alpha_composite(img, hl)

    # Combine glow + main
    final = Image.new("RGBA", (size+60, size+60), (0,0,0,0))
    final.alpha_composite(glow)
    final.alpha_composite(img, (30, 30))
    return final

# ─────────────────────────────────────────────
# 4. Face card character art (AI-generated)
# ─────────────────────────────────────────────
FACE_CARD_PROMPTS = {
    ("J", "S"): "cyberpunk Jack of Spades, neon cyan glowing suit, geometric angular face, holographic circuit patterns, dark background, digital art",
    ("J", "H"): "cyberpunk Jack of Hearts, neon magenta glowing suit, romantic roguish face with holographic heart accents, dark bg, digital art",
    ("J", "D"): "cyberpunk Jack of Diamonds, neon magenta glowing suit, sharp confident face with holographic diamond facets, dark bg, digital art",
    ("J", "C"): "cyberpunk Jack of Clubs, neon cyan glowing suit, mysterious face with holographic club symbol, dark bg, digital art",
    ("Q", "S"): "cyberpunk Queen of Spades, neon cyan glowing crown, elegant regal woman with geometric holographic spade accents, dark bg, digital art",
    ("Q", "H"): "cyberpunk Queen of Hearts, neon magenta glowing crown, beautiful queen with heart-shaped holographic jewelry, dark bg, digital art",
    ("Q", "D"): "cyberpunk Queen of Diamonds, neon magenta glowing crown, glamorous queen with diamond holographic facets, dark bg, digital art",
    ("Q", "C"): "cyberpunk Queen of Clubs, neon cyan glowing crown, mysterious regal woman with club holographic accents, dark bg, digital art",
    ("K", "S"): "cyberpunk King of Spades, neon cyan glowing crown, bearded king with geometric spade hologram, dark bg, digital art",
    ("K", "H"): "cyberpunk King of Hearts, neon magenta glowing crown, heroic king with heart-shaped holographic glow, dark bg, digital art",
    ("K", "D"): "cyberpunk King of Diamonds, neon magenta glowing crown, wealthy king with diamond holographic facets, dark bg, digital art",
    ("K", "C"): "cyberpunk King of Clubs, neon cyan glowing crown, powerful king with club holographic accents, dark bg, digital art",
}

def build_number_card(rank_str, rank_val, suit, body, border, pips, font_rank, font_idx):
    """Compose a number card (A-10)."""
    card = Image.alpha_composite(body, border)
    draw = ImageDraw.Draw(card)
    color = COLORS[suit]
    pip = pips[suit]

    # Corner rank text with strong glow
    for radius in [5, 3, 1]:
        for dx, dy in [(radius,0), (-radius,0), (0,radius), (0,-radius)]:
            draw.text((78+dx, 72+dy), rank_str, font=font_rank,
                      fill=(color[0], color[1], color[2], max(40, 120-radius*20)), anchor="mm")

    draw.text((78, 72), rank_str, font=font_rank, fill=WHITE, anchor="mm")

    # Corner pip
    cp = pip.resize((56, 56), Image.LANCZOS)
    card.alpha_composite(cp, (78-cp.width//2, 130-cp.height//2))

    # Bottom-right corner (rotated)
    ct = Image.new("RGBA", (180, 200), (0,0,0,0))
    ctd = ImageDraw.Draw(ct)
    for radius in [5, 3, 1]:
        for dx, dy in [(radius,0), (-radius,0), (0,radius), (0,-radius)]:
            ctd.text((90+dx, 50+dy), rank_str, font=font_rank,
                     fill=(color[0], color[1], color[2], max(40, 120-radius*20)), anchor="mm")
    ctd.text((90, 50), rank_str, font=font_rank, fill=WHITE, anchor="mm")
    ct.alpha_composite(cp, (90-cp.width//2, 105-cp.height//2))
    ct = ct.rotate(180)
    card.alpha_composite(ct, (CARD_W-180-14, CARD_H-200-18))

    # Center pips
    pip_h = 200 if rank_val == 1 else 120
    big = pip.resize((int(pip.width * pip_h / pip.height), pip_h), Image.LANCZOS)

    layouts = {
        1: [(0.50, 0.50)], 2: [(0.50, 0.20), (0.50, 0.80)],
        3: [(0.50, 0.20), (0.50, 0.50), (0.50, 0.80)],
        4: [(0.30, 0.20), (0.70, 0.20), (0.30, 0.80), (0.70, 0.80)],
        5: [(0.30, 0.20), (0.70, 0.20), (0.50, 0.50), (0.30, 0.80), (0.70, 0.80)],
        6: [(0.30, 0.20), (0.70, 0.20), (0.30, 0.50), (0.70, 0.50), (0.30, 0.80), (0.70, 0.80)],
        7: [(0.30, 0.20), (0.70, 0.20), (0.50, 0.33), (0.30, 0.50), (0.70, 0.50), (0.30, 0.80), (0.70, 0.80)],
        8: [(0.30, 0.20), (0.70, 0.20), (0.50, 0.33), (0.30, 0.50), (0.70, 0.50), (0.50, 0.67), (0.30, 0.80), (0.70, 0.80)],
        9: [(0.30, 0.17), (0.70, 0.17), (0.30, 0.38), (0.70, 0.38), (0.50, 0.50), (0.30, 0.62), (0.70, 0.62), (0.30, 0.83), (0.70, 0.83)],
        10: [(0.30, 0.17), (0.70, 0.17), (0.50, 0.28), (0.30, 0.40), (0.70, 0.40), (0.30, 0.60), (0.70, 0.60), (0.50, 0.72), (0.30, 0.83), (0.70, 0.83)],
    }
    for fx, fy in layouts.get(rank_val, [(0.50, 0.50)]):
        x = int(fx * CARD_W - big.width / 2)
        y = int(fy * CARD_H - big.height / 2)
        card.alpha_composite(big, (x, y))

    return card


def build_face_card(rank_str, suit, body, border, pips, font_rank, character_art=None):
    """Compose a face card (J/Q/K) with character art."""
    card = Image.alpha_composite(body, border)
    draw = ImageDraw.Draw(card)
    color = COLORS[suit]
    pip = pips[suit]

    # Corner rank + pip (same as number cards)
    for radius in [5, 3, 1]:
        for dx, dy in [(radius,0), (-radius,0), (0,radius), (0,-radius)]:
            draw.text((78+dx, 72+dy), rank_str, font=font_rank,
                      fill=(color[0], color[1], color[2], max(40, 120-radius*20)), anchor="mm")
    draw.text((78, 72), rank_str, font=font_rank, fill=WHITE, anchor="mm")
    cp = pip.resize((56, 56), Image.LANCZOS)
    card.alpha_composite(cp, (78-cp.width//2, 130-cp.height//2))

    # Bottom-right
    ct = Image.new("RGBA", (180, 200), (0,0,0,0))
    ctd = ImageDraw.Draw(ct)
    for radius in [5, 3, 1]:
        for dx, dy in [(radius,0), (-radius,0), (0,radius), (0,-radius)]:
            ctd.text((90+dx, 50+dy), rank_str, font=font_rank,
                     fill=(color[0], color[1], color[2], max(40, 120-radius*20)), anchor="mm")
    ctd.text((90, 50), rank_str, font=font_rank, fill=WHITE, anchor="mm")
    ct.alpha_composite(cp, (90-cp.width//2, 105-cp.height//2))
    ct = ct.rotate(180)
    card.alpha_composite(ct, (CARD_W-180-14, CARD_H-200-18))

    # Decorative suit symbols in corners of card face
    for fx, fy in [(0.10, 0.08), (0.90, 0.08), (0.10, 0.92), (0.90, 0.92)]:
        dp = pip.resize((50, 50), Image.LANCZOS)
        card.alpha_composite(dp, (int(fx*CARD_W-dp.width/2), int(fy*CARD_H-dp.height/2)))

    # Large center suit symbol (semi-transparent watermark)
    bg_pip = pip.resize((int(pip.width*1.5), int(pip.height*1.5)), Image.LANCZOS)
    # Dim it for the background
    bg_pix = bg_pip.load()
    for y in range(bg_pip.height):
        for x in range(bg_pip.width):
            r, g, b, a = bg_pix[x, y]
            if a > 0:
                bg_pix[x, y] = (r, g, b, a//3)  # reduce opacity
    cx, cy = CARD_W//2 - bg_pip.width//2, CARD_H//2 - bg_pip.height//2
    card.alpha_composite(bg_pip, (cx, cy))

    # If we have character art, place it
    if character_art:
        art = character_art.convert("RGBA")
        # Scale to fit center area
        max_w, max_h = int(CARD_W * 0.55), int(CARD_H * 0.5)
        art.thumbnail((max_w, max_h), Image.LANCZOS)
        ax = CARD_W//2 - art.width//2
        ay = CARD_H//2 - art.height//2 + 15
        # Glow behind character
        cglow = Image.new("RGBA", (int(max_w*0.8), int(max_h*0.8)), color+(20,))
        cglow = cglow.filter(ImageFilter.GaussianBlur(25))
        card.alpha_composite(cglow, (CARD_W//2 - cglow.width//2, CARD_H//2 - cglow.height//2 + 15))
        card.alpha_composite(art, (ax, ay))

    return card


# ── Card back ──
def make_back():
    img = Image.new("RGBA", (CARD_W, CARD_H), (0,0,0,0))
    draw = ImageDraw.Draw(img)

    # Gradient body
    for y in range(CARD_H):
        t = y / CARD_H
        draw.line([(0,y),(CARD_W,y)], fill=(int(14+6*(1-t)), int(10+4*(1-t)), int(26+10*(1-t)), 235))

    # Border glow
    for r in range(20, 36, 2):
        a = max(0, 100 - r * 3)
        draw.rounded_rectangle([(r,r),(CARD_W-r,CARD_H-r)], radius=max(26-r+8,4), outline=CYAN+(a,), width=2)
    draw.rounded_rectangle([(10,10),(CARD_W-10,CARD_H-10)], radius=24, outline=CYAN, width=5)

    # Central diamond emblem
    cx, cy = CARD_W//2, CARD_H//2
    for mult in [1.0, 0.7, 0.4]:
        sz = int(160 * mult)
        col = (0, 200, 255, int(80 * mult)) if mult < 1 else CYAN
        draw.polygon([(cx, cy-sz), (cx+sz, cy), (cx, cy+sz), (cx-sz, cy)], outline=col, width=3 if mult==1 else 1)

    # Corner circles
    for (ox, oy) in [(40,40),(CARD_W-40,40),(40,CARD_H-40),(CARD_W-40,CARD_H-40)]:
        draw.ellipse([(ox-12,oy-12),(ox+12,oy+12)], outline=CYAN, width=2)
        draw.ellipse([(ox-4,oy-4),(ox+4,oy+4)], fill=CYAN+(100,))

    return img


# ─────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────
print("Building card body...")
body = make_card_body()
body.save(f"{SRC}/body.png")

print("Building border...")
border = make_border()
border.save(f"{SRC}/border.png")

print("Generating ornate suit pips...")
pips = {}
for s in ["S","H","D","C"]:
    pips[s] = make_ornate_pip(s, COLORS[s])
    pips[s].save(f"{SRC}/pip_{s}.png")

print("Generating card back...")
back = make_back()
back.save(f"{OUT}/back.png")

font_rank = ImageFont.truetype(FP, 110)

# Build number cards (A-10)
RANKS = [("A",1),("2",2),("3",3),("4",4),("5",5),("6",6),("7",7),("8",8),("9",9),("10",10)]
SUITS = ["S","H","D","C"]
count = 0
for s in SUITS:
    for rs, rv in RANKS:
        c = build_number_card(rs, rv, s, body, border, pips, font_rank, font_rank)
        c.save(f"{OUT}/{rs}{s}.png")
        count += 1
print(f"Built {count} number cards")

# Face cards — load pre-generated geometric art
FACE_DIR = f"{SRC}/faces"
FACE_RANKS = ["J","Q","K"]
face_count = 0
for s in SUITS:
    for rs in FACE_RANKS:
        art_path = f"{FACE_DIR}/{rs}{s}.png"
        char_art = Image.open(art_path).convert("RGBA") if os.path.exists(art_path) else None
        c = build_face_card(rs, s, body, border, pips, font_rank, character_art=char_art)
        c.save(f"{OUT}/{rs}{s}.png")
        face_count += 1
print(f"Built {face_count} face cards (text-only, character art TBD)")
print("DONE")

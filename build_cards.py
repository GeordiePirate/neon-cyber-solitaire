"""Composite cyberpunk playing cards from a frame template + suit pips + rank text.
Fixed version: tight alpha-threshold pip cropping, balanced layout, safe corner margins."""
from PIL import Image, ImageDraw, ImageFont
import os

SRC = "Assets/Sprites/Cards/_src"
OUT = "Assets/Sprites/Cards"
CARD_W, CARD_H = 576, 1024

CYAN = (0, 224, 255, 255)
MAGENTA = (255, 21, 148, 255)
WHITE = (235, 245, 255, 255)

frame = Image.open(f"{SRC}/frame.png").convert("RGBA").resize((CARD_W, CARD_H))

def luma_to_alpha(img, thresh=30, soft=50):
    """Convert a neon-on-black image to alpha based on RGB luminance.
    Black bg -> transparent, glowing shape -> opaque."""
    img = img.convert("RGBA"); px = img.load(); w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            m = max(r, g, b)
            if m <= thresh: a = 0
            elif m < thresh+soft: a = int(255*(m-thresh)/soft)
            else: a = 255
            px[x, y] = (r, g, b, a)
    return img

def tight_bbox(img, alpha_thresh=40):
    a = img.split()[3]
    mask = a.point(lambda p: 255 if p >= alpha_thresh else 0)
    return mask.getbbox()

grid = luma_to_alpha(Image.open(f"{SRC}/pips_grid.png"))
G = grid.size[0] // 2
def crop_pip(col, row):
    cell = grid.crop((col*G, row*G, col*G+G, row*G+G))
    bbox = tight_bbox(cell, 60)
    return cell.crop(bbox) if bbox else cell

PIPS = {
    "S": crop_pip(0, 0),  # spade  cyan
    "H": crop_pip(1, 0),  # heart  magenta
    "D": crop_pip(0, 1),  # diamond magenta
    "C": crop_pip(1, 1),  # club   cyan
}
SUIT_COLOR = {"S": CYAN, "H": MAGENTA, "D": MAGENTA, "C": CYAN}
RED = {"H", "D"}

def tint(img, color):
    img = img.convert("RGBA"); px = img.load(); w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0: continue
            lum = max(r, g, b) / 255.0
            px[x, y] = (int(color[0]*lum), int(color[1]*lum), int(color[2]*lum), a)
    return img

def scaled(img, target_h):
    r = target_h / img.size[1]
    return img.resize((max(1,int(img.size[0]*r)), target_h), Image.LANCZOS)

font_path = "/c/Windows/Fonts/bahnschrift.ttf"
if not os.path.exists(font_path): font_path = "C:/Windows/Fonts/bahnschrift.ttf"
font_rank = ImageFont.truetype(font_path, 110)

# Canonical playing-card pip layouts (x_frac, y_frac), y top->bottom.
# Columns at x=.30/.70, center .50. These match real playing cards.
def col_layout(rank):
    L, Rr, C = .30, .70, .50
    if rank == 1:  return [(C,.50)]
    if rank == 2:  return [(C,.24),(C,.76)]
    if rank == 3:  return [(C,.24),(C,.50),(C,.76)]
    if rank == 4:  return [(L,.24),(Rr,.24),(L,.76),(Rr,.76)]
    if rank == 5:  return [(L,.24),(Rr,.24),(C,.50),(L,.76),(Rr,.76)]
    if rank == 6:  return [(L,.24),(Rr,.24),(L,.50),(Rr,.50),(L,.76),(Rr,.76)]
    if rank == 7:  return [(L,.24),(Rr,.24),(C,.37),(L,.50),(Rr,.50),(L,.76),(Rr,.76)]
    if rank == 8:  return [(L,.24),(Rr,.24),(C,.37),(L,.50),(Rr,.50),(C,.63),(L,.76),(Rr,.76)]
    if rank == 9:  return [(L,.22),(Rr,.22),(L,.40),(Rr,.40),(C,.50),(L,.60),(Rr,.60),(L,.78),(Rr,.78)]
    if rank == 10: return [(L,.20),(Rr,.20),(C,.30),(L,.40),(Rr,.40),(L,.60),(Rr,.60),(C,.70),(L,.80),(Rr,.80)]
    return []

def draw_text_glow(d, xy, text, font, glow, anchor):
    x, y = xy
    for off in [(-2,0),(2,0),(0,-2),(0,2),(-2,-2),(2,2)]:
        d.text((x+off[0], y+off[1]), text, font=font, fill=(glow[0],glow[1],glow[2],70), anchor=anchor)
    d.text((x, y), text, font=font, fill=WHITE, anchor=anchor)

def render_index(rank_str, color):
    """Render a rank glyph + pip into a small transparent tile (upright).
    Returned tile can be rotated 180 cleanly for the bottom-right corner."""
    tile = Image.new("RGBA", (170, 230), (0,0,0,0))
    d = ImageDraw.Draw(tile)
    draw_text_glow(d, (85, 70), rank_str, font_rank, color, "mm")
    return tile  # pip added separately so it scales with suit

# Corner index geometry (safe margins inside the cyan border)
CX, CY = 74, 86       # center of top-left rank
PIPX, PIPY = 74, 168  # center of top-left index pip

def build_number_card(rank_str, rank_val, suit):
    card = frame.copy()
    color = SUIT_COLOR[suit]
    pip = tint(PIPS[suit], color)

    # Build ONE upright corner tile (rank glyph + small pip), then stamp it
    # top-left and rotate 180 for bottom-right. Rotating a finished tile keeps
    # the glyph crisp (no re-rasterising a rotated font).
    tile = Image.new("RGBA", (180, 250), (0,0,0,0))
    td = ImageDraw.Draw(tile)
    draw_text_glow(td, (88, 72), rank_str, font_rank, color, "mm")
    cpip = scaled(pip, 58)
    tile.alpha_composite(cpip, (88 - cpip.size[0]//2, 150 - cpip.size[1]//2))

    # top-left
    card.alpha_composite(tile, (14, 18))
    # bottom-right (rotate the whole tile 180)
    card.alpha_composite(tile.rotate(180, expand=False), (CARD_W - tile.size[0] - 14, CARD_H - tile.size[1] - 18))

    # center pips
    pip_h = 150 if rank_val == 1 else 92
    big = scaled(pip, pip_h)
    for (fx, fy) in col_layout(rank_val):
        p = big if fy <= 0.5 else big.transpose(Image.FLIP_TOP_BOTTOM)
        x = int(fx*CARD_W - p.size[0]/2)
        y = int(fy*CARD_H - p.size[1]/2)
        card.alpha_composite(p, (x, y))
    return card

os.makedirs(OUT, exist_ok=True)

# ── Build all 40 number cards (A-10 x 4 suits) ──────────────────
RANKS = [("A",1),("2",2),("3",3),("4",4),("5",5),("6",6),("7",7),("8",8),("9",9),("10",10)]
SUITS = ["S","H","D","C"]
count = 0
for su in SUITS:
    for rs, rv in RANKS:
        build_number_card(rs, rv, su).save(f"{OUT}/{rs}{su}.png")
        count += 1
print(f"built {count} number cards")

# ── Card back ──────────────────────────────────────────────────
back = Image.open(f"{SRC}/back.png").convert("RGBA").resize((CARD_W, CARD_H))
back.save(f"{OUT}/back.png")
print("saved back.png")

# clean up old sample files
for f in os.listdir(OUT):
    if f.startswith("sample_"):
        os.remove(os.path.join(OUT, f))
print("total card files:", len([f for f in os.listdir(OUT) if f.endswith('.png')]))

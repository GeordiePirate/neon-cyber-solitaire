#!/usr/bin/env python3
"""
Generate 52 cyberpunk neon playing cards + card back.
Style: neon-outlined pips, blocky neon fonts, Art Nouveau face card line art,
       dark card body with cyan/magenta glow, thin glowing borders.

Output: Assets/Resources/Cards/{key}.png at 160x224 RGBA
"""

from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops, ImagePath
import os, math, random

# ── Configuration ───────────────────────────────────────────────
CARD_W, CARD_H = 160, 224
OUT_DIR = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Resources\Cards"
BACKUP_DIR = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Resources\Cards_old"

# Neon colors
NEON_CYAN = (0, 225, 255, 255)
NEON_MAGENTA = (255, 20, 147, 255)
NEON_PURPLE = (180, 50, 255, 200)
DARK_BG = (8, 4, 20, 230)
CARD_BODY = (10, 6, 25, 235)
BORDER_COLOR = (30, 20, 60, 180)

SUIT_COLORS = {
    'H': NEON_MAGENTA,  # Hearts
    'D': NEON_MAGENTA,  # Diamonds
    'C': NEON_CYAN,     # Clubs
    'S': NEON_CYAN,     # Spades
}

RANK_ORDER = ['A', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K']
SUITS = ['C', 'D', 'H', 'S']
SUIT_SYMBOLS = {'H': '♥', 'D': '♦', 'C': '♣', 'S': '♠'}

# ── Font setup ──────────────────────────────────────────────────
# Try to find a chunky/blocky font
FONT_PATHS = [
    r"C:\Windows\Fonts\impact.ttf",
    r"C:\Windows\Fonts\Arial.ttf",
    r"C:\Windows\Fonts\arialbd.ttf",
    r"C:\Windows\Fonts\segoeui.ttf",
    r"C:\Windows\Fonts\segoeuib.ttf",
    r"C:\Windows\Fonts\consola.ttf",
    r"C:\Windows\Fonts\consolab.ttf",
    r"C:\Windows\Fonts\courbd.ttf",
    r"C:\Windows\Fonts\lucon.ttf",
]

def find_font():
    for p in FONT_PATHS:
        if os.path.exists(p):
            return p
    return None

FONT_PATH = find_font()

# ── Drawing Utilities ───────────────────────────────────────────

def glow_filter(img, radius=3, intensity=1.0):
    """Create a neon glow effect around bright pixels."""
    if img.mode != 'RGBA':
        img = img.convert('RGBA')
    # Extract alpha channel
    alpha = img.split()[3]
    # Create glow by blurring alpha
    glow_alpha = alpha.filter(ImageFilter.GaussianBlur(radius=radius))
    # Create colored glow layer
    glow = Image.new('RGBA', img.size, (0, 0, 0, 0))
    # Use the alpha to create the glow
    for x in range(img.width):
        for y in range(img.height):
            ga = glow_alpha.getpixel((x, y))
            if ga > 0:
                glow.putpixel((x, y), (255, 255, 255, min(int(ga * intensity), 255)))
    return glow

def draw_neon_text(draw, pos, text, color, font_size, glow_radius=2, glow_intensity=0.6):
    """Draw text with neon glow effect."""
    if FONT_PATH and os.path.exists(FONT_PATH):
        try:
            font = ImageFont.truetype(FONT_PATH, font_size)
        except:
            font = ImageFont.load_default()
    else:
        font = ImageFont.load_default()
    
    # Create temporary image for glow
    tmp = Image.new('RGBA', (CARD_W, CARD_H), (0, 0, 0, 0))
    tdraw = ImageDraw.Draw(tmp)
    tdraw.text(pos, text, fill=(255, 255, 255, 255), font=font)
    
    # Generate glow
    glow = glow_filter(tmp, radius=glow_radius, intensity=glow_intensity)
    
    # Apply colored glow
    for x in range(glow.width):
        for y in range(glow.height):
            gp = glow.getpixel((x, y))
            if gp[3] > 0:
                glow.putpixel((x, y), (color[0], color[1], color[2], gp[3]))
    
    # Composite glow then text
    img = Image.new('RGBA', (CARD_W, CARD_H), (0, 0, 0, 0))
    img.paste(glow, (0, 0), glow)
    
    # Draw the actual text
    text_layer = Image.new('RGBA', (CARD_W, CARD_H), (0, 0, 0, 0))
    tdraw2 = ImageDraw.Draw(text_layer)
    tdraw2.text(pos, text, fill=color, font=font)
    img.paste(text_layer, (0, 0), text_layer)
    
    return img

def draw_glowing_suit(draw, pos, suit, size, glow_radius=3):
    """Draw a glowing suit symbol."""
    color = SUIT_COLORS[suit]
    symbol = SUIT_SYMBOLS[suit]
    
    # Find font for the symbol
    if FONT_PATH and os.path.exists(FONT_PATH):
        try:
            sym_font = ImageFont.truetype(FONT_PATH, size)
        except:
            sym_font = ImageFont.load_default()
    else:
        sym_font = ImageFont.load_default()
    
    # Create temp for glow
    tmp = Image.new('RGBA', (CARD_W, CARD_H), (0, 0, 0, 0))
    tdraw = ImageDraw.Draw(tmp)
    bbox = tdraw.textbbox((0, 0), symbol, font=sym_font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    tx = pos[0] - tw // 2
    ty = pos[1] - th // 2
    tdraw.text((tx, ty), symbol, fill=(255, 255, 255, 255), font=sym_font)
    
    # Glow
    glow = glow_filter(tmp, radius=glow_radius, intensity=0.7)
    for x in range(glow.width):
        for y in range(glow.height):
            gp = glow.getpixel((x, y))
            if gp[3] > 0:
                c = color
                # Fade glow
                glow.putpixel((x, y), (c[0], c[1], c[2], min(gp[3], 180)))
    
    # Draw the symbol itself
    draw.text((tx, ty), symbol, fill=color, font=sym_font)
    
    return glow

def draw_rounded_rect(draw, rect, color, radius=4):
    """Draw a rounded rectangle."""
    x1, y1, x2, y2 = rect
    draw.rounded_rectangle(rect, radius=radius, fill=color)

def draw_neon_border(img):
    """Add a thin neon glow border around the card."""
    # Inner border glow
    border_layers = []
    for offset, alpha, radius in [(3, 60, 2), (6, 30, 3), (10, 15, 4)]:
        glow = Image.new('RGBA', img.size, (0, 0, 0, 0))
        gdraw = ImageDraw.Draw(glow)
        gdraw.rounded_rectangle(
            [offset, offset, CARD_W - offset, CARD_H - offset],
            radius=6, outline=(100, 100, 255, alpha), width=1
        )
        border_layers.append(glow)
    
    for layer in border_layers:
        img.paste(layer, (0, 0), layer)
    return img

# ── Card Body ───────────────────────────────────────────────────

def make_card_body():
    """Create subtle dark transparent background — no thick border."""
    img = Image.new('RGBA', (CARD_W, CARD_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Very subtle dark transparent panel — let the neon art shine
    draw.rounded_rectangle([1, 1, CARD_W - 2, CARD_H - 2], radius=4, fill=(5, 3, 15, 200))
    return img

# ── Pip Layouts ─────────────────────────────────────────────────

def get_pip_positions(value, suit):
    """Get positions and rotations for pip symbols based on card value."""
    positions = []
    cx, cy = CARD_W // 2, CARD_H // 2 + 5
    
    if value == 1:  # Ace
        positions = [(cx, cy)]
    elif value == 2:
        positions = [(cx, cy - 50), (cx, cy + 50)]
    elif value == 3:
        positions = [(cx, cy - 55), (cx, cy), (cx, cy + 55)]
    elif value == 4:
        positions = [(cx - 20, cy - 45), (cx + 20, cy - 45),
                     (cx - 20, cy + 45), (cx + 20, cy + 45)]
    elif value == 5:
        positions = [(cx - 20, cy - 50), (cx + 20, cy - 50),
                     (cx, cy),
                     (cx - 20, cy + 50), (cx + 20, cy + 50)]
    elif value == 6:
        positions = [(cx - 20, cy - 50), (cx + 20, cy - 50),
                     (cx - 20, cy - 10), (cx + 20, cy - 10),
                     (cx - 20, cy + 50), (cx + 20, cy + 50)]
    elif value == 7:
        positions = [(cx - 20, cy - 55), (cx + 20, cy - 55),
                     (cx - 20, cy - 20), (cx + 20, cy - 20),
                     (cx, cy + 5),
                     (cx - 20, cy + 45), (cx + 20, cy + 45)]
    elif value == 8:
        positions = [(cx - 20, cy - 55), (cx + 20, cy - 55),
                     (cx - 25, cy - 25), (cx + 25, cy - 25),
                     (cx, cy - 40),
                     (cx - 20, cy + 10), (cx + 20, cy + 10),
                     (cx, cy + 50)]
    elif value == 9:
        positions = [(cx - 22, cy - 55), (cx + 22, cy - 55),
                     (cx - 22, cy - 30), (cx + 22, cy - 30),
                     (cx - 22, cy - 5), (cx + 22, cy - 5),
                     (cx - 22, cy + 20), (cx + 22, cy + 20),
                     (cx - 22, cy + 45), (cx + 22, cy + 45)]
    elif value >= 10:
        positions = [(cx - 22, cy - 55), (cx + 22, cy - 55),
                     (cx - 22, cy - 35), (cx + 22, cy - 35),
                     (cx - 22, cy - 15), (cx + 22, cy - 15),
                     (cx - 22, cy + 5), (cx + 22, cy + 5),
                     (cx - 22, cy + 25), (cx + 22, cy + 25),
                     (cx - 22, cy + 45), (cx + 22, cy + 45)]
    
    return positions[:value] if value <= 10 else [(cx - offset, cy + y * 20 - 50) for y in range(10)]

def draw_pips(draw, suit, positions):
    """Draw small pip symbols at given positions."""
    color = SUIT_COLORS[suit]
    symbol = SUIT_SYMBOLS[suit]
    sym_size = 14
    
    try:
        sym_font = ImageFont.truetype(FONT_PATH, sym_size) if FONT_PATH else ImageFont.load_default()
    except:
        sym_font = ImageFont.load_default()
    
    for px, py in positions:
        if FONT_PATH:
            bbox = draw.textbbox((0, 0), symbol, font=sym_font)
            tw = bbox[2] - bbox[0]
            th = bbox[3] - bbox[1]
            draw.text((int(px - tw/2), int(py - th/2)), symbol, fill=color, font=sym_font)
        else:
            draw.text((int(px - 5), int(py - 7)), symbol, fill=color)

# ── Face Card Art ───────────────────────────────────────────────

def draw_face_art(draw, rank, suit, color):
    """Draw Art Nouveau-inspired face card line art."""
    cx, cy = CARD_W // 2, CARD_H // 2 + 5
    
    if rank == 'J':
        # Jack - elegant figure in profile with ornate collar
        points = {
            'head': (cx, cy - 30),
            'neck': (cx - 2, cy - 12),
            'shoulder': (cx - 18, cy + 10),
            'chest': (cx - 10, cy + 25),
            'collar_l': (cx - 15, cy - 2),
            'collar_r': (cx + 5, cy - 2),
            'crown': (cx - 5, cy - 40),
        }
        
        # Hair/head outline
        draw.arc([cx - 12, cy - 45, cx + 8, cy - 15], 200, 340, fill=color, width=2)
        # Face profile
        draw.arc([cx - 8, cy - 38, cx + 10, cy - 18], 220, 350, fill=color, width=1)
        # Neck
        draw.line([(cx - 3, cy - 15), (cx - 5, cy + 2), (cx - 8, cy + 10)], fill=color, width=1)
        # Shoulder/collar - Art Nouveau flowing lines
        draw.arc([cx - 22, cy - 5, cx + 2, cy + 30], 180, 320, fill=color, width=2)
        draw.arc([cx + 2, cy - 5, cx + 12, cy + 15], 200, 340, fill=color, width=1)
        # Ornate collar details
        for i in range(3):
            dx = i * 5
            draw.arc([cx - 15 + dx, cy - 3, cx + dx, cy + 8], 200, 340, fill=color, width=1)
        # Crown/head piece
        draw.arc([cx - 10, cy - 48, cx + 6, cy - 35], 180, 340, fill=color, width=1)
        draw.point((cx - 3, cy - 42), fill=color)  # Gem
    
    elif rank == 'Q':
        # Queen - elegant woman profile with flowing hair and crown
        # Hair - flowing Art Nouveau curves
        draw.arc([cx - 15, cy - 48, cx + 15, cy - 10], 200, 340, fill=color, width=2)
        draw.arc([cx - 20, cy - 42, cx + 5, cy - 5], 180, 350, fill=color, width=1)
        # Face
        draw.arc([cx - 8, cy - 38, cx + 10, cy - 18], 220, 350, fill=color, width=1)
        # Eye 
        draw.point((cx - 2, cy - 27), fill=color)
        # Neck with necklace
        draw.line([(cx - 2, cy - 15), (cx - 4, cy + 2)], fill=color, width=1)
        # Necklace - ornate curve
        draw.arc([cx - 12, cy - 3, cx + 6, cy + 12], 200, 340, fill=color, width=1)
        draw.point((cx - 8, cy + 2), fill=color)  # Pendant
        # Crown
        draw.arc([cx - 10, cy - 48, cx + 8, cy - 36], 190, 350, fill=color, width=2)
        for i in range(3):
            draw.point((cx - 6 + i * 6, cy - 40), fill=color)  # Crown gems
        # Flowing dress line
        draw.arc([cx - 18, cy + 8, cx + 8, cy + 35], 190, 340, fill=color, width=2)
        # Art Nouveau leaf curl
        draw.arc([cx - 25, cy + 15, cx - 8, cy + 30], 270, 90, fill=color, width=1)
    
    elif rank == 'K':
        # King - regal profile with crown, beard, ornate robe
        # Crown
        draw.arc([cx - 12, cy - 50, cx + 10, cy - 35], 190, 350, fill=color, width=2)
        for i in range(3):
            draw.point((cx - 7 + i * 7, cy - 42), fill=color)
        # Hair
        draw.arc([cx - 18, cy - 48, cx + 15, cy - 15], 190, 350, fill=color, width=2)
        # Face
        draw.arc([cx - 8, cy - 38, cx + 10, cy - 18], 220, 350, fill=color, width=1)
        # Beard
        draw.arc([cx - 5, cy - 15, cx + 10, cy + 5], 200, 340, fill=color, width=1)
        draw.arc([cx - 3, cy - 12, cx + 8, cy + 8], 180, 340, fill=color, width=1)
        # Eye
        draw.point((cx - 1, cy - 27), fill=color)
        # Neck
        draw.line([(cx - 2, cy - 15), (cx - 4, cy + 2)], fill=color, width=1)
        # Royal robe
        draw.arc([cx - 20, cy - 2, cx + 10, cy + 30], 180, 340, fill=color, width=2)
        draw.arc([cx + 2, cy - 2, cx + 15, cy + 20], 200, 350, fill=color, width=1)
        # Shoulder armor
        draw.arc([cx - 22, cy - 5, cx - 8, cy + 15], 270, 90, fill=color, width=2)
        # Ornate detail on robe
        for i in range(2):
            draw.line([(cx - 15 + i * 10, cy + 10), (cx - 12 + i * 10, cy + 25)], fill=color, width=1)

# ── Main Card Generation ────────────────────────────────────────

def generate_card(rank, suit):
    """Generate a single card image with neon cyberpunk style."""
    color = SUIT_COLORS[suit]
    symbol = SUIT_SYMBOLS[suit]
    
    # Create base card body
    img = make_card_body()
    draw = ImageDraw.Draw(img)
    
    # ── Rank index (top-left) ──
    try:
        rank_font = ImageFont.truetype(FONT_PATH, 20) if FONT_PATH else ImageFont.load_default()
    except:
        rank_font = ImageFont.load_default()
    
    # Rank text with glow
    rank_str = rank
    rank_x, rank_y = 10, 6
    draw.text((rank_x + 1, rank_y + 1), rank_str, fill=(0, 0, 0, 200), font=rank_font)
    draw.text((rank_x, rank_y), rank_str, fill=color, font=rank_font)
    
    # Suit symbol next to rank
    sym_x = rank_x + (20 if rank == '10' else 14)
    try:
        sm_font = ImageFont.truetype(FONT_PATH, 14) if FONT_PATH else ImageFont.load_default()
    except:
        sm_font = ImageFont.load_default()
    draw.text((sym_x, rank_y + 3), symbol, fill=color, font=sm_font)
    
    # ── Bottom-right index (inverted) ──
    rank_br_x = CARD_W - 10 - (20 if rank == '10' else 14)
    rank_br_y = CARD_H - 22
    draw.text((rank_br_x + 1, rank_br_y + 1), rank_str, fill=(0, 0, 0, 200), font=rank_font)
    draw.text((rank_br_x, rank_br_y), rank_str, fill=color, font=rank_font)
    draw.text((rank_br_x, rank_br_y + 18), symbol, fill=color, font=sm_font)
    
    # ── Face cards: draw art nouveau line art ──
    if rank in ('J', 'Q', 'K'):
        draw_face_art(draw, rank, suit, color)
        
        # Small pip in corner
        draw.text((rank_x + 4, rank_y + 24), symbol, fill=(color[0], color[1], color[2], 80), font=sm_font)
        draw.text((rank_br_x, rank_br_y - 2), symbol, fill=(color[0], color[1], color[2], 80), font=sm_font)
    
    # ── Number cards: draw pips ──
    else:
        value = RANK_ORDER.index(rank) + 1
        if value <= 10:
            pip_positions = get_pip_positions(value, suit)
            draw_pips(draw, suit, pip_positions)
        
        # For Ace: big centered symbol
        if value == 1:
            try:
                big_font = ImageFont.truetype(FONT_PATH, 52) if FONT_PATH else ImageFont.load_default()
            except:
                big_font = ImageFont.load_default()
            bbox = draw.textbbox((0, 0), symbol, font=big_font)
            tw = bbox[2] - bbox[0]
            th = bbox[3] - bbox[1]
            big_x = CARD_W // 2 - tw // 2
            big_y = CARD_H // 2 - th // 2
            
            # Big glow for Ace
            for glow_r in range(5, 0, -1):
                glow_alpha = max(0, 60 - glow_r * 10)
                draw.text((big_x + glow_r, big_y), symbol, fill=(color[0], color[1], color[2], glow_alpha), font=big_font)
                draw.text((big_x - glow_r, big_y), symbol, fill=(color[0], color[1], color[2], glow_alpha), font=big_font)
                draw.text((big_x, big_y + glow_r), symbol, fill=(color[0], color[1], color[2], glow_alpha), font=big_font)
                draw.text((big_x, big_y - glow_r), symbol, fill=(color[0], color[1], color[2], glow_alpha), font=big_font)
            
            draw.text((big_x, big_y), symbol, fill=color, font=big_font)
    
    # ── Thin neon outline — just a hairline ──
    draw.rounded_rectangle([2, 2, CARD_W - 3, CARD_H - 3], radius=4,
                          outline=(color[0]//2, color[1]//2, color[2]//2, 80), width=1)
    
    return img


def generate_card_back():
    """Generate the neon cyberpunk card back design."""
    img = Image.new('RGBA', (CARD_W, CARD_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Dark transparent body — no thick fill
    draw.rounded_rectangle([1, 1, CARD_W - 2, CARD_H - 2], radius=4, fill=(4, 2, 12, 220))
    
    cyan = NEON_CYAN
    purple = NEON_PURPLE
    
    # Circuit board pattern - horizontal traces
    for y in range(20, CARD_H - 20, 25):
        for x in range(15, CARD_W - 15, 20):
            if random.random() < 0.6:
                draw.line([(x, y), (x + random.randint(8, 15), y)], fill=(cyan[0], cyan[1], cyan[2], 60), width=1)
            if random.random() < 0.6:
                draw.line([(x, y), (x, y + random.randint(8, 15))], fill=(purple[0], purple[1], purple[2], 50), width=1)
    
    # Circuit nodes
    for _ in range(15):
        nx = random.randint(15, CARD_W - 15)
        ny = random.randint(20, CARD_H - 20)
        draw.ellipse([nx - 2, ny - 2, nx + 2, ny + 2], fill=(cyan[0], cyan[1], cyan[2], 100))
    
    # Diamond in center
    cx, cy = CARD_W // 2, CARD_H // 2
    diamond_pts = [(cx, cy - 35), (cx + 25, cy), (cx, cy + 35), (cx - 25, cy)]
    draw.polygon(diamond_pts, outline=(cyan[0], cyan[1], cyan[2], 120), width=2)
    diamond_inner = [(cx, cy - 25), (cx + 15, cy), (cx, cy + 25), (cx - 15, cy)]
    draw.polygon(diamond_inner, outline=(purple[0], purple[1], purple[2], 80), width=1)
    
    # Cyber text
    try:
        cb_font = ImageFont.truetype(FONT_PATH, 12) if FONT_PATH else ImageFont.load_default()
    except:
        cb_font = ImageFont.load_default()
    draw.text((cx - 10, cy - 6), "✦", fill=(cyan[0], cyan[1], cyan[2], 150), font=cb_font)
    
    # Thin hairline border
    draw.rounded_rectangle([1, 1, CARD_W - 2, CARD_H - 2], radius=4,
                          outline=(30, 20, 60, 80), width=1)
    
    return img


# ── Main ────────────────────────────────────────────────────────

def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    
    print(f"Generating 52 cards to {OUT_DIR}...")
    
    for suit in SUITS:
        for rank in RANK_ORDER:
            key = rank + suit
            img = generate_card(rank, suit)
            path = os.path.join(OUT_DIR, f"{key}.png")
            img.save(path)
            print(f"  {key}.png")
    
    # Card back
    back = generate_card_back()
    back_path = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Resources\card_back.png"
    back.save(back_path)
    print(f"  card_back.png")
    
    print(f"\nDone! {52} cards + back generated.")

if __name__ == '__main__':
    main()

#!/usr/bin/env python3
"""
Generate 52 neon cyberpunk playing cards — BOLD art visible at 160x224.
Style: bright neon suits, blocky bold font, glow bleed, thin outline.
"""

from PIL import Image, ImageDraw, ImageFilter
import os

CARD_W, CARD_H = 160, 224
OUT = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Resources\Cards"
BACK = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Resources\card_back.png"

PINK = (255, 30, 150, 255)
CYAN = (0, 225, 255, 255)
COLORS = {'H': PINK, 'D': PINK, 'C': CYAN, 'S': CYAN}
SYMS = {'H': '\u2665', 'D': '\u2666', 'C': '\u2663', 'S': '\u2660'}
RANKS = ['A','2','3','4','5','6','7','8','9','10','J','Q','K']

# Font
FONT = None
for d in [r"C:\Windows\Fonts\impact.ttf", r"C:\Windows\Fonts\arialbd.ttf",
          r"C:\Windows\Fonts\segoeuib.ttf", r"C:\Windows\Fonts\lucon.ttf",
          r"C:\Windows\Fonts\courbd.ttf"]:
    if os.path.exists(d):
        from PIL import ImageFont
        FONT = ImageFont.truetype(d, 1)
        break


def glow(img, radius=5, intensity=0.3):
    """Create colored glow aura. Returns RGBA image."""
    alpha = img.split()[3]
    blur = alpha.filter(ImageFilter.GaussianBlur(radius))
    g = Image.new('RGBA', img.size, (0,0,0,0))
    for x in range(img.size[0]):
        for y in range(img.size[1]):
            a = blur.getpixel((x,y))
            if a > 0:
                c = img.getpixel((x,y))
                g.putpixel((x,y), (c[0],c[1],c[2], min(int(a*intensity), 200)))
    return g


def make_rank_font(sz):
    try:
        return ImageFont.truetype(FONT.path if hasattr(FONT,'path') else FONT, sz)
    except:
        return ImageFont.load_default()


def card(rank, suit):
    c = COLORS[suit]
    s = SYMS[suit]
    
    # Work on a slightly larger canvas for glow bleed
    bw, bh = CARD_W+16, CARD_H+16
    big = Image.new('RGBA', (bw, bh), (0,0,0,0))
    d = ImageDraw.Draw(big)
    
    ox, oy = 8, 8
    
    # Dark transparent body
    d.rounded_rectangle([ox, oy, ox+CARD_W-1, oy+CARD_H-1], radius=4, fill=(3,2,10,200))
    
    # ── Rank index (top-left) ──
    rf = make_rank_font(24)
    sf = make_rank_font(16)
    d.text((ox+6, oy+4), rank, fill=c, font=rf)
    sx = ox + 6 + (28 if rank == '10' else 22)
    d.text((sx, oy+7), s, fill=c, font=sf)
    
    # Bottom-right
    brx = ox + CARD_W - 12 - (28 if rank == '10' else 22)
    bry = oy + CARD_H - 28
    d.text((brx, bry), rank, fill=c, font=rf)
    d.text((brx, bry+20), s, fill=c, font=sf)
    
    cx, cy = ox + CARD_W//2, oy + CARD_H//2 + 2
    
    # ── Center art ──
    val = RANKS.index(rank) + 1
    
    if rank in ('J','Q','K'):
        draw_face(d, rank, c, cx, cy)
    elif val == 1:
        # Big glowing center symbol
        bf = make_rank_font(58)
        bb = d.textbbox((0,0), s, font=bf)
        tw = bb[2]-bb[0]; th = bb[3]-bb[1]
        sx = cx - tw//2; sy = cy - th//2
        for r in range(4,0,-1):
            for dx,dy in [(r,0),(-r,0),(0,r),(0,-r)]:
                d.text((sx+dx, sy+dy), s, fill=(c[0],c[1],c[2],80-r*15), font=bf)
        d.text((sx, sy), s, fill=c, font=bf)
        d.text((sx-1, sy-1), s, fill=c, font=bf)
    else:
        # Pips
        pf = make_rank_font(20)
        for px, py in pips(val, cx, cy):
            bb = d.textbbox((0,0), s, font=pf)
            tw = bb[2]-bb[0]; th = bb[3]-bb[1]
            d.text((px-tw//2, py-th//2), s, fill=c, font=pf)
            d.text((px-tw//2-1, py-th//2-1), s, fill=(c[0],c[1],c[2],60), font=pf)
    
    # ── Thin outline ──
    d.rounded_rectangle([ox, oy, ox+CARD_W-1, oy+CARD_H-1], radius=4,
                       outline=(c[0]//3,c[1]//3,c[2]//3,50), width=1)
    
    # ── Glow aura ──
    g = glow(big, radius=5, intensity=0.3)
    
    # Crop back
    result = Image.new('RGBA', (CARD_W, CARD_H), (0,0,0,0))
    result.paste(g.crop((8,8,8+CARD_W,8+CARD_H)), (0,0), 
                 g.crop((8,8,8+CARD_W,8+CARD_H)))
    result.alpha_composite(big.crop((8,8,8+CARD_W,8+CARD_H)))
    return result


def pips(val, cx, cy):
    if val == 2: return [(cx,cy-35),(cx,cy+35)]
    if val == 3: return [(cx,cy-35),(cx,cy),(cx,cy+35)]
    if val == 4: return [(cx-18,cy-30),(cx+18,cy-30),(cx-18,cy+30),(cx+18,cy+30)]
    if val == 5: return [(cx-18,cy-30),(cx+18,cy-30),(cx,cy),(cx-18,cy+30),(cx+18,cy+30)]
    if val == 6: return [(cx-18,cy-30),(cx+18,cy-30),(cx-18,cy-5),(cx+18,cy-5),(cx-18,cy+30),(cx+18,cy+30)]
    s = 20; g = 22; ys = -42
    if val == 7: return [(cx-s,ys),(cx+s,ys),(cx-s,ys+g),(cx+s,ys+g),(cx,5),(cx-s,ys+2*g),(cx+s,ys+2*g)]
    if val == 8: return [(cx-s,ys),(cx+s,ys),(cx-s,ys+g),(cx+s,ys+g),(cx-s,5),(cx+s,5),(cx-s,ys+2*g),(cx+s,ys+2*g)]
    if val == 9: return [(cx-s,ys),(cx+s,ys),(cx,cy-15),(cx-s,ys+g),(cx+s,ys+g),(cx,cy+15),(cx-s,ys+2*g-5),(cx+s,ys+2*g-5),(cx,cy+35)]
    if val >= 10: return [(cx-s,ys-5),(cx+s,ys-5),(cx-s,cy-25),(cx+s,cy-25),(cx,cy-10),(cx,cy+5),(cx-s,cy+20),(cx+s,cy+20),(cx-s,cy+40),(cx+s,cy+40)]


def draw_face(d, rank, c, cx, cy):
    """Bold Art Nouveau face card line art - drawn 2px thick."""
    w = 2
    if rank == 'J':
        d.arc([cx-12, cy-40, cx+10, cy-10], 200, 340, fill=c, width=w)
        d.arc([cx-8, cy-35, cx+10, cy-15], 220, 350, fill=c, width=w)
        d.line([(cx-3, cy-12), (cx-5, cy+5), (cx-8, cy+12)], fill=c, width=w)
        d.arc([cx-22, cy-3, cx+5, cy+30], 180, 320, fill=c, width=w)
        d.arc([cx-12, cy-45, cx+8, cy-32], 180, 340, fill=c, width=w)
        d.arc([cx-25, cy+12, cx-5, cy+28], 270, 90, fill=c, width=w)
    elif rank == 'Q':
        d.arc([cx-18, cy-45, cx+18, cy-5], 200, 340, fill=c, width=w)
        d.arc([cx-22, cy-38, cx+8, cy-2], 180, 350, fill=c, width=w)
        d.arc([cx-8, cy-35, cx+10, cy-15], 220, 350, fill=c, width=w)
        d.line([(cx-2, cy-12), (cx-4, cy+5)], fill=c, width=w)
        d.arc([cx-14, cy-2, cx+8, cy+15], 200, 340, fill=c, width=w)
        d.arc([cx-12, cy-45, cx+10, cy-33], 190, 350, fill=c, width=w)
        d.arc([cx-20, cy+10, cx+10, cy+38], 190, 340, fill=c, width=w)
        d.point((cx-5, cy-25), fill=c)
        d.point((cx-10, cy+3), fill=c)
        for i in range(3): d.point((cx-6+i*6, cy-38), fill=c)
    elif rank == 'K':
        d.arc([cx-14, cy-47, cx+12, cy-32], 190, 350, fill=c, width=w)
        d.arc([cx-20, cy-45, cx+18, cy-12], 190, 350, fill=c, width=w)
        d.arc([cx-8, cy-35, cx+10, cy-15], 220, 350, fill=c, width=w)
        d.arc([cx-5, cy-12, cx+10, cy+8], 200, 340, fill=c, width=w)
        d.line([(cx-2, cy-12), (cx-4, cy+5)], fill=c, width=w)
        d.arc([cx-22, cy+2, cx+12, cy+32], 180, 340, fill=c, width=w)
        d.arc([cx-24, cy-2, cx-6, cy+18], 270, 90, fill=c, width=w)
        d.point((cx-2, cy-25), fill=c)
        for i in range(3): d.point((cx-7+i*7, cy-40), fill=c)


def card_back():
    bw, bh = CARD_W+16, CARD_H+16
    big = Image.new('RGBA', (bw, bh), (0,0,0,0))
    d = ImageDraw.Draw(big)
    ox, oy = 8, 8
    d.rounded_rectangle([ox, oy, ox+CARD_W-1, oy+CARD_H-1], radius=4, fill=(3,2,8,220))
    d.rounded_rectangle([ox, oy, ox+CARD_W-1, oy+CARD_H-1], radius=4, outline=(30,20,60,60), width=1)
    
    cx, cy = ox+CARD_W//2, oy+CARD_H//2
    pts = [(cx,cy-35),(cx+25,cy),(cx,cy+35),(cx-25,cy)]
    d.polygon(pts, outline=(CYAN[0],CYAN[1],CYAN[2],150), width=3)
    pts2 = [(cx,cy-22),(cx+15,cy),(cx,cy+22),(cx-15,cy)]
    d.polygon(pts2, outline=(CYAN[0],CYAN[1],CYAN[2],80), width=1)
    d.text((cx-8, cy-7), "\u2726", fill=(CYAN[0],CYAN[1],CYAN[2],200), font=make_rank_font(14))
    
    g = glow(big, radius=6, intensity=0.3)
    result = Image.new('RGBA', (CARD_W, CARD_H), (0,0,0,0))
    result.paste(g.crop((8,8,8+CARD_W,8+CARD_H)), (0,0), 
                 g.crop((8,8,8+CARD_W,8+CARD_H)))
    result.alpha_composite(big.crop((8,8,8+CARD_W,8+CARD_H)))
    return result


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    print("Generating...")
    for suit in ['C','D','H','S']:
        for rank in RANKS:
            card(rank, suit).save(os.path.join(OUT, f"{rank}{suit}.png"))
    card_back().save(BACK)
    print("Done! 52 cards + back")

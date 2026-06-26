#!/usr/bin/env python3
"""Resize 576x1024 card art to 160x224 for Unity Resources"""
from PIL import Image
import os, sys

src_dir = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Sprites\Cards"
dst_dir = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Resources\Cards"
back_src = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Sprites\Cards\back.png"
back_dst = r"C:\Users\Computer\NeonCyberSolitaire\Assets\Resources\card_back.png"

os.makedirs(dst_dir, exist_ok=True)
count = 0

for fname in sorted(os.listdir(src_dir)):
    if not fname.endswith(".png") or fname.startswith("_") or fname == "back.png":
        continue
    src_path = os.path.join(src_dir, fname)
    dst_path = os.path.join(dst_dir, fname)
    try:
        img = Image.open(src_path).convert("RGBA")
        resized = img.resize((160, 224), Image.LANCZOS)
        resized.save(dst_path)
        count += 1
        if count % 5 == 0:
            print(f"  [{count}/52] {fname}")
    except Exception as e:
        print(f"  FAILED {fname}: {e}")

print(f"\nDone: {count} cards resized to 160x224")

# Card back
if os.path.exists(back_src):
    img = Image.open(back_src).convert("RGBA")
    resized = img.resize((160, 224), Image.LANCZOS)
    resized.save(back_dst)
    print(f"card_back resized and saved")

print("All done!")

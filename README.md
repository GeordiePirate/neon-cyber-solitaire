# ⚡ NEON CYBER SOLITAIRE

A high-energy, neon-drenched arcade take on classic Klondike Solitaire. Built for mobile (iOS/Android) in Unity.

## 🎮 Concept

Standard Klondike rules with a cyberpunk twist:

- **Streak Multiplier** — Fast moves build a combo (x1.5 → x4.0), boosting your score
- **Net-Scan** — Once per game, scan all face-down cards to reveal their values for 3 seconds
- **Neon Visuals** — URP Bloom + HDR colors for that glowing cyber aesthetic
- **Haptic Feedback** — Satisfying clicks and bass drops on combos

## 📁 Project Structure

```
NeonCyberSolitaire/
├── Assets/
│   ├── Scripts/           # All C# source code
│   │   ├── CardData.cs            # Card enum & data structure
│   │   ├── BoardManager.cs        # Deck shuffle, deal, game logic
│   │   ├── AbilityManager.cs      # Net-Scan power-up
│   │   ├── CardVisualController.cs # Neon glow & animations
│   │   ├── CardInputHandler.cs    # Touch/drag controls
│   │   ├── ScoreManager.cs        # Streak multiplier & scoring
│   │   ├── FloatingText.cs        # Combo popup animations
│   │   ├── FoundationDropZone.cs  # Foundation drop targets
│   │   └── TableauLayoutManager.cs # Card positioning
│   ├── Sprites/           # Card art and UI elements (create these)
│   ├── Prefabs/           # Reusable card prefab
│   ├── Fonts/             # Custom cyber typography
│   └── Audio/             # Synthwave soundtrack & SFX
├── Packages/
│   └── manifest.json
├── ProjectSettings/
│   └── ...
└── README.md
```

## 🚀 Getting Started

### Prerequisites

1. **Install Unity Hub** — https://unity.com/download
2. **Install Unity 6 (6000.0.27f1)** via Unity Hub
   - Add **Universal Render Pipeline** and **Android/iOS Build Support**
3. **Clone or open this folder** in Unity Hub

### First Launch

1. Open the project in Unity Hub
2. When prompted, let Unity import and compile all scripts
3. Install URP: `Window → Package Manager → Universal RP → Install`
4. Create a URP Asset: `Create → Rendering → URP Asset (with 2D Renderer)`
5. Assign it: `Edit → Project Settings → Graphics → Scriptable Render Pipeline Settings`
6. Set up the scene:
   - Create a `Canvas` (screen space - overlay)
   - Drop `BoardManager`, `ScoreManager`, `AbilityManager` scripts on an empty GameObject
   - Create your Card Prefab with `CardVisualController` + `CardInputHandler`
   - Set up `TableauLayoutManager` with the prefab reference
   - Add a **Global Volume** with **Bloom** override

### Build & Run

- **Play in Editor:** Hit the Play button
- **Build for Android:** `File → Build Settings → Android → Build`
- **Build for iOS:** `File → Build Settings → iOS → Build`

## 🎨 Visual Style Guide

| Element | Colour | Hex |
|---------|--------|-----|
| Red Suits | Neon Pink | `#FF0F94` |
| Black Suits | Electric Cyan | `#00E0FF` |
| Net-Scan | Wireframe Green | `#33FF33` |
| Face-down | Dim Circuit | `#4D4D66` |
| Background | Near Black | `#0D0D1A` |
| x4.0 Streak | Hot Pink | `#FF3399` |

## 🕹️ Controls

- **Tap & Drag** — Move cards between columns
- **Tap Stock** — Draw new cards
- **Double-tap card** — Quick-send to foundation
- **Net-Scan button** — Activate once-per-game peek ability

## 🔧 Extending

The architecture is modular:

- **`BoardManager`** — All game logic. Add new rules by extending move validation.
- **`ScoreManager`** — Tweak `streakWindow`, `maxMultiplier`, and point values.
- **`AbilityManager`** — Add new powers by creating new methods and events.
- **`CardVisualController`** — Swap in your own sprites and VFX.
- **`CardInputHandler`** — Add gestures (swipe, double-tap) easily.

---

*Built with Unity 6 · URP 2D Renderer · Designed for Mobile*

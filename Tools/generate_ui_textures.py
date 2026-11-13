"""Generate ZCraft UI textures in the project art style.

Style source: Content/ART_STYLE.md — voxel industrial, stepped silhouettes,
graphite metal, functional colour accents (orange = heat/warning,
cyan = power/automation, red = danger, green = nominal).

All textures are 9-slice friendly: the outer band stays constant when the
texture is stretched, so Godot's StyleBoxTexture margins can scale the middle.

Run:  python Tools/generate_ui_textures.py
Out:  Resources/Textures/UI/*.png
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent.parent / "Resources" / "Textures" / "UI"

# Palette
CLEAR = (0, 0, 0, 0)
SHADOW = (14, 16, 19, 255)
DARK = (26, 29, 33, 255)
FILL = (47, 52, 58, 255)
FILL_HI = (60, 66, 74, 255)
LIGHT = (96, 104, 118, 255)
PEAK = (126, 136, 152, 255)
ORANGE = (255, 138, 30, 255)
CYAN = (53, 196, 224, 255)


def stepped_box(x0: int, y0: int, x1: int, y1: int, cut: int = 4) -> list[tuple[int, int]]:
    """Rectangle with cut (chamfered) corners instead of rounded ones.

    No anti-aliasing and no arcs: the silhouette stays hard-edged, which is what
    the voxel industrial style in Content/ART_STYLE.md asks for.
    """
    return [
        (x0 + cut, y0), (x1 - cut, y0),
        (x1, y0 + cut), (x1, y1 - cut),
        (x1 - cut, y1), (x0 + cut, y1),
        (x0, y1 - cut), (x0, y0 + cut),
    ]


def base_plate(size: tuple[int, int], cut: int, fill, edge_light, edge_dark) -> Image.Image:
    """One bevelled plate: stepped silhouette, light top-left edge, dark bottom-right."""
    img = Image.new("RGBA", size, CLEAR)
    d = ImageDraw.Draw(img)
    w, h = size

    d.polygon(stepped_box(0, 0, w - 1, h - 1, cut), fill=fill)
    # bevelled edges: light on top+left, shadow on bottom+right
    d.line([(cut, 0), (w - 1 - cut, 0)], fill=edge_light)
    d.line([(0, cut), (0, h - 1 - cut)], fill=edge_light)
    d.line([(cut, h - 1), (w - 1 - cut, h - 1)], fill=edge_dark)
    d.line([(w - 1, cut), (w - 1, h - 1 - cut)], fill=edge_dark)
    # corner step highlights
    for x, y in ((cut, 0), (w - 1 - cut, 0), (0, cut), (w - 1, cut)):
        d.point((x, y), fill=edge_light)
    return img


def rivet(d: ImageDraw.ImageDraw, cx: int, cy: int, colour) -> None:
    """One rivet: a lit square with a shadow pixel — the voxel version of a screw."""
    d.rectangle([cx, cy, cx + 1, cy + 1], fill=colour)


def panel() -> Image.Image:
    img = base_plate((96, 96), 8, FILL, PEAK, SHADOW)
    d = ImageDraw.Draw(img)
    # inner recess
    d.rectangle([16, 16, 79, 79], outline=DARK)
    d.rectangle([17, 17, 78, 78], outline=FILL_HI)
    for x, y in ((6, 6), (87, 6), (6, 87), (87, 87)):
        rivet(d, x, y, LIGHT)
    return img


def button(state: str) -> Image.Image:
    fill = {"normal": FILL_HI, "hover": (72, 79, 89, 255), "pressed": DARK}[state]
    edge = ORANGE if state == "hover" else PEAK
    img = base_plate((64, 32), 4, fill, edge, SHADOW)
    d = ImageDraw.Draw(img)
    if state == "pressed":
        d.rectangle([4, 4, 59, 27], outline=SHADOW)  # inset, no bottom bevel
        d.line([(4, 4), (59, 4)], fill=DARK)
    else:
        d.line([(8, 26), (55, 26)], fill=ORANGE if state == "hover" else LIGHT)
    if state == "hover":
        d.rectangle([2, 2, 61, 29], outline=ORANGE)
    return img


def slot() -> Image.Image:
    img = base_plate((64, 64), 4, DARK, LIGHT, SHADOW)
    d = ImageDraw.Draw(img)
    d.rectangle([8, 8, 55, 55], fill=(20, 22, 26, 255), outline=FILL)
    d.line([(8, 8), (55, 8)], fill=(12, 13, 15, 255))
    d.line([(8, 8), (8, 55)], fill=(12, 13, 15, 255))
    # corner notches, so the slot reads as a socket
    for x, y in ((4, 4), (59, 4), (4, 59), (59, 59)):
        d.rectangle([x - 1, y - 1, x + 1, y + 1], fill=FILL_HI)
    return img


def bar(kind: str) -> Image.Image:
    if kind == "bg":
        img = base_plate((64, 16), 2, DARK, FILL, SHADOW)
        ImageDraw.Draw(img).rectangle([6, 6, 57, 9], fill=(16, 18, 21, 255))
        return img
    img = base_plate((64, 16), 2, CYAN, (150, 230, 245, 255), (20, 90, 105, 255))
    d = ImageDraw.Draw(img)
    d.rectangle([6, 6, 57, 9], fill=(70, 210, 235, 255))
    return img


def preview(images: list[tuple[str, Image.Image]]) -> Image.Image:
    pad, scale = 16, 2
    width = max(im.width for _, im in images) * scale + pad * 2
    height = sum(im.height * scale + pad for _, im in images) + pad
    sheet = Image.new("RGBA", (width, height), (24, 26, 30, 255))
    y = pad
    for _, im in images:
        big = im.resize((im.width * scale, im.height * scale), Image.Resampling.NEAREST)
        sheet.alpha_composite(big, (pad, y))
        y += big.height + pad
    return sheet


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    assets = {
        "panel_metal.png": panel(),
        "button_normal.png": button("normal"),
        "button_hover.png": button("hover"),
        "button_pressed.png": button("pressed"),
        "slot.png": slot(),
        "bar_bg.png": bar("bg"),
        "bar_fill.png": bar("fill"),
    }
    for name, img in assets.items():
        img.save(OUT / name)
        print(f"{name}: {img.width}x{img.height}")

    preview(list(assets.items())).save(OUT / "preview.png")
    print(f"preview.png -> {OUT}")


if __name__ == "__main__":
    main()

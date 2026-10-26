"""Prepare user-supplied sprites without changing the original PNGs."""
from pathlib import Path
from collections import deque
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "assets" / "武器五种"
OUTPUT = SOURCE / "处理后"
FILES = {
    "defect": "1c967f0b73c77bd63a70cdde0e19c7d1_720.png",
    "ironclad": "249a8096f631b5bfc37d07bb0f91755d.png",
    "necrobinder": "55ce63d2f0fcdad29f71dbdd1338a9e5.png",
    "regent": "ac9b11a00356faffc988bc3db2ef1d0f.png",
    "silent": "d5e3408ed281925ea1c5246e0a1d7b13.png",
}


def remove_magenta(image):
    """Only remove magenta connected to the outside, not isolated art details."""
    pixels = image.load()
    width, height = image.size
    visited = bytearray(width * height)
    queue = deque()
    for x in range(width):
        queue.extend(((x, 0), (x, height - 1)))
    for y in range(height):
        queue.extend(((0, y), (width - 1, y)))
    while queue:
        x, y = queue.popleft()
        index = y * width + x
        if visited[index]:
            continue
        visited[index] = 1
        r, g, b, a = pixels[x, y]
        if not (r > 95 and b > 90 and min(r, b) - g > 35 and abs(r - b) < 100):
            continue
        pixels[x, y] = (0, 0, 0, 0)
        if x > 0:
            queue.append((x - 1, y))
        if x + 1 < width:
            queue.append((x + 1, y))
        if y > 0:
            queue.append((x, y - 1))
        if y + 1 < height:
            queue.append((x, y + 1))
    return image


def prepare():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for character, filename in FILES.items():
        with Image.open(SOURCE / filename) as source:
            image = source.convert("RGBA")
        if character == "ironclad":
            image = remove_magenta(image)
        # Keep the supplied empty canvas: it encodes the first-person composition.
        image.thumbnail((1280, 800), Image.Resampling.LANCZOS)
        image.save(OUTPUT / f"{character}.png", optimize=True)
        alpha = image.getchannel("A")
        assert alpha.getextrema()[0] == 0 and alpha.getextrema()[1] == 255
        assert alpha.getpixel((0, 0)) == 0
        assert alpha.getbbox() is not None
        print(f"{character}: {image.width}x{image.height}, transparent PNG verified")
        if character in ("regent", "defect"):
            split_casting(image, character)


def split_casting(image, character):
    """Separate the supplied floating energy from the hand; no new art is generated."""
    width, height = image.size
    region = Image.new("L", image.size)
    if character == "defect":
        # 电球轮廓，避开机械指尖与手腕。
        contour = [(0.64, 0.43), (0.84, 0.43), (0.84, 0.63), (0.80, 0.715),
                   (0.72, 0.715), (0.705, 0.70), (0.698, 0.678),
                   (0.674, 0.653), (0.650, 0.646), (0.64, 0.63)]
    else:
        # 星辉下沿沿着手指上方分开，保留原来覆盖手心的柔光。
        contour = [(0.68, 0.35), (0.90, 0.35), (0.91, 0.59), (0.871, 0.605),
                   (0.861, 0.626), (0.855, 0.68), (0.825, 0.715), (0.792, 0.73),
                   (0.770, 0.668), (0.751, 0.647), (0.736, 0.657),
                   (0.721, 0.627), (0.693, 0.625), (0.68, 0.61)]
    ImageDraw.Draw(region).polygon([(int(x*width), int(y*height)) for x, y in contour], fill=255)
    mask = region.load()
    energy = Image.new("RGBA", image.size)
    energy.paste(image, mask=region)
    hand = image.copy()
    hand_pixels = hand.load()
    for y in range(height):
        for x in range(width):
            if mask[x, y]:
                hand_pixels[x, y] = (0, 0, 0, 0)
    assert energy.getchannel("A").getbbox() is not None
    hand.save(OUTPUT / f"{character}_hand.png", optimize=True)
    energy.save(OUTPUT / f"{character}_energy.png", optimize=True)
    print(f"{character}: hand/energy layers prepared")


if __name__ == "__main__":
    prepare()

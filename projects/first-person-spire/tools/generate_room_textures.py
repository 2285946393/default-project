"""通过用户指定的免费魔搭创空间生成房间补充纹理，令牌不进入输出。"""
import base64
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[3]
OUTPUT = ROOT / "assets" / "杀戮尖塔测试" / "房间纹理"
BASE = "https://studio-lf3000y-image-studio.api-inference.modelscope.net"
PROMPTS = {
    "wall": "A seamless tileable game environment wall texture, flat straight-on orthographic view, hand painted stylized 2D illustration with bold irregular outlines and soft broad brush shading, large weathered warm gray stone blocks with sparse small moss patches and thin climbing vines at the edges, muted colors, readable soft details, gentle ambient lighting, no dramatic darkness, no perspective, no room, no characters, no weapons, no text, no frame, no windows, no doors, no floor, fill the entire square with wall material, evenly distributed detail, compatible with a whimsical illustrated card game.",
    "floor": "A seamless tileable game ground texture, perfectly top-down orthographic view, hand painted stylized 2D illustration with bold irregular outlines and soft broad brush shading, broad uneven worn warm gray stone slabs with scattered small pebbles and very sparse moss in cracks, irregular natural joints rather than a perfect checkerboard, muted colors, gentle even ambient lighting, no perspective, no horizon, no buildings, no characters, no weapons, no text, no border, fill the entire square with ground material, compatible with a whimsical illustrated card game.",
    "ceiling": "A seamless tileable game overhead canopy texture, perfectly flat orthographic material view looking upward, hand painted stylized 2D illustration with bold irregular outlines and soft broad brush shading, broad overlapping soft sage leaves and a few thin branches over muted pale green haze, sparse readable detail, gentle even ambient light, muted colors, no perspective, no landscape, no horizon, no buildings, no characters, no text, no border, fill the entire square with overhead canopy material, compatible with a whimsical illustrated card game.",
}
COMMON = (
    "A seamless tileable game material texture, flat orthographic view, hand painted "
    "stylized 2D illustration with bold irregular outlines and soft broad brush shading, "
    "muted colors and gentle even ambient light, readable details, no perspective, "
    "no horizon, no characters, no weapons, no text, no frame, no scene composition, "
    "material fills the entire square, whimsical illustrated card game style. "
)
PROMPTS.update({
    "hive-wall": COMMON + "Straight-on wall view: muted ochre hexagonal wax cells and broad intertwined olive brown woody roots, small teal bioluminescent dots, dry organic insect nest architecture, not stone bricks, no horror, no flesh.",
    "hive-floor": COMMON + "Perfect top-down ground view: irregular flattened amber ochre hexagonal wax slabs and a few broad olive roots, tiny teal glowing specks in seams, dry solid nest floor, no perspective.",
    "hive-ceiling": COMMON + "Upward material view: interwoven olive brown roots and muted amber wax ridges with a few tiny teal glow dots, an organic nest roof, no sky, no leaves, no flesh.",
    "glory-wall": COMMON + "Straight-on wall view: weathered blue gray rectangular masonry, occasional broad vertical stone relief grooves, subtle pale turquoise mineral veins, old monumental city architecture, no moss, no plants.",
    "glory-floor": COMMON + "Perfect top-down ground view: blue gray irregular angular stone paving and a few small broken stone fragments, subtle pale turquoise seams, worn monumental city floor, no plants.",
    "glory-ceiling-flat": COMMON + "Only a close flat surface of blue gray weathered ceiling masonry and sparse short shallow carved turquoise grooves, every stone has the same scale across the entire image, parallel outlines, no long ribs, no vanishing point, no vertical building, no columns, no tower, no sky, no perspective, no depth composition. This is a flat repeatable material swatch, not an illustration of architecture.",
})

def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    token = Path("D:/software/dsh-home/.modelscope-token").read_text(encoding="utf-8").strip()
    opener = urllib.request.build_opener(
        urllib.request.ProxyHandler({"https": "http://127.0.0.1:7892"}))
    (OUTPUT / "prompts.json").write_text(
        json.dumps(PROMPTS, ensure_ascii=False, indent=2), encoding="utf-8")
    for name, prompt in PROMPTS.items():
        target = OUTPUT / f"{name}-v1.png"
        if target.exists():
            print(f"{name}: existing image retained", flush=True)
            continue
        request = urllib.request.Request(BASE + "/v1/images/generations",
            data=json.dumps({"model": "qwen-image-2.1", "prompt": prompt,
                             "size": "1024x1024", "n": 1}).encode(),
            headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"})
        with opener.open(request, timeout=150) as response:
            result = json.load(response)
        item = result["data"][0]
        if item.get("b64_json"):
            content = base64.b64decode(item["b64_json"])
        else:
            with opener.open(item["url"], timeout=90) as response:
                content = response.read()
        if not content.startswith(b"\x89PNG\r\n\x1a\n"):
            raise ValueError("Expected PNG image")
        target.write_bytes(content)
        # 不保存临时签名链接，避免泄露访问参数。
        print(f"{name}: saved {len(content)} bytes", flush=True)
    from PIL import Image, ImageDraw
    preview = Image.new("RGB", (768, 840), "#f5f4ec")
    draw = ImageDraw.Draw(preview)
    for row, prefix in enumerate(("", "hive-", "glory-")):
        for column, surface in enumerate(("wall", "floor", "ceiling")):
            selected = "glory-ceiling-flat" if prefix == "glory-" and surface == "ceiling" else prefix + surface
            with Image.open(OUTPUT / f"{selected}-v1.png") as image:
                preview.paste(image.convert("RGB").resize((256, 256)),
                              (column * 256, row * 280 + 24))
            draw.text((column * 256 + 8, row * 280 + 5),
                      f"{prefix or 'overgrowth-'}{surface}", fill="#222222")
    preview.save(OUTPUT / "preview-v1.jpg", quality=88)

if __name__ == "__main__":
    main()

"""把抠出来的一代遗物图规整成二代能用的尺寸。

解决两个实测出来问题：
  1. **比正式遗物「小一节」**：一代的图是 128x128 且四周有透明留白，
     二代的是 85x85 贴边；放进同一个格子一起缩放，我们那张就显得小。
  2. **详情页显示成锁头**：详情大图走 BigIconPath（原版 256x256），
     只给小图标的话，缺的那张会回退成 locked_model.png（锁）。
     ⚠️ 所以**三张图（小图 / 轮廓 / 大图）必须都给**，一件都不能省。

做法：以**小图标**的透明边界为准，同一件遗物的三张图用**同一个裁剪框和缩放比**，
保证轮廓与大图跟小图标严格对齐。一代没画大图的，用规整后的小图放大补一张（会有点糊，但比锁头好）。

用法（在工程根目录跑）：
    python tools/normalize_icons.py
它会把 QuestSpire/images/relics/ 下成对的图就地规整。重复跑是安全的（幂等）。
"""
import os
from PIL import Image

DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                   "QuestSpire", "images", "relics")
ICON = 85    # 原版小图标尺寸
BIG = 256    # 原版大图尺寸


def alpha_bbox(im):
    return im.getchannel("A").getbbox()


def make_normalizer(icon):
    """按小图标的透明边界算出「裁剪 + 缩放 + 居中」这一步，三张图共用。"""
    bbox = alpha_bbox(icon)
    if bbox is None:
        return None
    cw, ch = bbox[2] - bbox[0], bbox[3] - bbox[1]
    scale = ICON / max(cw, ch)

    def normalize(im):
        cropped = im.crop(bbox)
        nw, nh = max(1, round(cw * scale)), max(1, round(ch * scale))
        cropped = cropped.resize((nw, nh), Image.LANCZOS)
        canvas = Image.new("RGBA", (ICON, ICON), (0, 0, 0, 0))
        canvas.paste(cropped, ((ICON - nw) // 2, (ICON - nh) // 2))
        return canvas

    return normalize


def process(name):
    icon_p = os.path.join(DIR, name + ".png")
    out_p = os.path.join(DIR, name + "_outline.png")
    big_p = os.path.join(DIR, name + "_big.png")

    icon = Image.open(icon_p).convert("RGBA")
    normalize = make_normalizer(icon)
    if normalize is None:
        print("%-16s 整张图都是透明的，跳过" % name)
        return

    small = normalize(icon)
    small.save(icon_p, "PNG")

    if os.path.exists(out_p):
        normalize(Image.open(out_p).convert("RGBA")).save(out_p, "PNG")

    if os.path.exists(big_p):
        src_note = "一代的大图"
        big = Image.open(big_p).convert("RGBA")
        bb = alpha_bbox(big)
        if bb:
            big = big.crop(bb)
            bw, bh = big.size
            s = BIG / max(bw, bh)
            big = big.resize((max(1, round(bw * s)), max(1, round(bh * s))), Image.LANCZOS)
            canvas = Image.new("RGBA", (BIG, BIG), (0, 0, 0, 0))
            canvas.paste(big, ((BIG - big.size[0]) // 2, (BIG - big.size[1]) // 2))
            big = canvas
    else:
        src_note = "小图放大补的"
        big = small.resize((BIG, BIG), Image.LANCZOS)

    big.save(big_p, "PNG")
    print("%-16s → 小图 %dx%d / 大图 %dx%d（%s）" % (name, ICON, ICON, BIG, BIG, src_note))


def main():
    if not os.path.isdir(DIR):
        raise SystemExit("找不到目录: " + DIR)
    names = sorted(
        f[:-4] for f in os.listdir(DIR)
        if f.endswith(".png") and not f.endswith("_outline.png") and not f.endswith("_big.png")
    )
    print("目录: %s" % DIR)
    print("要规整 %d 件：%s\n" % (len(names), "、".join(names)))
    for n in names:
        process(n)
    print("\n完成。改完图记得跑 tools/build_pck.ps1 重新打包，再 dotnet build 部署。")


if __name__ == "__main__":
    main()

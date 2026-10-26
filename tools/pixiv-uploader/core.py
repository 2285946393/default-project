"""NovelAI 参数提取 + 上传清单管理。

设计要点：
- NovelAI 的 PNG 自带完整生成参数（Comment 字段里的 JSON），不需要另外写 md。
- 但打码/重存会把这些元数据全部洗掉，所以必须在打码之前先提取并存进清单。
- 清单跟图片放一起，文件名 `_上传清单.json`，可以随便改、可以手工编辑。
"""

from __future__ import annotations

import json
import re
import time
from dataclasses import dataclass, field, asdict
from pathlib import Path

from PIL import Image

MANIFEST_NAME = "_上传清单.json"
IMAGE_EXTS = {".png", ".jpg", ".jpeg", ".webp"}

# NovelAI 权重写法：{{tag}} / [[tag]] / 1.2::tag::
_WEIGHT_NUM = re.compile(r"^-?\d+(?:\.\d+)?::(.*?)::$")
_ARTIST = re.compile(r"^artist:\s*(.+)$", re.IGNORECASE)
# 不适合当 Pixiv 标签的通用质量词
_NOISE_TAGS = {
    "best quality", "amazing quality", "very aesthetic", "absurdres", "highres",
    "masterpiece", "no text", "highly detailed", "8k image", "8k wallpaper",
}


def strip_weights(tag: str) -> str:
    """去掉 NovelAI 的权重标记，只留纯标签文本。"""
    t = tag.strip()
    m = _WEIGHT_NUM.match(t)
    if m:
        t = m.group(1)
    prev = None
    while prev != t:          # 层层剥掉 {{ }} / [[ ]]
        prev = t
        t = t.strip("{}[]").strip()
    return t.strip()


def split_tags(prompt: str) -> list[str]:
    """按逗号拆标签，去掉权重和空项。"""
    if not prompt:
        return []
    return [t for t in (strip_weights(p) for p in prompt.split(",")) if t]


def extract_artists(tags: list[str]) -> list[str]:
    """从标签里挑出画师名，保持原顺序去重。"""
    out: list[str] = []
    for t in tags:
        m = _ARTIST.match(t)
        if m:
            name = m.group(1).strip()
            if name and name not in out:
                out.append(name)
    return out


def pick_tags(tags: list[str], limit: int = 12) -> list[str]:
    """挑一组适合发 Pixiv 的标签：画师优先，然后是有信息量的内容标签。"""
    artists = extract_artists(tags)
    rest = []
    for t in tags:
        if _ARTIST.match(t):
            continue
        low = t.lower()
        if low in _NOISE_TAGS:
            continue
        if t not in rest:
            rest.append(t)
    return (artists + rest)[:limit]


def read_nai_meta(path: str | Path) -> dict | None:
    """读一张图的 NovelAI 参数。不是 NovelAI 图就返回 None。"""
    try:
        with Image.open(path) as im:
            info = im.info
            size = im.size
    except Exception:
        return None

    raw = info.get("Comment")
    if not raw:
        return None
    try:
        data = json.loads(raw)
    except Exception:
        return None

    prompt = data.get("prompt") or ""
    uc = data.get("uc") or ""
    tags = split_tags(prompt)
    return {
        "prompt": prompt,
        "uc": uc,
        "tags": tags,
        "artists": extract_artists(tags),
        "seed": data.get("seed"),
        "steps": data.get("steps"),
        "scale": data.get("scale"),
        "sampler": data.get("sampler"),
        "model": data.get("model_name") or info.get("Source"),
        "software": info.get("Software"),
        "width": size[0],
        "height": size[1],
    }


def default_title(path: Path, meta: dict | None) -> str:
    """没指定标题时的默认值：用文件名去掉时间戳尾巴。"""
    stem = path.stem
    # 去掉 _20261017_180849_3654443442 这种生成器时间戳
    stem = re.sub(r"_\d{8}_\d{6}(?:_\d+)?$", "", stem)
    stem = re.sub(r"[_]+", " ", stem).strip()
    return stem or path.stem


# ---------------------------------------------------------------- 去参数

# PNG 里携带文字元数据的块类型。删掉它们不影响一个像素。
_PNG_TEXT_CHUNKS = {b"tEXt", b"iTXt", b"zTXt", b"eXIf"}


def _strip_png(src: Path, dst: Path) -> int:
    """删掉 PNG 的文本块，IDAT 原样搬运（不重新编码，画质无损）。

    返回删掉的块数。
    """
    raw = src.read_bytes()
    if raw[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("不是 PNG")

    out = bytearray(raw[:8])
    pos = 8
    removed = 0
    n = len(raw)
    while pos + 8 <= n:
        length = int.from_bytes(raw[pos:pos + 4], "big")
        ctype = raw[pos + 4:pos + 8]
        end = pos + 12 + length          # 4 len + 4 type + data + 4 crc
        if end > n:
            break
        if ctype in _PNG_TEXT_CHUNKS:
            removed += 1
        else:
            out += raw[pos:end]
        if ctype == b"IEND":
            break
        pos = end

    dst.write_bytes(bytes(out))
    return removed


# JPEG 里携带元数据的段：APP1(EXIF/XMP) / APP2(ICC 之外的) / APP13(IPTC) / COM
_JPEG_DROP_MARKERS = {0xE1, 0xE2, 0xED, 0xFE}


def _strip_jpeg(src: Path, dst: Path) -> int:
    """删掉 JPEG 的 EXIF / XMP / 注释段，图像数据原样保留。"""
    raw = src.read_bytes()
    if raw[:2] != b"\xff\xd8":
        raise ValueError("不是 JPEG")

    out = bytearray(b"\xff\xd8")
    pos = 2
    removed = 0
    n = len(raw)
    while pos + 4 <= n:
        if raw[pos] != 0xFF:
            out += raw[pos:]
            break
        marker = raw[pos + 1]
        # SOS 之后是压缩数据，整段照搬
        if marker == 0xDA:
            out += raw[pos:]
            break
        seglen = int.from_bytes(raw[pos + 2:pos + 4], "big")
        seg_end = pos + 2 + seglen
        if seg_end > n:
            break
        if marker in _JPEG_DROP_MARKERS:
            removed += 1
        else:
            out += raw[pos:seg_end]
        pos = seg_end

    dst.write_bytes(bytes(out))
    return removed


def strip_metadata(src: str | Path, dst: str | Path | None = None) -> tuple[str, int]:
    """把图里的生成参数剥掉，写到 dst（默认同目录加 _clean 后缀）。

    只删元数据块，不动图像数据 —— 画质和原图完全一致。
    返回 (输出路径, 删掉的块数)。
    """
    src = Path(src)
    if dst is None:
        dst = src.with_name(f"{src.stem}_clean{src.suffix}")
    dst = Path(dst)
    dst.parent.mkdir(parents=True, exist_ok=True)

    suffix = src.suffix.lower()
    if suffix == ".png":
        removed = _strip_png(src, dst)
    elif suffix in (".jpg", ".jpeg"):
        removed = _strip_jpeg(src, dst)
    else:
        # webp 等格式暂时原样复制（至少不会带着 NovelAI 的 tEXt）
        dst.write_bytes(src.read_bytes())
        removed = 0
    return str(dst), removed


def has_metadata(path: str | Path) -> bool:
    """快速判断一张图里有没有可被提取的参数。"""
    try:
        with Image.open(path) as im:
            return bool(im.info.get("Comment") or im.info.get("Description"))
    except Exception:
        return False


@dataclass
class Item:
    """一条待上传记录。"""
    src: str                          # 原图绝对路径
    title: str = ""
    tags: list[str] = field(default_factory=list)
    caption: str = ""
    r18: bool = False
    r18g: bool = False
    sexual: bool = False              # Pixiv「性描写」必填项
    private: bool = False             # 公开范围：不公开
    masked: str = ""                  # 打码后的图；空 = 直接用原图
    strip_meta: bool = True           # 上传前剥掉生成参数（默认开）
    status: str = "pending"           # pending / uploaded / skipped
    illust_id: str = ""
    # 存档用的生成参数（不随上传发出，只留底）
    meta: dict = field(default_factory=dict)

    @property
    def source_path(self) -> str:
        """真正要发出去的那张（打码版优先）。"""
        return self.masked or self.src

    @property
    def upload_path(self) -> str:
        """上传用的路径。需要剥参数时会生成一份临时干净文件。"""
        if not self.strip_meta:
            return self.source_path
        p = Path(self.source_path)
        if not has_metadata(p):
            return str(p)             # 本来就没参数，直接用
        clean = p.with_name(f"{p.stem}_clean{p.suffix}")
        if not clean.exists() or clean.stat().st_mtime < p.stat().st_mtime:
            strip_metadata(p, clean)
        return str(clean)

    @property
    def name(self) -> str:
        return Path(self.src).name

    def to_dict(self) -> dict:
        d = asdict(self)
        return d

    @classmethod
    def from_dict(cls, d: dict) -> "Item":
        known = {k: d[k] for k in cls.__dataclass_fields__ if k in d}
        return cls(**known)


class Manifest:
    """一个目录下的上传清单。"""

    def __init__(self, folder: str | Path):
        self.folder = Path(folder).resolve()
        self.path = self.folder / MANIFEST_NAME
        self.items: list[Item] = []

    # ---------- 读写 ----------

    def load(self) -> "Manifest":
        if self.path.exists():
            try:
                data = json.loads(self.path.read_text(encoding="utf-8"))
                self.items = [Item.from_dict(d) for d in data.get("items", [])]
            except Exception:
                self.items = []
        return self

    def save(self) -> None:
        payload = {
            "version": 1,
            "updated": time.strftime("%Y-%m-%d %H:%M:%S"),
            "folder": str(self.folder),
            "items": [i.to_dict() for i in self.items],
        }
        self.path.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8"
        )

    # ---------- 扫描 ----------

    def scan(self) -> tuple[int, int]:
        """扫目录里的图，把还没进清单的加进来。返回 (新增, 总数)。"""
        existing = {str(Path(i.src).resolve()) for i in self.items}
        added = 0
        for p in sorted(self.folder.iterdir()):
            if not p.is_file() or p.suffix.lower() not in IMAGE_EXTS:
                continue
            if p.name == MANIFEST_NAME:
                continue
            rp = str(p.resolve())
            if rp in existing:
                continue
            meta = read_nai_meta(p)
            item = Item(src=rp)
            item.title = default_title(p, meta)
            if meta:
                item.tags = pick_tags(meta["tags"])
                item.meta = {k: v for k, v in meta.items() if k != "tags"}
            self.items.append(item)
            added += 1
        return added, len(self.items)

    def add_files(self, paths: list[str | Path]) -> int:
        """加入指定的几个文件（单个文件模式用）。"""
        existing = {str(Path(i.src).resolve()) for i in self.items}
        added = 0
        for raw in paths:
            p = Path(raw).resolve()
            if not p.is_file() or p.suffix.lower() not in IMAGE_EXTS:
                continue
            if str(p) in existing:
                continue
            meta = read_nai_meta(p)
            item = Item(src=str(p))
            item.title = default_title(p, meta)
            if meta:
                item.tags = pick_tags(meta["tags"])
                item.meta = {k: v for k, v in meta.items() if k != "tags"}
            self.items.append(item)
            added += 1
        return added

    # ---------- 打码后匹配 ----------

    def relink_masked(self) -> int:
        """把目录里新出现的图，按文件名前缀匹配到对应的原图当打码版。

        打码工具常存成 xxx_masked.png / xxx_修.png，所以用前缀匹配。
        """
        known_src = {str(Path(i.src).resolve()) for i in self.items}
        candidates = [
            p for p in sorted(self.folder.iterdir())
            if p.is_file() and p.suffix.lower() in IMAGE_EXTS
            and str(p.resolve()) not in known_src
        ]
        linked = 0
        for item in self.items:
            if item.masked:
                continue
            stem = Path(item.src).stem
            for c in candidates:
                if c.stem.startswith(stem) or stem.startswith(c.stem):
                    item.masked = str(c.resolve())
                    linked += 1
                    break
        return linked

    # ---------- 统计 ----------

    def pending(self) -> list[Item]:
        return [i for i in self.items if i.status == "pending"]

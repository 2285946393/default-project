import struct, os, json, re

PCK = r"D:\software\steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
OUT = r"C:\Users\有花无实\AppData\Local\Temp\sts2loc"
os.makedirs(OUT, exist_ok=True)
FILE_BASE = 112

size = os.path.getsize(PCK)
f = open(PCK, "rb")
needle = b"localization/"
pat = re.compile(rb"^localization/(zhs|eng)/([A-Za-z0-9_]+)\.json")

hits = []
CH = 8 * 1024 * 1024
pos = 0; tail = b""
while pos < size:
    f.seek(pos); d = f.read(CH); hay = tail + d; base = pos - len(tail)
    i = hay.find(needle)
    while i != -1:
        hits.append(base + i); i = hay.find(needle, i + 1)
    tail = hay[-32:]; pos += CH
print("hits:", len(hits))

def u32at(o):
    f.seek(o); return struct.unpack("<I", f.read(4))[0]
def u64at(o):
    f.seek(o); return struct.unpack("<Q", f.read(8))[0]

found = {}
for o in hits:
    pl = u32at(o - 4)
    if pl <= 0 or pl > 120:
        continue
    f.seek(o); raw = f.read(pl)
    m = pat.match(raw)
    if not m:
        continue
    lang, table = m.group(1).decode(), m.group(2).decode()
    off = u64at(o + pl); sz = u64at(o + pl + 8)
    if sz <= 0 or sz > 5_000_000 or off > size:
        continue
    found[(lang, table)] = (off, sz)

print("valid entries:", len(found))
ok = 0; bad = []
for (lang, table), (off, sz) in sorted(found.items()):
    f.seek(FILE_BASE + off); data = f.read(sz)
    try:
        json.loads(data.decode("utf-8"))
        open(os.path.join(OUT, f"{lang}__{table}.json"), "wb").write(data); ok += 1
    except Exception as e:
        bad.append((lang, table, off, sz, str(e)[:60]))
print("extracted json ok:", ok)
for b in bad[:20]:
    print("  BAD:", b)
print("zhs tables:", sorted(t for (l, t) in found if l == "zhs"))
print("eng tables:", sorted(t for (l, t) in found if l == "eng"))

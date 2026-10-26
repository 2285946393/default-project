"""Hades 1 / 2 的 sjson 文本解析器。

sjson 是 Supergiant 的配置格式，长得像 JSON 但：
- 键值之间用换行或空格分隔，不一定有逗号
- 支持 /* */ 注释
- 数组用 [ ]，对象用 { }

对话文件的结构：
    {
      lang = "zh-CN"
      Texts = [
        {
          /*  Event: xxx / 英文原文 */
          Id = "Hades_0088"
          Speaker = "Hades"
          DisplayName = "中文译文"
        }
      ]
    }

注释里是英文原文和事件名 —— 这是白送的平行语料。
"""

import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")


def strip_comments(text):
    """去掉 /* */ 和 // 注释，但把注释内容单独收集起来。"""
    comments = []

    def repl(m):
        comments.append(m.group(0))
        return " "

    text = re.sub(r"/\*.*?\*/", repl, text, flags=re.S)
    text = re.sub(r"//[^\n]*", repl, text)
    return text, comments


TOKEN_RE = re.compile(
    r"""
    (?P<ws>\s+)
  | (?P<num>-?\d+\.\d+(?:[eE][-+]?\d+)?|-?\d+)
  | (?P<str>"(?:[^"\\]|\\.)*")
  | (?P<name>[A-Za-z_$][A-Za-z_0-9$.]*)
  | (?P<punct>[{}\[\]=,;:])
    """,
    re.VERBOSE,
)


def tokenize(text):
    toks = []
    for m in TOKEN_RE.finditer(text):
        kind = m.lastgroup
        if kind == "ws":
            continue
        toks.append((kind, m.group()))
    return toks


class Parser:
    def __init__(self, toks):
        self.t = toks
        self.i = 0

    def peek(self, k=0):
        j = self.i + k
        return self.t[j] if j < len(self.t) else (None, None)

    def next(self):
        v = self.peek()
        self.i += 1
        return v

    def parse_value(self):
        kind, v = self.peek()
        if v == "{":
            return self.parse_object()
        if v == "[":
            return self.parse_array()
        if kind == "str":
            self.next()
            return unescape(v[1:-1])
        if kind == "num":
            self.next()
            return float(v) if ("." in v or "e" in v.lower()) else int(v)
        if kind == "name":
            self.next()
            low = v.lower()
            if low == "true":
                return True
            if low == "false":
                return False
            if low == "null" or low == "nil":
                return None
            return v
        self.next()
        return None

    def parse_object(self):
        self.next()  # {
        obj = {}
        while True:
            kind, v = self.peek()
            if v == "}":
                self.next()
                break
            if v is None:
                break
            if v in (",", ";", ":"):
                self.next()
                continue
            if kind in ("name", "str"):
                key = v if kind == "name" else unescape(v[1:-1])
                self.next()
                if self.peek()[1] == "=":
                    self.next()
                elif self.peek()[1] == ":":
                    self.next()
                obj[key] = self.parse_value()
                continue
            self.next()
        return obj

    def parse_array(self):
        self.next()  # [
        arr = []
        while True:
            v = self.peek()[1]
            if v == "]":
                self.next()
                break
            if v is None:
                break
            if v in (",", ";"):
                self.next()
                continue
            arr.append(self.parse_value())
        return arr


def unescape(s):
    return (
        s.replace("\\n", "\n")
        .replace("\\t", "\t")
        .replace('\\"', '"')
        .replace("\\\\", "\\")
    )


def parse_sjson(text):
    clean, _ = strip_comments(text)
    toks = tokenize(clean)
    p = Parser(toks)
    return p.parse_value()


# ---------- 对话专用 ----------
BLOCK_RE = re.compile(
    r"\{\s*/\*(?P<comment>.*?)\*/\s*"
    r"Id\s*=\s*\"(?P<id>[^\"]*)\"\s*"
    r"(?:Speaker\s*=\s*\"(?P<speaker>[^\"]*)\"\s*)?"
    r"DisplayName\s*=\s*\"(?P<text>(?:[^\"\\]|\\.)*)\"",
    re.S,
)

EVENT_RE = re.compile(r"Event:\s*(\S+)")
# 英文原文形如  Zeus: Why, look, Zagreus...
LINE_RE = re.compile(r"^\s*([A-Za-z_][A-Za-z_0-9]*)\s*:\s*(.+)$", re.M)


def parse_dialogue(path, en_path=None):
    """解析对话文件，返回条目列表。

    两种格式都要兼容：
      H1: 注释里带 Event 和英文原文，字段是 Id/Speaker/DisplayName
      H2: 无注释，字段是 Id/InheritFrom/DisplayName（Speaker 从 Id 前缀推）

    给了 en_path 时，按 Id 把英文原文配进来。
    """
    with open(path, "r", encoding="utf-8-sig", errors="replace") as f:
        raw = f.read()

    # 先试结构化解析（更可靠）
    rows = []
    try:
        data = parse_sjson(raw)
        texts = data.get("Texts") if isinstance(data, dict) else None
        if isinstance(texts, list):
            for item in texts:
                if not isinstance(item, dict):
                    continue
                rid = item.get("Id") or ""
                text = item.get("DisplayName") or item.get("Text") or ""
                if not isinstance(text, str) or not text:
                    continue
                speaker = item.get("Speaker") or ""
                if not speaker and "_" in rid:
                    speaker = rid.split("_")[0]
                rows.append(
                    {
                        "Id": rid,
                        "Speaker": speaker,
                        "Text": text,
                        "Event": item.get("Event") or "",
                        "English": "",
                    }
                )
    except Exception:
        rows = []

    # 补英文原文：先看注释（H1），再用英文文件按 Id 对齐（H2）
    if rows:
        for m in BLOCK_RE.finditer(raw):
            rid = m.group("id")
            comment = m.group("comment") or ""
            english = []
            for lm in LINE_RE.finditer(comment):
                english.append(f"{lm.group(1)}: {lm.group(2).strip()}")
            if not english:
                continue
            for r in rows:
                if r["Id"] == rid and not r["English"]:
                    r["English"] = "\n".join(english)
                    ev = EVENT_RE.search(comment)
                    if ev and not r["Event"]:
                        r["Event"] = ev.group(1)
                    break

        # 仍然缺英文的，去英文版对照
        if en_path and os.path.exists(en_path):
            need = {r["Id"] for r in rows if not r["English"]}
            if need:
                try:
                    with open(en_path, "r", encoding="utf-8-sig", errors="replace") as f:
                        en_raw = f.read()
                    en_data = parse_sjson(en_raw)
                    en_texts = en_data.get("Texts") if isinstance(en_data, dict) else []
                    lookup = {}
                    for item in en_texts or []:
                        if isinstance(item, dict):
                            lookup[item.get("Id")] = item
                    for r in rows:
                        if r["English"]:
                            continue
                        item = lookup.get(r["Id"])
                        if not item:
                            continue
                        en_text = item.get("DisplayName") or item.get("Text") or ""
                        if isinstance(en_text, str) and en_text:
                            r["English"] = f"{r['Speaker']}: {en_text}"
                            if not r["Event"] and item.get("Event"):
                                r["Event"] = item["Event"]
                except Exception:
                    pass

    return rows


def load_sjson(path):
    with open(path, "r", encoding="utf-8-sig", errors="replace") as f:
        return parse_sjson(f.read())

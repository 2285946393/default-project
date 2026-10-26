"""Hades 1 / 2 Lua 数据表解析器。

Supergiant 的脚本是纯文本 Lua，格式非常规整：
    TraitData =
    {
        Key =
        {
            Field = 123,
            Nested = { A = 1 },
        },
    }

本模块把这种表解析成 Python dict，不执行任何 Lua 代码。
"""

import re
import sys

sys.stdout.reconfigure(encoding="utf-8")


# ---------- 词法 ----------
def strip_comment(line):
    """去掉行尾的 -- 注释，但保留字符串里的 --。"""
    out = []
    i = 0
    n = len(line)
    in_str = None
    while i < n:
        c = line[i]
        if in_str:
            out.append(c)
            if c == "\\" and i + 1 < n:
                out.append(line[i + 1])
                i += 2
                continue
            if c == in_str:
                in_str = None
            i += 1
            continue
        if c in "\"'":
            in_str = c
            out.append(c)
            i += 1
            continue
        if c == "-" and i + 1 < n and line[i + 1] == "-":
            break
        out.append(c)
        i += 1
    return "".join(out)


TOKEN_RE = re.compile(
    r"""
    (?P<ws>\s+)
  | (?P<num>-?\d+\.\d+(?:[eE][-+]?\d+)?|-?\d+(?:[eE][-+]?\d+)?)
  | (?P<str>"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*')
  | (?P<name>[A-Za-z_][A-Za-z_0-9]*)
  | (?P<punct>[{}\[\]=,;()])
    """,
    re.VERBOSE,
)


def tokenize(text):
    toks = []
    for line in text.splitlines():
        line = strip_comment(line)
        if not line.strip():
            continue
        for m in TOKEN_RE.finditer(line):
            kind = m.lastgroup
            val = m.group()
            if kind == "ws":
                continue
            toks.append((kind, val))
    return toks


# ---------- 语法 ----------
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

    def expect(self, val):
        kind, v = self.next()
        if v != val:
            raise SyntaxError(f"期望 {val!r}，得到 {v!r} (位置 {self.i})")

    def parse_value(self):
        kind, v = self.peek()
        if v == "{":
            return self.parse_table()
        if kind == "num":
            self.next()
            return float(v) if ("." in v or "e" in v.lower()) else int(v)
        if kind == "str":
            self.next()
            return v[1:-1]
        if kind == "name":
            self.next()
            if v == "true":
                return True
            if v == "false":
                return False
            if v == "nil":
                return None
            return {"__ref__": v}
        if v in ("-",):
            self.next()
            return -self.parse_value()
        self.next()
        return None

    def parse_table(self):
        self.expect("{")
        arr = []
        dct = {}
        while True:
            kind, v = self.peek()
            if v == "}":
                self.next()
                break
            if v is None:
                raise SyntaxError("表未闭合")
            if v in (",", ";"):
                self.next()
                continue

            # 带方括号的键：[1] = ...
            if v == "[":
                self.next()
                key = self.parse_value()
                self.expect("]")
                self.expect("=")
                val = self.parse_value()
                dct[str(key)] = val
                continue

            # name = value
            if kind == "name" and self.peek(1)[1] == "=":
                self.next()
                self.next()
                val = self.parse_value()
                dct[v] = val
                continue

            # 纯数组元素
            val = self.parse_value()
            arr.append(val)

        if dct and arr:
            dct["__array__"] = arr
            return dct
        return dct if dct else arr


def merge_table(dst, src):
    """模拟 OverwriteTableKeys：把 src 的键覆盖进 dst。"""
    if not isinstance(dst, dict) or not isinstance(src, dict):
        return dst
    for k, v in src.items():
        if k == "__array__":
            continue
        if k in dst and isinstance(dst[k], dict) and isinstance(v, dict):
            merge_table(dst[k], v)
        else:
            dst[k] = v
    return dst


def parse_assignments(text):
    """解析顶层赋值，返回 {Name: value}。

    支持三种形式：
        Name = { ... }                       普通赋值
        OverwriteTableKeys(Name, { ... })    Hades 2 的增量覆盖
        AddTableKeys(Name, { ... })          同上
    """
    toks = tokenize(text)
    p = Parser(toks)
    result = {}
    while p.i < len(toks):
        kind, v = p.peek()
        if v is None:
            break

        # 跳过 local 变量声明
        if v == "local":
            p.next()
            while p.i < len(toks) and p.peek()[1] != "=":
                p.next()
            if p.i < len(toks):
                p.next()
                try:
                    p.parse_value()
                except SyntaxError:
                    pass
            continue

        # OverwriteTableKeys(Target, { ... })
        if kind == "name" and v in ("OverwriteTableKeys", "AddTableKeys") and p.peek(1)[1] == "(":
            p.next()  # 函数名
            p.next()  # (
            tgt_kind, tgt = p.next()
            if p.peek()[1] == ",":
                p.next()
            try:
                payload = p.parse_value()
            except SyntaxError:
                continue
            if tgt not in result:
                result[tgt] = {}
            merge_table(result[tgt], payload)
            continue

        # Name = { ... }
        if kind == "name" and p.peek(1)[1] == "=":
            name = v
            p.next()
            p.next()
            try:
                val = p.parse_value()
            except SyntaxError:
                continue
            if name in result and isinstance(result[name], dict) and isinstance(val, dict):
                merge_table(result[name], val)
            else:
                result[name] = val
            continue

        p.next()
    return result


# ---------- 扁平化 ----------
def flatten(obj, prefix="", out=None):
    """把嵌套表压成 {路径: 标量}，方便做表格。"""
    if out is None:
        out = {}
    if isinstance(obj, dict):
        for k, v in obj.items():
            if k == "__array__":
                continue
            p = f"{prefix}.{k}" if prefix else k
            if isinstance(v, (dict, list)):
                flatten(v, p, out)
            else:
                out[p] = v
    elif isinstance(obj, list):
        for idx, v in enumerate(obj, 1):
            p = f"{prefix}[{idx}]"
            if isinstance(v, (dict, list)):
                flatten(v, p, out)
            else:
                out[p] = v
    return out


def load_file(path):
    with open(path, "r", encoding="utf-8-sig", errors="replace") as f:
        return parse_assignments(f.read())

"""临时起个本地 http 服务，用 chrome 无头截几张图（验证本地 HTML 用）。"""
import functools
import http.server
import os
import socketserver
import subprocess
import sys
import tempfile
import threading
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CHROME = r"D:\software\chrome\Google\Chrome\Application\chrome.exe"


def main():
    rel = sys.argv[1]           # 例如 pages/xxx.html
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 8899
    sizes = sys.argv[3].split("|") if len(sys.argv) > 3 else ["1280,2600", "390,1700"]
    scale = sys.argv[4] if len(sys.argv) > 4 else "1"

    handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=ROOT)
    httpd = socketserver.TCPServer(("127.0.0.1", port), handler)
    httpd.allow_reuse_address = True
    threading.Thread(target=httpd.serve_forever, daemon=True).start()
    time.sleep(0.5)

    from urllib.parse import quote
    url = "http://127.0.0.1:%d/%s" % (port, quote(rel))
    print("url:", url)

    tmp = tempfile.mkdtemp(prefix="shot-")
    outs = []
    for i, spec in enumerate(sizes):
        size = spec
        out = os.path.join(tempfile.gettempdir(), "shot_%d.png" % i)
        if os.path.exists(out):
            os.remove(out)
        cmd = [
            CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
            "--no-first-run", "--user-data-dir=" + os.path.join(tmp, "p%d" % i),
            "--force-device-scale-factor=" + scale,
            "--window-size=" + size,
            "--screenshot=" + out, url,
        ]
        p = subprocess.run(cmd, capture_output=True, timeout=120)
        ok = os.path.exists(out)
        print("%s -> %s %s (%d bytes)" % (
            size, out, "OK" if ok else "FAIL",
            os.path.getsize(out) if ok else 0))
        if not ok and p.stderr:
            print(p.stderr.decode("utf-8", "replace")[-500:])
        outs.append(out)

    # 额外 dump 一次真实 DOM，确认脚本有没有跑
    dom = subprocess.run(
        [CHROME, "--headless=new", "--disable-gpu", "--no-first-run",
         "--user-data-dir=" + os.path.join(tmp, "dom"),
         "--virtual-time-budget=5000", "--dump-dom", url],
        capture_output=True, timeout=120,
    )
    text = dom.stdout.decode("utf-8", "replace")
    dump_path = os.path.join(tempfile.gettempdir(), "shot_dom.html")
    with open(dump_path, "w", encoding="utf-8") as f:
        f.write(text)
    for probe in ("bar-fill", "tier", "共 200 款", "无底洞", "尝个味"):
        print("  DOM 含 %-10s : %s" % (probe, probe in text))
    print("dom bytes: %d -> %s" % (len(text), dump_path))

    httpd.shutdown()
    print("\n".join(outs))


if __name__ == "__main__":
    main()

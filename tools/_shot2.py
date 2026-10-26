"""临时：无头 Chrome 截一张本地 HTML（带虚拟时间，让动画跑完）。"""
import functools, http.server, os, socketserver, subprocess, sys, tempfile, threading, time
from urllib.parse import quote

ROOT = r"C:\Users\有花无实\Documents\Default Project"
CHROME = r"D:\software\chrome\Google\Chrome\Application\chrome.exe"

rel = sys.argv[1]
out = sys.argv[2]
size = sys.argv[3] if len(sys.argv) > 3 else "1400,900"
budget = sys.argv[4] if len(sys.argv) > 4 else "5000"
query = sys.argv[5] if len(sys.argv) > 5 else ""
port = 8891

handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=ROOT)
socketserver.TCPServer.allow_reuse_address = True
httpd = socketserver.TCPServer(("127.0.0.1", port), handler)
threading.Thread(target=httpd.serve_forever, daemon=True).start()
time.sleep(0.4)

url = "http://127.0.0.1:%d/%s%s" % (port, quote(rel), query)
tmp = tempfile.mkdtemp(prefix="shot-")
if os.path.exists(out):
    os.remove(out)

cmd = [CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
       "--no-first-run", "--user-data-dir=" + tmp,
       "--force-device-scale-factor=1",
       "--virtual-time-budget=" + budget,
       "--window-size=" + size,
       "--screenshot=" + out, url]
p = subprocess.run(cmd, capture_output=True, timeout=180)
print("url:", url)
print("rc:", p.returncode, "ok:", os.path.exists(out),
      os.path.getsize(out) if os.path.exists(out) else 0)
if not os.path.exists(out) and p.stderr:
    print(p.stderr.decode("utf-8", "replace")[-800:])

# 顺便把页面里的 #err 抓出来
dom = subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--no-first-run",
                      "--user-data-dir=" + tmp + "-d",
                      "--virtual-time-budget=4000", "--dump-dom", url],
                     capture_output=True, timeout=180)
text = dom.stdout.decode("utf-8", "replace")
import re
m = re.search(r'id="err"[^>]*>(.*?)</div>', text, re.S)
print("err:", (m.group(1)[:300] if m else "(空)"))
print("hint:", (re.search(r'id="hint"[^>]*>(.*?)</span>', text, re.S) or [None, '?'])[1])
httpd.shutdown()

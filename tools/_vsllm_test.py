import os
import json, urllib.request, ssl, time

BASE = 'https://vsllm.cc/v1'
KEY = os.environ.get('VSDEEPSEEK_KEY') or os.environ.get('OPENAI_API_KEY', '')
ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

# 挑最有价值的模型实测
MODELS = [
    'claude-opus-4-5-20251101',
    'claude-opus-4-6',
    'claude-opus-4-8',
    'claude-opus-5',
    'claude-sonnet-4-6',
    'claude-sonnet-5',
    'claude-haiku-4-5-20251001',
    'gemini-3.1-pro-preview',
    'gemini-3.5-flash',
    'gemini-3.6-flash-high',
    'deepseek-v4-pro',
    'deepseek-v4.1-flash',
    'qwen3.8-max',
    'qwen3.7-max',
    'gpt-5.2',
    'gpt-5.4',
]

log = []
for m in MODELS:
    body = {'model': m, 'messages': [{'role': 'user', 'content': '只回复两个字：通了'}], 'max_tokens': 500, 'stream': False}
    r = urllib.request.Request(BASE + '/chat/completions',
                              data=json.dumps(body).encode(),
                              headers={'Authorization': 'Bearer ' + KEY,
                                       'Content-Type': 'application/json',
                                       'User-Agent': 'curl/8'})
    t0 = time.time()
    try:
        resp = urllib.request.urlopen(r, timeout=120, context=ctx)
        raw = resp.read().decode('utf-8', 'replace')
        ms = int((time.time() - t0) * 1000)
        if raw.lstrip().startswith('data:'):
            text = ''
            for line in raw.split('\n'):
                line = line.strip()
                if not line.startswith('data:'): continue
                pl = line[5:].strip()
                if pl == '[DONE]': break
                try:
                    j = json.loads(pl)
                    dl = j.get('choices', [{}])[0].get('delta', {})
                    text += dl.get('content') or ''
                except: pass
            log.append('[%s] OK %dms :: %s' % (m, ms, text.strip()[:60] or '(empty)'))
        else:
            d = json.loads(raw)
            if 'error' in d:
                log.append('[%s] ERR %dms :: %s' % (m, ms, json.dumps(d['error'], ensure_ascii=False)[:120]))
            else:
                msg = d.get('choices', [{}])[0].get('message', {})
                log.append('[%s] OK %dms :: %s' % (m, ms, (msg.get('content') or '').strip()[:60]))
    except Exception as e:
        ms = int((time.time() - t0) * 1000)
        log.append('[%s] FAIL %dms :: %s' % (m, ms, repr(e)[:150]))

open(r'C:\Users\有花无实\Documents\Default Project\_vsllm_result.txt', 'w', encoding='utf-8').write('\n'.join(log))
print('done, %d models tested' % len(MODELS))

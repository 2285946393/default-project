/**
 * 直调 WorkBuddy 模型（国内版 / 国际版），复用 dsh-connect-workbuddy 插件的客户端。
 *
 * 为什么这么写：插件的凭据是「桌面端加密」的，只有插件自己的代码解得开
 * （要靠 WORKBUDDY_APP_EXECUTABLE 指向 WorkBuddyAI.exe），所以别自己去读 token 文件。
 *
 * 用法：
 *   node tools/wb.mjs "问题"                       # 默认走国际版(global) 的免费模型
 *   node tools/wb.mjs --region cn "问题"           # 走国内版
 *   node tools/wb.mjs --model deepseek-v4.1-flash "问题"
 *   node tools/wb.mjs --list                       # 列两边的模型和积分
 *
 * ⚠️ 需要国际版/国内版客户端**已经登录**，并且本机能找到 WorkBuddyAI.exe（下面已内置路径）。
 */
import process from 'node:process'

const PLUGIN = 'file:///D:/software/dsh-home/profiles/web/node_modules/dsh-connect-workbuddy/lib/index.js'
process.env.WORKBUDDY_APP_EXECUTABLE ||= 'D:\\software\\WorkBuddy AI\\WorkBuddyAI.exe'

const mod = await import(PLUGIN)
const { WorkBuddyCredentialStore, WorkBuddyUpstreamClient, prepareChatBody } = mod

const argv = process.argv.slice(2)
const opt = { region: 'global', model: 'deepseek-v4.1-flash', list: false, stdin: false }
const parts = []
for (let i = 0; i < argv.length; i++) {
  const a = argv[i]
  if (a === '--region') opt.region = argv[++i]
  else if (a === '--model') opt.model = argv[++i]
  else if (a === '--list') opt.list = true
  else if (a === '--stdin') opt.stdin = true
  else parts.push(a)
}

const store = new WorkBuddyCredentialStore({ region: opt.region })
const client = new WorkBuddyUpstreamClient(opt.region)

if (opt.list) {
  for (const r of ['cn', 'global']) {
    const s = new WorkBuddyCredentialStore({ region: r })
    try {
      const c = await s.current()
      console.log(`\n== ${r} ==`)
      if (!c) { console.log('  未登录'); continue }
      console.log(`  账号 ${c.nickname ?? c.uid}  域 ${c.domain}`)
      const models = await client.fetchModels(c)
      for (const m of models) console.log(`   - ${m.id ?? m.name}  credits=${m.credits ?? '?'}`)
    } catch (e) {
      console.log(`  x ${r}: ${e.message}`)
    }
  }
  process.exit(0)
}

let prompt = parts.join(' ')
if (opt.stdin) {
  const chunks = []
  for await (const c of process.stdin) chunks.push(c)
  prompt = Buffer.concat(chunks).toString('utf8')
}
if (!prompt.trim()) { console.error('没给问题'); process.exit(1) }

const cred = await store.current()
if (!cred) { console.error(`${opt.region} 未登录`); process.exit(2) }

const body = JSON.stringify({
  model: opt.model,
  // ⚠️ 国际版（www.workbuddy.ai）要求**第一条必须是 system**，否则报
  //    `code 11128: first message is not system prompt`。国内版无所谓，都加上最稳。
  messages: [
    { role: 'system', content: '你是乐于助人的中文助手，回答简洁准确。' },
    { role: 'user', content: prompt },
  ],
  stream: true,
})

const res = await client.chatStream(cred, prepareChatBody(body))
if (!res.ok) {
  console.error(`上游拒绝：status=${res.status} kind=${res.kind} ${res.message}`)
  process.exit(3)
}

const reader = res.response.body.getReader()
const dec = new TextDecoder()
let buf = '', out = ''
while (true) {
  const { done, value } = await reader.read()
  if (done) break
  buf += dec.decode(value, { stream: true })
  const lines = buf.split('\n')
  buf = lines.pop()
  for (const line of lines) {
    const s = line.trim()
    if (!s.startsWith('data:')) continue
    const payload = s.slice(5).trim()
    if (payload === '[DONE]') continue
    try {
      const j = JSON.parse(payload)
      const d = j.choices?.[0]?.delta ?? j.choices?.[0]?.message
      if (d?.content) out += d.content
      if (d?.reasoning_content) process.stderr.write(d.reasoning_content)
    } catch { /* 忽略非 JSON 行 */ }
  }
}
console.log(out)

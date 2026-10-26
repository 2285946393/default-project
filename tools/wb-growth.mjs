#!/usr/bin/env node
/**
 * WorkBuddy 国内版「成长中心 / 成长计划」任务工具。
 *
 * 复用 dsh-connect-workbuddy 插件的凭据存储解出国内版 token，直调 www.workbuddy.cn。
 * 接口清单来自前端 bundle：assets/workbuddy.cn/web/usercenter/<hash>/assets/growthSpace-Bhoa8Yzh.js
 *
 * 用法：
 *   node tools/wb-growth.mjs list      # 列出全部任务 / 进度 / 状态
 *   node tools/wb-growth.mjs accept    # 领取所有「未领取」的任务（进度从此开始计算）
 *   node tools/wb-growth.mjs claim     # 领取所有「已完成」任务的奖励
 *   node tools/wb-growth.mjs status    # 顺带看下等级 / 能量 / 积分
 *
 * ⚠️ 只做「领取任务 / 领奖」这类状态流转，**不做**任何伪造完成进度的操作 ——
 *    活动规则第五条明确禁止用脚本刷量，违者取消资格并追回奖励。
 */
import process from 'node:process'

const PLUGIN = 'file:///D:/software/dsh-home/profiles/web/node_modules/dsh-connect-workbuddy/lib/index.js'
process.env.WORKBUDDY_APP_EXECUTABLE ||= 'D:\\software\\WorkBuddy AI\\WorkBuddyAI\\WorkBuddyAI.exe'

const { WorkBuddyCredentialStore } = await import(PLUGIN)
const cred = await new WorkBuddyCredentialStore({ region: 'cn' }).resolve()
const BASE = 'https://www.workbuddy.cn'

async function api(method, path, body) {
  const res = await fetch(BASE + path, {
    method,
    headers: {
      Authorization: `Bearer ${cred.accessToken}`,
      Accept: 'application/json',
      'Content-Type': 'application/json',
      'X-Client-Platform': 'web',
      'X-User-Id': cred.uid,
      'X-Domain': cred.domain,
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  })
  const text = await res.text()
  let json
  try { json = JSON.parse(text) } catch { json = undefined }
  return { http: res.status, json, text }
}

const STATUS_CN = {
  not_accepted: '未领取',
  accepted: '已领取',
  in_progress: '进行中',
  completed: '✅待领奖',
  claimed: '已领奖',
}

async function fetchTasks() {
  const r = await api('GET', '/v2/activity/growth/tasks')
  if (r.http !== 200 || r.json?.code !== 0) throw new Error(`拉取任务失败: HTTP ${r.http} ${r.text?.slice(0, 200)}`)
  return r.json.data.tasks ?? []
}

function render(tasks) {
  console.log('code'.padEnd(30), '状态'.padEnd(10), '进度'.padEnd(7), '积分', ' 标题')
  console.log('-'.repeat(112))
  let sum = 0
  for (const t of tasks) {
    const p = t.progress ? `${t.progress.current}/${t.progress.target}` : '-'
    if (t.accept_status !== 'claimed') sum += t.reward_credit ?? 0
    console.log(
      String(t.task_code).padEnd(30),
      (STATUS_CN[t.accept_status] ?? t.accept_status).padEnd(10),
      p.padEnd(7),
      String(t.reward_credit ?? 0).padStart(4),
      ' ' + t.title,
    )
  }
  console.log('-'.repeat(112))
  console.log(`未到手的积分合计：${sum}`)
}

async function status() {
  const [p, e, c] = await Promise.all([
    api('GET', '/v2/activity/growth/profile'),
    api('GET', '/activity/growth/energy'),
    api('POST', '/billing/meter/get-user-resource', { PageNumber: 1, PageSize: 50 }),
  ])
  const prof = p.json?.data
  if (prof) console.log(`等级  : Lv.${prof.level} ${prof.level_name}  已完成 ${prof.completed}/${prof.total}`)
  const en = e.json?.data
  if (en) console.log(`能量  : 余额 ${en.balance}（累计获得 ${en.total_earned}，已消耗 ${en.total_consumed}）`)
  const packs = c.json?.data?.Response?.Data?.Accounts ?? c.json?.data?.Data?.Accounts ?? c.json?.data?.Accounts ?? []
  if (packs.length) {
    const remain = packs.reduce((s, a) => s + Number(a.CapacityRemain ?? a.CapacityRemainPrecise ?? 0), 0)
    console.log(`积分  : 剩余 ${remain} credits（共 ${packs.length} 个积分包）`)
    for (const a of packs.filter((x) => Number(x.CapacityRemain) > 0)) {
      console.log(`         - ${a.PackageName} 剩 ${a.CapacityRemain}/${a.CapacitySize}  到期 ${a.CycleEndTime || a.ExpiredTime || '-'}`)
    }
  }
}

const cmd = process.argv[2] ?? 'list'

if (cmd === 'list') {
  const tasks = await fetchTasks()
  render(tasks)
  await status()
} else if (cmd === 'accept') {
  const tasks = await fetchTasks()
  const codes = tasks.filter((t) => t.accept_status === 'not_accepted').map((t) => t.task_code)
  if (codes.length === 0) { console.log('没有「未领取」的任务。'); process.exit(0) }
  console.log(`领取 ${codes.length} 个任务：${codes.join(', ')}`)
  const r = await api('POST', '/activity/growth/tasks/accept', { task_codes: codes })
  console.log(`HTTP ${r.http}`, JSON.stringify(r.json ?? r.text).slice(0, 600))
  console.log('\n领取后：')
  render(await fetchTasks())
} else if (cmd === 'claim') {
  const tasks = await fetchTasks()
  const done = tasks.filter((t) => t.accept_status === 'completed')
  if (done.length === 0) { console.log('没有「已完成待领奖」的任务。'); process.exit(0) }
  for (const t of done) {
    const r = await api('POST', `/activity/growth/tasks/${t.task_code}/claim`)
    console.log(`领奖 ${t.task_code} (${t.title}) -> HTTP ${r.http}`, JSON.stringify(r.json ?? r.text).slice(0, 300))
  }
  console.log('\n领奖后：')
  render(await fetchTasks())
} else if (cmd === 'status') {
  await status()
} else {
  console.log('用法：node tools/wb-growth.mjs [list|accept|claim|status]')
  process.exit(1)
}

#!/usr/bin/env node
// 一条命令拉 B 站收藏夹。
//   node tools/bili-fav.mjs                  列出所有收藏夹
//   node tools/bili-fav.mjs --items 默认       列出某个收藏夹里的条目
//   node tools/bili-fav.mjs --items 106382383 --pages 10 --json out.json
//   node tools/bili-fav.mjs --refresh        重新从浏览器取一次登录态
//   node tools/bili-fav.mjs --search "webgl 着色器" --order click --pages 2
//
// 登录态缓存位置：%LOCALAPPDATA%\bili-fav\cookie.txt（故意放在仓库外面，别提交进 git）

import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import crypto from "node:crypto";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { DatabaseSync } from "node:sqlite";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
  "(KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
const CACHE_DIR = path.join(process.env.LOCALAPPDATA || os.homedir(), "bili-fav");
const COOKIE_FILE = path.join(CACHE_DIR, "cookie.txt");

const argv = process.argv.slice(2);
const flag = (name) => argv.includes(name);
const opt = (name, fallback = null) => {
  const i = argv.indexOf(name);
  return i >= 0 && argv[i + 1] ? argv[i + 1] : fallback;
};

const NEEDS_LOGIN =
  "\n没找到能用的 B 站登录态。\n" +
  "先在浏览器里登录 bilibili.com（Chrome 或 Edge 都行），然后重跑：\n" +
  "  node tools/bili-fav.mjs --refresh\n";

function log(...a) {
  console.log(...a);
}

function decryptCookie(hex, key) {
  const buf = Buffer.from(hex, "hex");
  const version = buf.subarray(0, 3).toString("ascii");
  if (version !== "v10" && version !== "v11") return null;
  const nonce = buf.subarray(3, 15);
  const tag = buf.subarray(buf.length - 16);
  const data = buf.subarray(15, buf.length - 16);
  const d = crypto.createDecipheriv("aes-256-gcm", key, nonce);
  d.setAuthTag(tag);
  let plain = Buffer.concat([d.update(data), d.final()]);
  // Chromium 在明文前面塞了 32 字节的 SHA256(host_key) 做域名绑定
  if (plain.length > 32) plain = plain.subarray(32);
  return plain.toString("utf8");
}

function collectCandidates() {
  const tmp = path.join(os.tmpdir(), "bili-fav-snap");
  log("· 正在从浏览器快照里取登录态（需要管理员权限，几秒）");
  const out = execFileSync(
    "powershell",
    ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", path.join(HERE, "bili-fav-extract.ps1"), "-OutDir", tmp],
    { encoding: "utf8" },
  );
  const manifestPath = out.trim().split(/\r?\n/).filter(Boolean).pop();
  // PowerShell 5.1 的 UTF8 会带 BOM，先去掉
  const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8").replace(/^\uFEFF/, ""));

  const candidates = [];
  for (const entry of manifest) {
    if (!entry.key || !fs.existsSync(entry.key) || !fs.existsSync(entry.cookies)) continue;
    const key = Buffer.from(fs.readFileSync(entry.key, "utf8").trim(), "hex");
    if (key.length !== 32) continue;
    let rows = [];
    try {
      const db = new DatabaseSync(entry.cookies, { readOnly: true });
      rows = db
        .prepare(
          "SELECT host_key, name, hex(encrypted_value) AS h FROM cookies " +
            "WHERE host_key LIKE '%bilibili%' AND length(encrypted_value) > 0",
        )
        .all();
      db.close();
    } catch {
      continue;
    }
    const jar = new Map();
    for (const row of rows) {
      let value = null;
      try {
        value = decryptCookie(row.h, key);
      } catch {
        continue;
      }
      if (!value) continue;
      const prev = jar.get(row.name);
      if (!prev || (prev.host !== ".bilibili.com" && row.host_key === ".bilibili.com")) {
        jar.set(row.name, { host: row.host_key, value });
      }
    }
    if (!jar.has("SESSDATA")) continue;
    const score = jar.size + (jar.get("SESSDATA").host === ".bilibili.com" ? 10 : 0);
    candidates.push({
      score,
      browser: entry.browser,
      profile: entry.profile,
      header: [...jar.entries()].map(([k, v]) => `${k}=${v.value}`).join("; "),
    });
  }
  candidates.sort((a, b) => b.score - a.score);
  log(`· 扫到 ${candidates.length} 份 B 站登录态候选：` + candidates.map((c) => `${c.browser}/${c.profile}`).join("、"));
  return candidates;
}

function saveCookie(candidate) {
  fs.mkdirSync(CACHE_DIR, { recursive: true });
  fs.writeFileSync(
    COOKIE_FILE,
    JSON.stringify(
      {
        savedAt: new Date().toISOString(),
        from: `${candidate.browser}/${candidate.profile}`,
        header: candidate.header,
      },
      null,
      1,
    ),
    "utf8",
  );
  log(`· 登录态已缓存（来源 ${candidate.browser}/${candidate.profile}）`);
}

function readCached() {
  if (fs.existsSync(COOKIE_FILE)) {
    try {
      const cached = JSON.parse(fs.readFileSync(COOKIE_FILE, "utf8"));
      if (cached.header) return cached.header;
    } catch {}
  }
  return null;
}

async function api(cookie, url) {
  const res = await fetch(url, {
    headers: {
      "User-Agent": UA,
      Accept: "application/json, text/plain, */*",
      "Accept-Language": "zh-CN,zh;q=0.9",
      Referer: "https://space.bilibili.com/",
      Cookie: cookie,
    },
  });
  return res.json();
}

async function tryLogin(cookie) {
  try {
    const nav = await api(cookie, "https://api.bilibili.com/x/web-interface/nav");
    return nav?.data?.isLogin ? nav.data : null;
  } catch {
    return null;
  }
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function listFolders(cookie, mid) {
  const data = await api(cookie, `https://api.bilibili.com/x/v3/fav/folder/created/list-all?up_mid=${mid}`);
  if (data.code !== 0) throw new Error(`收藏夹列表失败：${data.code} ${data.message}`);
  return data.data?.list || [];
}

// B 站搜索：order = totalrank(综合) / click(播放) / pubdate(最新) / dm(弹幕) / stow(收藏)
const ORDERS = { totalrank: "综合", click: "播放最多", pubdate: "最新发布", dm: "弹幕最多", stow: "收藏最多" };

function stripTags(s) {
  return String(s || "").replace(/<[^>]+>/g, "").replace(/&quot;/g, '"').replace(/&amp;/g, "&");
}

async function searchVideos(cookie, keyword, order, pages, minPlay) {
  const out = [];
  for (let pn = 1; pn <= pages; pn++) {
    const r = await api(
      cookie,
      "https://api.bilibili.com/x/web-interface/search/type?search_type=video" +
        `&keyword=${encodeURIComponent(keyword)}&page=${pn}&order=${order}`,
    );
    if (r.code !== 0) throw new Error(`搜索失败：${r.code} ${r.message}`);
    const list = r.data?.result || [];
    for (const v of list) {
      const play = Number(v.play) || 0;
      if (play < minPlay) continue;
      out.push({
        title: stripTags(v.title),
        up: v.author,
        play,
        danmaku: v.video_review,
        duration: v.duration,
        bvid: v.bvid,
        pubdate: v.pubdate,
        description: stripTags(v.description).slice(0, 120),
      });
    }
    if (list.length < 20) break;
    await sleep(350);
  }
  return out;
}

async function listItems(cookie, mediaId, pages) {
  const items = [];
  for (let pn = 1; pn <= pages; pn++) {
    const r = await api(
      cookie,
      `https://api.bilibili.com/x/v3/fav/resource/list?media_id=${mediaId}&pn=${pn}&ps=20` +
        `&keyword=&order=mtime&type=0&tid=0&platform=web`,
    );
    if (r.code !== 0) throw new Error(`条目拉取失败：${r.code} ${r.message}`);
    const medias = r.data?.medias || [];
    if (!medias.length) break;
    for (const m of medias) {
      items.push({
        title: m.title,
        up: m.upper?.name || "",
        duration: m.duration,
        bvid: m.bvid,
        intro: (m.intro || "").slice(0, 300),
        favTime: m.fav_time,
      });
    }
    if (!r.data?.has_more) break;
    await sleep(300);
  }
  return items;
}

async function main() {
  const refresh = flag("--refresh");
  let cookie = refresh ? null : readCached();
  let who = cookie ? await tryLogin(cookie) : null;
  if (!who && cookie) log("· 缓存的登录态不能用了，重新从浏览器取一次");
  if (!who) {
    for (const candidate of collectCandidates()) {
      const info = await tryLogin(candidate.header);
      if (info) {
        cookie = candidate.header;
        who = info;
        saveCookie(candidate);
        break;
      }
    }
  }
  if (!who) throw new Error(NEEDS_LOGIN);

  const wantItems = opt("--items");
  const wantSearch = opt("--search");
  const pages = Number(opt("--pages", "5"));
  const jsonOut = opt("--json");

  let payload;
  if (wantSearch) {
    const order = opt("--order", "click");
    const minPlay = Number(opt("--min-play", "0"));
    const hits = await searchVideos(cookie, wantSearch, order, pages, minPlay);
    payload = { keyword: wantSearch, order, count: hits.length, items: hits };
    log(`\n搜「${wantSearch}」（${ORDERS[order] || order}）—— ${hits.length} 条：\n`);
    hits.forEach((v, i) => {
      const wan = v.play >= 10000 ? (v.play / 10000).toFixed(1) + "万" : String(v.play);
      log(`  ${String(i + 1).padStart(3)}. [${wan}播放] ${v.title}`);
      log(`       ${v.up} · ${v.duration} · ${v.bvid}`);
    });
    log("");
  } else if (!wantItems) {
    const folders = await listFolders(cookie, who.mid);
    payload = { user: who.uname, mid: who.mid, folders };
    log(`\n${who.uname}（${who.mid}）的收藏夹：`);
    for (const f of folders) log(`  ${String(f.id).padEnd(12)} ${f.title}  （${f.media_count} 条）`);
    log("\n取条目的用法：node tools/bili-fav.mjs --items 默认 --pages 5\n");
  } else {
    const folders = await listFolders(cookie, who.mid);
    const byName =
      folders.find((f) => f.title === wantItems) ||
      folders.find((f) => f.title.startsWith(wantItems)) ||
      folders.find((f) => f.title.includes(wantItems));
    const id = byName ? byName.id : wantItems;
    const title = byName ? byName.title : `media_id ${id}`;
    const items = await listItems(cookie, id, pages);
    payload = { folder: title, mediaId: String(id), count: items.length, items };
    log(`\n${title}（${items.length} 条）：`);
    items.forEach((it, i) => log(`  ${String(i + 1).padStart(3)}. ${it.title}  | ${it.up} | ${it.duration}`));
    log("");
  }

  if (jsonOut) {
    fs.writeFileSync(jsonOut, JSON.stringify(payload, null, 1), "utf8");
    log(`已保存：${jsonOut}`);
  }
}

main().catch((err) => {
  console.error(err.message || err);
  process.exit(1);
});

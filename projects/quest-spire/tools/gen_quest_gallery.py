# -*- coding: utf-8 -*-
"""
生成「尖塔任务图鉴」页面。

用法（在任意目录）：
    python projects/quest-spire/tools/gen_quest_gallery.py

做三件事：
  1. 从 QuestSpireCode/Quests/QuestCatalog.cs 抽出全部任务（名称/条件/目标/金币/奖励类型）
  2. 从 notes/杀戮尖塔2-梗清单.md 匹配每个任务背后的梗；清单里没有的用下面的 MANUAL 兜底
  3. 生成单文件页面 pages/尖塔任务图鉴.html（可搜可筛，可爱风，无外部依赖）

⚠️ 加完新任务后重跑一次即可刷新页面。
⚠️ 新任务如果在梗清单里没有，往 MANUAL 里补一条就显示出来了。
"""
import json, io, os, re

HERE = os.path.dirname(os.path.abspath(__file__))                  # projects/quest-spire/tools
PROJ = os.path.dirname(HERE)                                       # projects/quest-spire
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))     # .../Default Project

CATALOG = os.path.join(PROJ, 'QuestSpireCode', 'Quests', 'QuestCatalog.cs')
MEME_MD = os.path.join(ROOT, 'notes', '杀戮尖塔2-梗清单.md')
OUT_HTML = os.path.join(ROOT, 'pages', '尖塔任务图鉴.html')

# —— 梗清单里没有、或者匹配不到的，在这里手动补 ——
MANUAL = {
    '电子木鱼': '赛博功德：敲一下积一点，敲够五十下也算修行',
    '三和大神': '「干一天玩三天」——在篝火躺着回血，一把铁都不打',
    '退退退': '网络热梗，遇事不决先喊一嗓子把人劝退',
    '摸鱼': '上班/上学的第一生产力',
    '裸装': '什么遗物都不依赖，纯靠本事打',
    '打工人': '打工人的一天从打卡开始',
    '全家桶': '该有的全都要，一样不落',
    # —— 其他游戏致敬系列 ——
    '只狼：弹反': '玩《只狼》的梗——完美弹反，一滴血不掉、盾也不破',
    '我的世界：挖矿': '玩《我的世界》的梗——下矿第一件事是翻箱子',
    '宝可梦：进化': '玩《宝可梦》的梗——打击和防御练满，就该进化了',
    '魔兽世界：附魔': '玩《魔兽世界》的梗——装备要附魔才算成型，牌要消耗掉才算数',
    '我的世界：纯净卡组': '玩《我的世界》的梗——「纯净」= 不装 mod、只留原版货（这里指卡组里几乎只剩打击防御）',
    '文明6：精打细算': '玩《文明6》的梗——每一点费用都要花在刀刃上',
    '泰拉瑞亚：以盾取胜': '玩《泰拉瑞亚》的梗——盾反流才是硬道理',
    '饥荒：硬扛过冬': '玩《饥荒》的梗——熬过第一个冬天就算活下来了',
    '暗黑破坏神：刷装备': '玩《暗黑破坏神》的梗——稀有度越高越值得刷',
    '原神：三分祈愿': '玩《原神》的梗——普通/罕见/稀有各来一份，像十连出货',
    '明日方舟：部署': '玩《明日方舟》的梗——干员摆对位置才叫部署',
    '宝可梦：全队进化': '玩《宝可梦》的梗——不是一只进化，是全队一起进化',
    # —— 尖塔社区自己的大梗 ——
    '宇！宙！冷！漠！': '社区把摄政王这张牌当歌唱（B 站《🎵宇！宙！冷！漠！🎵》172 万播放），主播「菜农来辣」带的头',
    '爪爪爪': '尖塔社区的信仰梗「Claw is Law」——爪击永远是对的',
    '地狱狂徒': '铁甲战士的「打击」流派——卡组塞满打击牌硬顶',
    '一拳超人': '同名动漫梗——一拳解决，不拖第二下',
    '自暴自弃': '和「一身清白」正好相反：干脆破罐破摔，诅咒拉满',
    '骨小妹·不死': '亡灵绑定者的召唤物 Osty，反复爬起来',
    '一步之遥': '都走到第三幕了，就差最后一步',
    # —— 自虐挑战（不是梗，是自己给自己加难度）——
    '血之契约': '自虐挑战：接下后敌人开场随机带一个增益',
    '盛碗虫泛滥': '自虐挑战：接下后每场战斗多一只盛碗虫',
    '怪潮': '自虐挑战：接下后每场战斗多出 2 只怪',
    '精英加餐': '自虐挑战：接下后每场战斗额外多一只精英',
    '大改造': '把卡组整个换血：打击→飞溅、防御→发现，还每场多一只精英',
    '骨头人：灾厄': '用「灾厄」这个减益效果收人头',
    # —— 杂项 ——
    '以撒的结合：道具大亨': '玩《以撒的结合》的梗——道具拿满，横着走',
    '星露谷物语：背包': '玩《星露谷物语》的梗——背包整理强迫症',
    '俄罗斯方块：堆高': '玩《俄罗斯方块》的梗——堆得越高越危险',
    '黑神话：悟空·修炼': '玩《黑神话：悟空》的梗——先炼心，再打怪',
    '我全都要·攻': '「小孩子才做选择，我全都要」——攻击牌也不例外',
    '我全都要·技': '同上，技能牌那一份',
    '我全都要·能': '同上，能力牌那一份',
    '我说叠甲有没有懂的': '「我说 XXXX 有没有懂的」句式梗，这次轮到叠甲流',
    '我说丢药有没有懂的': '同上，轮到扔药水流',
    '我说给药有没有懂的': '同上，轮到发药流',
    '我说重放有没有懂的': '同上，轮到重放流',
    '我说别塞有没有懂的': '同上，这次是劝你别往卡组里乱塞牌',
    '花拳绣腿': '看着花哨，一下打不死人',
    '两袖清风': '口袋里空空如也',
    '堆叠宝石': '宝石越堆越多',
    '诅咒交易': '和恶魔做交易，拿诅咒换强度',
    '轮回': '一圈又一圈，回到原点',
    '能量黑洞': '费用被吸干了',
    '牌堆搬运工': '把牌从这堆搬到那堆',
    '永固': '永久保留，不会消失',
    '皮糙肉厚': '血厚得离谱',
    # —— 基础任务（这些不是梗，是成语 / 俗语 / 流派说法，说明它的出处）——
    '见血封喉': '成语：见血就封喉，出手必致命',
    '铁壁防御': '打不穿的防御，像一堵铁墙',
    '刀剑连鸣': '刀剑碰撞声连成一片，一秒出一百下',
    '行云流水': '成语：打得顺，一气呵成',
    '破釜沉舟': '成语：砸锅沉船，不留退路',
    '稳如磐石': '成语：站着不动，谁也推不动',
    '药剂大师': '江湖人称「药罐子」',
    '凯旋之师': '打一路赢一路的常胜军',
    '精英猎手': '专挑精英下手',
    '屠龙者': '屠龙的人（首领战）',
    '空手出招': '把整只手牌打光才算本事',
    '蓄势待发': '攒着不出手，等一波大的',
    '硬抗到底': '站着挨打，硬撑到底',
    '毫发无伤': '成语：一根头发都没伤到',
    '命悬一线': '成语：血皮求生',
    '淬火修行': '打铁要趁热（篝火升级）',
    '篝火夜话': '篝火边歇脚，顺便聊两句',
    '挥金如土': '成语：钱花得像土一样（商店）',
    '白刃相见': '成语：短兵相接——不掏钱，只掏刀（0 费牌）',
    '重拳出击': '只打大牌',
    '奇珍异宝': '成语：专挑好东西用（稀有牌）',
    '十八般武艺': '成语：什么都会一点（能力牌）',
    '狂暴连击': '一拳接一拳，不停手',
    '守财之路': '攒钱流派，钱就是战斗力',
    '采风向导': '到处逛，什么房间都看一眼',
    '三费齐鸣': '1 费 / 2 费 / 3 费各来一张，凑成三件套',
    '朴实无华': '成语：不整花活，全靠普通牌赢',
    '球球交响': '充能球轮番上阵，像在演奏',
    '整整齐齐': '成语：摆得方方正正（卡组构成）',
    '礼让一手': '先让你一手（精英战第一回合不出牌）',
    '精准收割': '伤害刚刚好，一点不多',
    '问号猎手': '专找 ? 房间',
    '贫下中农': '老词：卡组里全是普通货',
    '只看不买': '进商店纯逛，一分钱不掏',
    '偶数为王': '只收偶数费的牌',
    '闪电战': '打完就走，绝不拖回合',
    '满血见王': '一滴血不掉地走到首领面前',
    '原装出厂': '出厂设置，一点没改过',
    '小伤不理': '小伤不用管，皮糙肉厚',
    '今晚不睡': '通宵打铁，一次都没休息',
    '药罐子': '药水喝个不停',
    '见钱眼开': '成语：见到金币眼睛都亮了',
    '一个都不放过': '该拿的全拿，一个不落',
    '熟能生巧': '成语：打多了自然就会了',
    '玩命': '拿命换强度',
    '手不释卷': '成语：牌不离手',
    '砍价高手': '讲价讲到你服',
    '养生': '早睡早起，血回得比谁都快',
    '稳如老狗': '网络语：稳得一塌糊涂',
    '厚积薄发': '成语：攒够了再一次性爆出来',
    '铁壁': '打不动的墙（单次受伤封顶）',
    '连环拳': '一拳接一拳，攻击牌多打一段',
    # —— 批之十 ——
    '一发入魂': '网络语：一击就中，一次就爽',
    '限时特惠': '促销话术——过期不候',
    '落地成盒': '网络语：刚落地就没了（这里是速通）',
    '极简主义': '越少越好，卡组越薄越强',
    '斗地主附体': '玩《斗地主》的梗——手里全是小牌才最舒服（0 费牌）',
    '羊关通关': '一路弃牌不停手，牌堆清清爽爽',
    '国际纵队': '老词：各国人凑一队（这里是各职业的牌凑一副）',
    '清仓大甩卖': '促销话术——全部下架，一张不留',
    '一身清白': '牌组干干净净，一张诅咒都不带',
    '铁布衫': '金钟罩铁布衫，硬功护体（单次格挡够厚）',
    '三杀': '一回合干掉三个，直接 triple kill',
    '复读机': '同一张牌来回念叨，说到第三遍',
    '破防': '格挡被打穿的那一刻',
    '满手牌': '一手牌攥得满满当当',
}

REW_LABEL = {
    'Gold': '金币', 'Relic': '遗物', 'CardAny': '任意牌', 'CardSkill': '技能牌',
    'CardPower': '能力牌', 'CardRare': '稀有牌', 'CardOtherClass': '别职业牌',
    'Upgrade': '升级卡', 'Transform': '变化卡', 'CardNamed': '指定核心牌', 'Enchant': '附魔',
}

STR = re.compile(r'"((?:[^"\\]|\\.)*)"')


def parse_quests():
    src = io.open(CATALOG, encoding='utf-8').read()
    starts = [m.start() for m in re.finditer(r'new\(\s*QuestKind\.', src)]
    items = []
    for i, s in enumerate(starts):
        e = starts[i + 1] if i + 1 < len(starts) else len(src)
        blk = src[s:e]
        kind = re.match(r'new\(\s*QuestKind\.(\w+)', blk)
        strs = STR.findall(blk)
        if not kind or len(strs) < 2:
            continue
        bare = STR.sub('""', blk)
        nums = re.search(r'""\s*,\s*([^,()"]+?)\s*,\s*([^,()"]+?)\s*(?:,|\))', bare)
        rw = re.search(r'Reward\s*=\s*RewardKind\.(\w+)', blk)
        card = re.search(r'RewardCard\s*=\s*"([^"]*)"', blk)
        ch = re.search(r'OnlyCharacter\s*=\s*typeof\(([\w.]+)\)', blk)
        items.append({
            'kind': kind.group(1), 'title': strs[0], 'desc': ' '.join(strs[1].split()),
            'target': nums.group(1).strip() if nums else '',
            'gold': nums.group(2).strip() if nums else '',
            'reward': rw.group(1) if rw else 'Gold',
            'card': card.group(1) if card else '',
            'char': ch.group(1).split('.')[-1] if ch else '',
        })
    return items


def attach_memes(quests):
    lines = io.open(MEME_MD, encoding='utf-8').read().split('\n') if os.path.exists(MEME_MD) else []
    for q in quests:
        found = ''
        for ln in lines:
            if '|' not in ln or q['title'] not in ln:
                continue
            parts = [p.strip() for p in ln.split('|')]
            if len(parts) >= 4:
                m = re.sub(r'\*\*|`|~~', '', parts[2])
                m = re.sub(r'\[([^\]]*)\]\([^)]*\)', r'\1', m)
                if 6 <= len(m) <= 150:
                    found = m
                    break
        q['meme'] = found or MANUAL.get(q['title'], '')
    return quests


TPL = r'''<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>尖塔任务图鉴 · __TOTAL__ 个任务与它们背后的梗</title>
<style>
  *{box-sizing:border-box;margin:0;padding:0}
  body{
    font-family:-apple-system,BlinkMacSystemFont,"Segoe UI","Microsoft YaHei","PingFang SC",system-ui,sans-serif;
    color:#5b4b63;
    background:linear-gradient(168deg,#fff1f6 0%,#f7f2ff 45%,#eef7ff 100%);
    background-attachment:fixed;
    padding:26px 18px 70px; -webkit-font-smoothing:antialiased;
  }
  .wrap{max-width:1180px;margin:0 auto}
  header{text-align:center;margin-bottom:22px}
  h1{font-size:clamp(21px,4.4vw,31px);letter-spacing:.5px}
  h1 small{display:block;font-size:13px;font-weight:400;color:#9a8aa3;margin-top:9px;letter-spacing:0}
  .stats{display:flex;gap:10px;justify-content:center;flex-wrap:wrap;margin:18px 0 20px}
  .stat{background:rgba(255,255,255,.78);border:1px solid #fff;border-radius:14px;
        padding:9px 15px;font-size:12.5px;color:#8b7a94;box-shadow:0 6px 18px rgba(180,150,190,.13)}
  .stat b{color:#ff7fae;font-size:17px;margin-right:3px}
  .bar{display:flex;gap:8px;justify-content:center;flex-wrap:wrap;margin-bottom:16px}
  .chip{border:1px solid rgba(255,255,255,.9);background:rgba(255,255,255,.7);
        border-radius:999px;padding:7px 15px;font-size:12.5px;color:#7d6c88;cursor:pointer;
        transition:.18s;font-family:inherit}
  .chip:hover{transform:translateY(-1px)}
  .chip.on{background:linear-gradient(120deg,#ffb6d0,#c9b3ff);color:#fff;border-color:transparent;
           box-shadow:0 6px 16px rgba(255,150,190,.34)}
  .search{display:block;margin:0 auto 22px;width:min(100%,420px);padding:11px 17px;
          border-radius:999px;border:1px solid #fff;background:rgba(255,255,255,.85);
          font-size:13.5px;color:#5b4b63;outline:none;font-family:inherit;
          box-shadow:0 6px 18px rgba(180,150,190,.13)}
  .search:focus{box-shadow:0 0 0 3px rgba(255,182,208,.4)}
  .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(268px,1fr));gap:14px}
  .card{background:rgba(255,255,255,.9);border-radius:18px;padding:15px 16px 14px;
        border:1px solid #fff;box-shadow:0 8px 22px rgba(180,150,190,.13);
        transition:transform .18s,box-shadow .18s;position:relative;overflow:hidden}
  .card:hover{transform:translateY(-3px);box-shadow:0 14px 30px rgba(180,150,190,.2)}
  .card.has-meme::before{content:"";position:absolute;left:0;top:0;bottom:0;width:3px;
        background:linear-gradient(#ffb6d0,#c9b3ff)}
  .no{position:absolute;right:12px;top:11px;font-size:11px;color:#c6b8cf}
  .title{font-size:15.5px;font-weight:700;margin-bottom:8px;padding-right:30px;line-height:1.35}
  .meme{font-size:12px;line-height:1.55;color:#a07ab0;background:rgba(255,240,247,.85);
        border-radius:10px;padding:7px 10px;margin-bottom:9px}
  .desc{font-size:13px;line-height:1.6;color:#6b5a74}
  .foot{display:flex;gap:6px;flex-wrap:wrap;margin-top:10px;align-items:center}
  .tag{font-size:11px;padding:3px 9px;border-radius:999px;background:#f4eefb;color:#9b87ad}
  .tag.gold{background:#fff5e0;color:#c69538}
  .tag.special{background:#ffeef5;color:#e07aa8}
  .tag.char{background:#eef4ff;color:#7f9ed4}
  .empty{text-align:center;color:#a99cb2;font-size:13.5px;padding:50px 0}
  footer{margin-top:34px;text-align:center;font-size:12px;color:#a99cb2;line-height:1.9}
  footer b{color:#ff8fb8}
  @media(max-width:520px){
    body{padding:18px 12px 56px}
    .grid{grid-template-columns:1fr}
  }
</style>
</head>
<body>
<div class="wrap">
  <header>
    <h1>🃏 尖塔任务图鉴
      <small>《杀戮尖塔 2》QuestSpire · __TOTAL__ 个任务，和它们背后的社区梗</small>
    </h1>
    <div class="stats">
      <div class="stat"><b>__TOTAL__</b>个任务</div>
      <div class="stat"><b>__HAVE__</b>个带梗解释</div>
      <div class="stat"><b>__SPECIAL__</b>个送特殊奖励</div>
      <div class="stat"><b>__GOLD__</b>个纯送金币</div>
    </div>
    <div class="bar">
      <button class="chip on" data-f="all">全部</button>
      <button class="chip" data-f="meme">有梗的</button>
      <button class="chip" data-f="special">送遗物/卡牌</button>
      <button class="chip" data-f="gold">纯金币</button>
    </div>
    <input class="search" id="q" placeholder="搜任务名 / 条件 / 梗…（比如「鸡煲」「删牌」「篝火」）">
  </header>
  <div class="grid" id="grid"></div>
  <div class="empty" id="empty" style="display:none">没有匹配的任务，换个词试试～</div>
  <footer>
    数据来自 <b>QuestSpire</b> 源码（<code>QuestCatalog.cs</code>，__TOTAL__ 条）<br>
    梗解释来自 <code>notes/杀戮尖塔2-梗清单.md</code> 与社区考据 · 由 <code>tools/gen_quest_gallery.py</code> 生成
  </footer>
</div>
<script>
const QUESTS = __DATA__;
const REW = __REW__;
const grid = document.getElementById('grid');
const emptyEl = document.getElementById('empty');
const qEl = document.getElementById('q');
let filter = 'all';

function esc(s){ return String(s).replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c])); }

function render(){
  const q = qEl.value.trim().toLowerCase();
  let n = 0;
  const out = [];
  for (let i = 0; i < QUESTS.length; i++){
    const it = QUESTS[i];
    if (filter === 'meme' && !it.meme) continue;
    if (filter === 'special' && it.reward === 'Gold') continue;
    if (filter === 'gold' && it.reward !== 'Gold') continue;
    if (q){
      const hay = (it.title + ' ' + it.desc + ' ' + (it.meme||'') + ' ' + it.kind).toLowerCase();
      if (!hay.includes(q)) continue;
    }
    n++;
    const gold = it.gold && it.gold !== '0' ? `<span class="tag gold">金币 +${esc(it.gold)}</span>` : '';
    const rew = it.reward !== 'Gold' ? `<span class="tag special">${esc(REW[it.reward] || it.reward)}</span>` : '';
    const ch = it.char ? `<span class="tag char">${esc(it.char)}</span>` : '';
    const meme = it.meme ? `<div class="meme">💡 ${esc(it.meme)}</div>` : '';
    out.push(`<div class="card${it.meme ? ' has-meme' : ''}">
      <div class="no">#${i + 1}</div>
      <div class="title">${esc(it.title)}</div>
      ${meme}
      <div class="desc">${esc(it.desc)}</div>
      <div class="foot">${gold}${rew}${ch}</div>
    </div>`);
  }
  grid.innerHTML = out.join('');
  emptyEl.style.display = n ? 'none' : 'block';
}

document.querySelectorAll('.chip').forEach(b => b.addEventListener('click', () => {
  document.querySelectorAll('.chip').forEach(x => x.classList.remove('on'));
  b.classList.add('on');
  filter = b.dataset.f;
  render();
}));
qEl.addEventListener('input', render);
render();
</script>
</body>
</html>'''


def main():
    quests = attach_memes(parse_quests())
    have = sum(1 for q in quests if q['meme'])
    special = sum(1 for q in quests if q['reward'] != 'Gold')

    out = (TPL.replace('__DATA__', json.dumps(quests, ensure_ascii=False))
              .replace('__REW__', json.dumps(REW_LABEL, ensure_ascii=False))
              .replace('__TOTAL__', str(len(quests)))
              .replace('__HAVE__', str(have))
              .replace('__SPECIAL__', str(special))
              .replace('__GOLD__', str(len(quests) - special)))
    io.open(OUT_HTML, 'w', encoding='utf-8').write(out)

    print('任务 %d 条，带梗解释 %d 条，特殊奖励 %d 条' % (len(quests), have, special))
    print('已写入：' + OUT_HTML)
    lack = [q['title'] for q in quests if not q['meme']]
    if lack:
        print('还没有梗解释的 %d 条（往 MANUAL 里补即可）：' % len(lack))
        print('  ' + ' / '.join(lack))


if __name__ == '__main__':
    main()

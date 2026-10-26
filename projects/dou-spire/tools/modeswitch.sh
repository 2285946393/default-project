#!/usr/bin/env bash
# 在「开发版 DouSpireDev」和「工坊版 DouSpire」之间一键切换 ——
# 解决的是这个反复踩的坑：
#
#   ① 每次 `build-mod.sh` / `build-workshop.sh` 编译，`CopyModOnBuild` 都会把
#      对应身份的那份 dll **重新写回 `mods\<ModName>\`** ⇒ 手动停用的动作会失效。
#   ② 两版同时在 `mods\` 里 ⇒ 主菜单两个入口、F9 抢热键、牌桌重复注册，
#      **测出来的结果不可信**（你根本不知道是哪一版在跑）。
#   ③ 游戏模组界面里的那个开关，开发版默认是**关**的；不开就测不到任何新改动。
#
# 这个脚本把「谁在场」这件事变成一条命令：
#
#   bash tools/modeswitch.sh dev        # 只留开发版（测新改动用这个）
#   bash tools/modeswitch.sh shop       # 只留工坊版（联机 / 上架后用这个）
#   bash tools/modeswitch.sh status     # 只看现在谁在场（**只读，不动任何文件**）
#
# 实现：把不在场的那一份**整个挪出 `mods\`** 到 `_disabled_mods\`。
#   ⚠️ 必须整个挪出去，不能在 `mods\` 里面改文件夹名 ——
#      `ModManager.ReadModsInDirRecursive` 是**递归**扫描的，放子目录照样会被加载。
set -uo pipefail

GAME="D:/software/steam/steamapps/common/Slay the Spire 2"
MODS="$GAME/mods"
PARK="$GAME/_disabled_mods"

CMD="${1:-status}"

say() { printf '%s\n' "$*"; }

# 现在场上有哪些（只看目录在不在 —— 不读 json，避免半截文件误判）
present() {
  [ -d "$MODS/DouSpireDev" ] && echo dev
  [ -d "$MODS/DouSpire" ] && echo shop
}

do_status() {
  say "游戏 mods 目录：$MODS"
  say ""
  for pair in "DouSpireDev:开发版" "DouSpire:工坊版"; do
    dir="${pair%%:*}"; label="${pair##*:}"
    if [ -d "$MODS/$dir" ]; then
      dll="$MODS/$dir/$dir.dll"
      if [ -f "$dll" ]; then
        sz=$(stat -c %s "$dll" 2>/dev/null || echo "?")
        mt=$(stat -c %y "$dll" 2>/dev/null | cut -d. -f1)
        ver=$(grep -o '"version"[^,]*' "$MODS/$dir/$dir.json" 2>/dev/null | head -1 | sed 's/.*: *//;s/"//g')
        say "  [在场] $dir（$label）  v$ver  $sz 字节  $mt"
      else
        say "  [在场] $dir（$label）  —— 但没有 $dir.dll（半截目录？）"
      fi
    else
      say "  [不在] $dir（$label）"
    fi
  done

  n=$(present | wc -l)
  say ""
  case "$n" in
    1) say "⇒ 只有一份在场，可以放心测。" ;;
    0) say "⇒ **一份都没有** —— 游戏里看不到斗地主。跑一次 dev 或 shop。" ;;
    *) say "⇒ ⚠️ **两版同时在场** —— 会两个入口 + 抢热键，测出来的结果不可信！" ;;
  esac

  if [ -d "$PARK" ]; then
    say ""
    say "已停用区（_disabled_mods）："
    ls -1 "$PARK" 2>/dev/null | sed 's/^/  /' || true
  fi
}

# 把 $1 挪到停用区；$2 = 停用区里的名字
park_it() {
  local dir="$1" stamp="$2"
  [ -d "$MODS/$dir" ] || return 0
  [ -d "$PARK" ] || mkdir -p "$PARK"
  local dest="$PARK/${dir}@${stamp}"
  # 同名已存在就加序号，绝不覆盖（可能正是你想要的历史快照）
  local i=1
  while [ -e "$dest" ]; do dest="$PARK/${dir}@${stamp}-$i"; i=$((i+1)); done
  mv "$MODS/$dir" "$dest" && say "  已停用：$dir → _disabled_mods/$(basename "$dest")"
}

# 把停用区里**最新的一份** $1 请回 mods/
unpark_it() {
  local dir="$1"
  [ -d "$MODS/$dir" ] && { say "  $dir 已经在场，不动。"; return 0; }
  [ -d "$PARK" ] || { say "  停用区里没有 $dir。"; return 1; }
  local latest
  latest="$(ls -1dt "$PARK/$dir"@* 2>/dev/null | head -1)"
  [ -n "$latest" ] || { say "  停用区里没有 $dir 的快照。"; return 1; }
  mv "$latest" "$MODS/$dir" && say "  已启用：$(basename "$latest") → mods/$dir"
}

ask_good() {
  local which="$1"
  [ -d "$MODS/$which" ] || { say ""; say "你还没把改动 build 进 $which —— 先：" ; say "  bash tools/build-mod.sh dou-spire${2:-}"; }
}

stamp="$(date +%m%d-%H%M)"

case "$CMD" in
  status|"")
    do_status
    ;;
  dev)
    say "── 切到【开发版】DouSpireDev（工坊版挪走）"
    park_it DouSpire "$stamp"
    unpark_it DouSpireDev || true
    say ""
    do_status
    say ""
    say "下一步：进游戏 → 模组界面确认「斗地主尖塔（开发版）」是**开**的 → F9 开桌。"
    say "⚠️ 改完源码记得先 build，再进游戏（旧 dll 还在内存里，不重启更新不了）。"
    ;;
  shop)
    say "── 切到【工坊版】DouSpire（开发版挪走）"
    park_it DouSpireDev "$stamp"
    # ★ 2026-10-10 修：不能假设工坊版已在场 —— 打包完它常常正躺在停用区
    #   （我自己刚才就踩了：切 shop 后**两份都不在场**）。这里主动请回来。
    unpark_it DouSpire || say "  （停用区也没有 DouSpire —— 用 build-workshop.sh 编一份）"
    say ""
    say "  工坊版由 build-workshop.sh 编译部署："
    say "    bash tools/build-workshop.sh          # 编译 + 部署到 mods\\DouSpire"
    say "    bash tools/build-workshop.sh --pack   # 再打包工坊工作区"
    say "    bash tools/build-workshop.sh --upload-only   # 只上传（工作区已打好包）"
    do_status
    say ""
    say "⚠️ 联机请**两端都用工坊版**（开发版连不上工坊版玩家 —— opcode 命名空间跟着 id 走）。"
    ;;
  *)
    say "用法："
    say "  bash tools/modeswitch.sh status   # 只看现在谁在场（只读）"
    say "  bash tools/modeswitch.sh dev      # 只留开发版"
    say "  bash tools/modeswitch.sh shop     # 只留工坊版"
    exit 1
    ;;
esac

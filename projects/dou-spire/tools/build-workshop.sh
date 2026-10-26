#!/usr/bin/env bash
# 按**工坊身份**编一次 DouSpire（id=DouSpire / DouSpire.dll / mods\DouSpire）。
#
# 为什么需要它：2026-10-09 起本地开发版改名成 DouSpireDev（见 DouSpire.csproj 里那段注释），
# 默认 `build-mod.sh dou-spire` 编出来的是**开发版**。要上架必须先按工坊身份编一次，
# 否则 prepare_workshop.py 的身份闸会拦住（那是故意的）。
#
# 用法：
#   bash tools/build-workshop.sh                # 只编译 + 部署到 mods\DouSpire
#   bash tools/build-workshop.sh --pack         # 编译 + 打包工坊工作区（**不上传**）
#   bash tools/build-workshop.sh --upload       # 编译 + 打包 + 上传工坊
#   bash tools/build-workshop.sh --upload-only  # **只上传**（工作区已打好包时用这个）
#
# ⚠️ 2026-10-10 教训：`--upload` 一整套跑完**会超时**（编译 ~10s、打包 ~10s、上传看网络
#    几十秒~几分钟）。网络一波动，上传会**卡在 `Initializing Steam`** 不动。
#    ⇒ 所以拆成三个档：打包归打包、上传归上传。**上传失败不用重新编译 / 重新打包**，
#      等网络好了直接 `--upload-only`。
set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJ="$(cd "$HERE/.." && pwd)"
WORKSPACE="D:/software/sts2-mod-uploader/DouSpire"
UPLOADER_DIR="D:/software/sts2-mod-uploader"

# ⚠️ 必须用**装了 Pillow** 的那个 python。
#    PATH 上的 `python` 是 WorkBuddy 的托管版 3.13.12，**没有 Pillow** ⇒
#    prepare_workshop.py 的图标处理会直接抛 ModuleNotFoundError。
#    本机装了 Pillow 的是 D:/software/Python/python.exe（2026-10-09 实测）。
PY="D:/software/Python/python.exe"
[ -x "$PY" ] || PY="python"

do_upload() {
  echo
  echo "── 上传（工作区 = $WORKSPACE）"
  [ -d "$WORKSPACE" ] || { echo "!! 工作区不存在，先跑 --pack"; exit 1; }
  cd "$UPLOADER_DIR" || exit 1
  ./ModUploader.exe upload -w "$WORKSPACE"
}

case "${1:-}" in
  --upload-only)
    do_upload
    exit $?
    ;;
esac

echo "── 按工坊身份编译（ModName=DouSpire）"
bash "$PROJ/../dou-spire-rules/tools/build-mod.sh" "$PROJ" -p:ModName=DouSpire || exit 1

if [ "${1:-}" = "--pack" ] || [ "${1:-}" = "--upload" ]; then
  echo
  echo "── 打包工坊工作区（python = $PY）"
  "$PY" "$PROJ/tools/prepare_workshop.py" || exit 1
fi

if [ "${1:-}" = "--upload" ]; then
  do_upload
fi

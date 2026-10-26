import fs from "node:fs";

const ws = JSON.parse(fs.readFileSync("D:/software/sts2-mod-uploader/DouSpire/workshop.json", "utf8"));
const descBytes = Buffer.byteLength(ws.description, "utf8");

const out = `# 斗地主尖塔 · 创意工坊说明文（最终版 2026-10-07 晚）

> ⚠️ **标题上限 128 字节**（UTF-8，一个汉字 3 字节，emoji 4 字节）。原 183 字节标题被 Steam 拒
> （k_EResultInvalidParam），本版 125 字节压线过。说明文上限 8000 字节，本版 ${descBytes} 字节。
> **本文与上传工作区 \`D:\\software\\sts2-mod-uploader\\DouSpire\\workshop.json\` 保持同步**，改说明文两处都改。
> 待办：瘦身版（148MB）+ 本文的「🤝」「🎙️」两段还没重传成功，见 \`projects/dou-spire/交接-20261007-晚-工坊上架-ZCode.md\`。

---

## 一、标题（125 字节，已上线）

${ws.title}

---

## 二、说明正文（BBCode，复制粘贴用）

\`\`\`
${ws.description}
\`\`\`

---

## 三、备注

- 加粗一律用 \`[b]…[/b]\`，Steam 不认 markdown 的 \`**…**\`。
- 上传器只支持一张封面图（image.png）；游戏截图要上工坊页得在 Steam 客户端里手动补。
- 标签：${ws.tags.join(" / ")}
- 依赖：RitsuLib 3747602295 + BaseLib 3737335127
`;

fs.writeFileSync(
  "C:/Users/有花无实/Documents/Default Project/notes/斗地主尖塔-创意工坊说明文.md",
  out
);
console.log("说明文笔记已同步，字节:", Buffer.byteLength(out, "utf8"));

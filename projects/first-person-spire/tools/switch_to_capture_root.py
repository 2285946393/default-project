# -*- coding: utf-8 -*-
"""把捕获方式改成「干净容器」：抓图时只抓怪物本身，背景/UI 一律不进来。

## 为什么要改

原来的做法靠 VisibilityLayer 隔离：给怪物的 Visuals 设层，再把祖先链一路 `|=` 同一层。
但祖先（一直到 room）也在这一层之后，**它底下别的分支只要碰巧带这层，就会被一起抓进去**
—— 用户实测「手牌看不见 / 抓到的东西太多」。

## 新做法

另建一个只属于本模组的干净容器 `FirstPersonCaptureRoot`：
- 它挂在展示层里，祖先链只有我们自己的节点（很短、很干净）
- 把怪物的 `Visuals` 临时挂进去，给它和它的子孙设捕获层
- 这样捕获视口只认这一层，**背景和 UI 根本进不来**
- 拍完把 `Visuals` 挂回原父节点

因为容器是我们自己的，**不需要去动 room 的层**，之前那些「污染 room」的连锁问题全部消失。
"""
import io
import os
import re

PROJ = r"C:\Users\有花无实\Documents\Default Project\projects\first-person-spire"
TARGET = os.path.join(PROJ, "PresentationSession.cs")

source = io.open(TARGET, encoding="utf-8").read()

# 1) 加字段：捕获容器 + 记录 Visuals 的原父节点
source = source.replace(
    "    private readonly List<SubViewport> _captures = new();",
    "    private readonly List<SubViewport> _captures = new();\n"
    "    // 只属于本模组的干净捕获容器：把怪物的 Visuals 临时挂进来，\n"
    "    // 这样捕获视口只认这一层，背景和 UI 根本进不来。\n"
    "    private Node2D? _captureRoot;\n"
    "    // 展示对象的 Visuals → 它原来的父节点，拍完要挂回去。\n"
    "    private readonly Dictionary<Node, Node> _originalParents = new();", 1)

# 2) Start() 里创建容器
source = source.replace(
    "        _stage = new SubViewport\n",
    "        _captureRoot = new Node2D { Name = \"FirstPersonCaptureRoot\" };\n"
    "        owner.AddChild(_captureRoot);\n"
    "        _stage = new SubViewport\n", 1)

io.open(TARGET, "w", encoding="utf-8").write(source)
print("已加捕获容器与字段")

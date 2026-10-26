# -*- coding: utf-8 -*-
"""把 PresentationSession 里丢失的方法补回去。

背景：用 Python 切文件时切多了，把 12 个方法删掉了。反编译 DLL 恢复了逻辑，
但反编译产物带编译器生成的匿名类，编译不过，所以按逻辑手写回干净版本。
"""
import io
import os

PROJ = r"C:\Users\有花无实\Documents\Default Project\projects\first-person-spire"
TARGET = os.path.join(PROJ, "PresentationSession.cs")

MISSING = r'''
    private void SyncEnemies(NCreature[] live)
    {
        foreach (var entry in _enemies.Where(e => !live.Contains(e.Creature)).ToArray())
        {
            if (Valid(entry.Creature)) Isolate(entry.Creature.Visuals, _hiddenLayer);
            entry.Sprite.Dispose();
            entry.Capture.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            entry.Capture.QueueFree();
            _captures.Remove(entry.Capture);
            _enemies.Remove(entry);
            _companionSlots.Remove(entry.Creature);
        }
        foreach (var creature in live.Where(c => !_enemies.Any(e => e.Creature == c)))
        {
            var layer = FrontLayout.AvailableLayer(_enemyLayers, _enemies.Select(e => e.Layer));
            // 极端情况下层位不足，额外生物保留本体显示，不关闭第一人称。
            if (layer == 0) continue;
            if (creature.GetCanvas() != _source!.FindWorld2D().Canvas) continue;
            RememberAnchor(creature);
            var capture = new SubViewport
            {
                World2D = _source!.FindWorld2D(), Disable3D = true,
                TransparentBg = true, GuiDisableInput = true, CanvasCullMask = layer,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            _captures.Add(capture);
            owner.AddChild(capture);
            var sprite = new ExtrudedSprite(_stage!, capture.GetTexture());
            sprite.Refresh(capture, allowRebuild: true);
            _enemies.Add(new EnemyView(creature, layer, capture, sprite));
        }
    }

    private void RememberAnchor(NCreature creature)
    {
        // 固定身体参考点，不把动画过程中变化的点击框中心当作角色的站位。
        _anchors.TryAdd(creature, creature.GetGlobalTransform().AffineInverse() *
            (creature.Hitbox.GetGlobalTransform() * (creature.Hitbox.Size * 0.5f)));
    }

    private Vector2 CanvasAnchor(NCreature creature) =>
        creature.GetGlobalTransformWithCanvas() * _anchors[creature];

    private static Vector2[] Corners(Rect2 rect) => [rect.Position,
        rect.Position + new Vector2(rect.Size.X, 0), rect.End,
        rect.Position + new Vector2(0, rect.Size.Y)];

    private UiProjection Projection(Control control)
    {
        if (!_ui.TryGetValue(control, out var projection))
            _ui.Add(control, projection = new UiProjection(control));
        return projection;
    }

    private void ShiftUi(NCreature creature, Vector2 shift)
    {
        // 自己人（含宠物）的血条已经固定在状态栏，不能再跟着 3D 位置平移，
        // 否则会一边停在栏里、一边又被挪走。
        bool docked = !creature.Entity.IsEnemy && _selfStatusRows.ContainsKey(creature);
        foreach (var control in new[] { creature.IntentContainer,
            docked ? null : creature.GetNodeOrNull<Control>("%HealthBar"), creature.OrbManager })
            if (Valid(control)) Projection(control!).Shift(shift);
        if (LocalContext.IsMe(creature.Entity))
        {
            Projection(creature.Hitbox).Shift(shift);
            var reticle = creature.GetNodeOrNull<Control>("%SelectionReticle");
            if (Valid(reticle)) Projection(reticle!).Shift(shift);
        }
    }

    private void SplitBackground()
    {
        if (!Valid(room.Background)) return;
        Isolate(room.Background, _backgroundLayer);
        var layers = room.Background.GetChildren()
            .Where(n => n.Name.ToString().StartsWith("Layer_", StringComparison.Ordinal)).ToArray();
        foreach (var node in layers.Skip(Math.Max(1, (layers.Length + 1) / 2))
            .Concat(room.Background.GetChildren().Where(n => n.Name == "Foreground")))
            if (node is CanvasItem item) Isolate(item, _foregroundLayer);
    }

    private Vector2 CompanionTarget(NCreature creature)
    {
        if (!_companionSlots.TryGetValue(creature, out var slot))
        {
            slot = Enumerable.Range(0, _enemyLayers.Length)
                .First(i => !_companionSlots.ContainsValue(i));
            _companionSlots.Add(creature, slot);
        }
        var (x, y) = FrontLayout.CompanionSlot(slot, _stage!.Size.X, _stage.Size.Y);
        return new Vector2(x, y);
    }

    private void DockSelfVfx(NCreature ally, Vector2 barTopLeft)
    {
        foreach (var item in _selfVfx.ToArray())
        {
            if (!GodotObject.IsInstanceValid(item)) { _selfVfx.Remove(item); continue; }
            if (!_vfxCreature.TryGetValue(item, out var ownerCreature) || ownerCreature != ally) continue;
            var target = new Vector2(barTopLeft.X - 6f, barTopLeft.Y - 34f);
            if (item is Control control)
            {
                var parent = control.GetGlobalTransformWithCanvas() *
                    control.GetTransform().AffineInverse();
                control.Position = parent.AffineInverse() * target;
            }
            else if (item is Node2D node2d) node2d.GlobalPosition = target;
        }
    }

    private void DockSelfStatusBars()
    {
        foreach (var ally in room.CreatureNodes.Where(Valid).Where(c => !c.Entity.IsEnemy))
        {
            if (!_selfStatusRows.TryGetValue(ally, out int row))
            {
                row = _selfStatusRows.Count;
                _selfStatusRows.Add(ally, row);
            }
            var (x, y) = FrontLayout.SelfStatusSlot(row, _stage!.Size.X, _stage.Size.Y);
            DockSelfVfx(ally, new Vector2(x, y));
            foreach (var bar in Descendants(ally).OfType<NHealthBar>())
            {
                if (!Valid(bar)) continue;
                _visibility.TryAdd(bar.HpBarContainer, bar.HpBarContainer.Visible);
                bar.HpBarContainer.Visible = true;
                var label = bar.GetNodeOrNull<CanvasItem>("%HpLabel");
                if (Valid(label))
                {
                    _visibility.TryAdd(label!, label!.Visible);
                    label!.Visible = true;
                }
                Projection(bar.HpBarContainer).MoveTo(new Vector2(x, y));
            }
        }
    }

    private void HideHpLabel(NHealthBar bar)
    {
        var label = bar.GetNodeOrNull<CanvasItem>("%HpLabel");
        if (!Valid(label)) return;
        _visibility.TryAdd(label!, label!.Visible);
        label.Visible = false;
    }

    private void Resize()
    {
        var size = _source!.GetVisibleRect().Size;
        _stage!.Size = new Vector2I(Math.Max(2, (int)size.X), Math.Max(2, (int)size.Y));
        _screen!.Size = size;
        _overlay!.GlobalTransform = _overlay.GetCanvasTransform().AffineInverse();
    }

    private void AdoptCombatVfx()
    {
        foreach (var container in new[] { room.CombatVfxContainer, room.BackCombatVfxContainer })
        {
            if (!Valid(container)) continue;
            foreach (var child in container!.GetChildren())
            {
                if (child is not CanvasItem item) continue;
                if (_vfxOwner.ContainsKey(item)) continue;
                var ownerView = NearestOwner(item);
                if (ownerView == null) continue;
                Isolate(item, ownerView.Layer);
                _vfxOwner[item] = ownerView.Layer;
                _vfxCreature[item] = ownerView.Creature;
                if (!ownerView.Creature.Entity.IsEnemy) _selfVfx.Add(item);
            }
        }
    }

    private EnemyView? NearestOwner(CanvasItem item)
    {
        if (_enemies.Count == 0) return null;
        var origin = item.GetGlobalTransformWithCanvas().Origin;
        EnemyView? best = null;
        float bestDistance = float.MaxValue;
        foreach (var entry in _enemies)
        {
            if (!Valid(entry.Creature)) continue;
            float distance = DistanceToCreature(entry.Creature, origin);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = entry;
        }
        return best;
    }

    private float DistanceToCreature(NCreature creature, Vector2 point)
    {
        var display = DisplayBounds(creature);
        if (display is { } d)
        {
            float dx = Math.Max(0f, Math.Max(d.X - point.X, point.X - (d.X + d.W)));
            float dy = Math.Max(0f, Math.Max(d.Y - point.Y, point.Y - (d.Y + d.H)));
            return MathF.Sqrt(dx * dx + dy * dy);
        }
        var body = creature.Visuals.GetCurrentBody();
        return Valid(body) ? point.DistanceTo(body.GlobalPosition) : float.MaxValue;
    }

    private static bool Valid(GodotObject? node) => node != null && GodotObject.IsInstanceValid(node);

    private static IEnumerable<Node> Descendants(Node root)
    {
        yield return root;
        foreach (var child in root.GetChildren())
            foreach (var nested in Descendants(child)) yield return nested;
    }

'''

ANCHOR = "    private static void Restore(Action action)"

source = io.open(TARGET, encoding="utf-8").read()
if "private void SyncEnemies" in source:
    print("SyncEnemies 已存在，无需补写")
else:
    assert ANCHOR in source, "找不到锚点"
    source = source.replace(ANCHOR, MISSING + ANCHOR, 1)
    io.open(TARGET, "w", encoding="utf-8").write(source)
    print("已补写丢失的方法")

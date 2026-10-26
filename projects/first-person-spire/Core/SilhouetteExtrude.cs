namespace FirstPersonSpire.Core;

// 轮廓挤出：把一张平面精灵变成「有厚度的立体」。
//
// 做法参考自星露谷 3DVR（GingasVR）反编译源码里的 VoxelMesh.Build：
// 逐像素扫描，在不透明的边缘处竖起侧壁，再加上前后两个完整面。
// 结果是一块「饼干模具压出来」的立体 —— 任何角度看都有厚度，
// 不会像单片平面那样转到 90° 就变成一条线。
//
// 本类不依赖 Godot，只算顶点数据，方便离线检查。
// 坐标系：精灵左下角为原点，x 向右、y 向上、z 为挤出方向。
// UV 已换算到 0..1，可直接配精灵区域使用。
public static class SilhouetteExtrude
{
    // 判定不透明的阈值，与参考实现一致。
    public const int AlphaThreshold = 48;

    public readonly record struct Vertex(float X, float Y, float Z, float U, float V);

    public readonly record struct Quad(Vertex A, Vertex B, Vertex C, Vertex D);

    public static List<Quad> Build(Func<int, int, bool> opaque, int width, int height, float depth)
    {
        if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));

        var quads = new List<Quad>();
        float px(int x) => (float)x / width;      // 像素 → 0..1
        float py(int y) => (float)y / height;

        // 前后两个完整面。背面用同一张贴图（参考实现也这么做，所以转头不会空白）。
        quads.Add(new Quad(new Vertex(0, 0, 0, 0, 1), new Vertex(1, 0, 0, 1, 1),
                           new Vertex(1, 1, 0, 1, 0), new Vertex(0, 1, 0, 0, 0)));
        quads.Add(new Quad(new Vertex(1, 0, depth, 1, 1), new Vertex(0, 0, depth, 0, 1),
                           new Vertex(0, 1, depth, 0, 0), new Vertex(1, 1, depth, 1, 0)));

        // 左右两侧：某像素不透明、而横向邻居透明 → 在该处竖起侧壁。
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!opaque(x, y)) continue;
                float y0 = py(y), y1 = py(y + 1), v0 = 1f - py(y), v1 = 1f - py(y + 1);
                if (!opaque(x + 1, y))   // 右边缘
                {
                    float u = px(x + 1);
                    quads.Add(new Quad(new Vertex(u, y0, 0, u, v0), new Vertex(u, y0, depth, u, v0),
                                       new Vertex(u, y1, depth, u, v1), new Vertex(u, y1, 0, u, v1)));
                }
                if (!opaque(x - 1, y))   // 左边缘
                {
                    float u = px(x);
                    quads.Add(new Quad(new Vertex(u, y1, 0, u, v1), new Vertex(u, y1, depth, u, v1),
                                       new Vertex(u, y0, depth, u, v0), new Vertex(u, y0, 0, u, v0)));
                }
            }
        }

        // 上下两侧：同理。
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!opaque(x, y)) continue;
                float x0 = px(x), x1 = px(x + 1), u0 = px(x), u1 = px(x + 1);
                if (!opaque(x, y + 1))   // 上边缘
                {
                    float v = 1f - py(y + 1);
                    quads.Add(new Quad(new Vertex(x0, 1f - v, 0, u0, v), new Vertex(x1, 1f - v, 0, u1, v),
                                       new Vertex(x1, 1f - v, depth, u1, v), new Vertex(x0, 1f - v, depth, u0, v)));
                }
                if (!opaque(x, y - 1))   // 下边缘
                {
                    float v = 1f - py(y);
                    quads.Add(new Quad(new Vertex(x1, 1f - v, 0, u1, v), new Vertex(x0, 1f - v, 0, u0, v),
                                       new Vertex(x0, 1f - v, depth, u0, v), new Vertex(x1, 1f - v, depth, u1, v)));
                }
            }
        }

        return quads;
    }

    // 从像素的 alpha 数组构造取值器。
    public static Func<int, int, bool> FromAlpha(byte[] alpha, int width, int height)
    {
        if (alpha.Length != width * height)
            throw new ArgumentException("alpha 长度与尺寸不符", nameof(alpha));
        return (x, y) => x >= 0 && y >= 0 && x < width && y < height
            && alpha[y * width + x] >= AlphaThreshold;
    }
}

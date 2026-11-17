using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class MapRender
{
    // Réduit splat3 (8192²) en une image OUT² : fond forêt, routes du splat (rouge = asphalte, vert = gravier).
    public static Bitmap Roads(string splatPath, int OUT)
    {
        var src = new Bitmap(splatPath);
        int N = src.Width, k = N / OUT;
        var sd = src.LockBits(new Rectangle(0, 0, N, N), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] s = new byte[sd.Stride * N];
        Marshal.Copy(sd.Scan0, s, 0, s.Length);
        src.UnlockBits(sd);
        int stride = sd.Stride;
        src.Dispose();

        var dst = new Bitmap(OUT, OUT, PixelFormat.Format32bppArgb);
        var dd = dst.LockBits(new Rectangle(0, 0, OUT, OUT), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        byte[] d = new byte[dd.Stride * OUT];
        for (int y = 0; y < OUT; y++)
            for (int x = 0; x < OUT; x++)
            {
                int asphalt = 0, gravel = 0;
                for (int j = 0; j < k; j++)
                    for (int i = 0; i < k; i++)
                    {
                        int o = (y * k + j) * stride + (x * k + i) * 4;   // BGRA
                        if (s[o + 2] > 100) asphalt++;
                        else if (s[o + 1] > 100) gravel++;
                    }
                byte r = 34, g = 52, b = 34;   // forêt
                if (asphalt > 0) { r = 70; g = 70; b = 70; }
                else if (gravel > 0) { r = 105; g = 90; b = 65; }
                int p = y * dd.Stride + x * 4;
                d[p] = b; d[p + 1] = g; d[p + 2] = r; d[p + 3] = 255;
            }
        Marshal.Copy(d, 0, dd.Scan0, d.Length);
        dst.UnlockBits(dd);
        return dst;
    }
}

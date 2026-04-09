namespace CefClient.Viewport
{
    using System;
    using System.Linq;

    public sealed class iPhoneDeviceProfileResult
    {
        public int PhysicalWidth { get; set; }
        public int PhysicalHeight { get; set; }

        public int CssWidth { get; set; }
        public int CssHeight { get; set; }

        public float DeviceScaleFactor { get; set; }

        public double DprX { get; set; }
        public double DprY { get; set; }

        public double Score { get; set; }

        public override string ToString()
        {
            return $"Physical={PhysicalWidth}x{PhysicalHeight}, CSS={CssWidth}x{CssHeight}, DPR={DeviceScaleFactor:F3}, Score={Score:F6}";
        }
    }

    public static class iPhoneViewportMatcher
    {
        // 常见 iPhone CSS 档位（Safari / Chromium 移动模拟常见视口）
        private static readonly (int CssW, int CssH)[] Profiles =
        {
            // 6/7/8/SE2/SE3
            (375, 667),

            // X/XS/11 Pro
            (375, 812),

            // XR/11
            (414, 896),

            // 12 mini / 13 mini
            (360, 780),

            // 12/12 Pro/13/13 Pro/14
            (390, 844),

            // 14 Pro
            (393, 852),

            // 12 Pro Max / 13 Pro Max / 14 Plus
            (428, 926),

            // 14 Pro Max / 15 Plus / 15 Pro Max / 16 Plus/Pro Max 常见档
            (430, 932),

            // 15 / 15 Pro / 16 / 16 Pro 常见档
            (393, 852),
            (402, 874)
        };

        // iPhone 常见 DPR
        private static readonly float[] CommonDprs =
        {
            2.0f,
            3.0f
        };

        public static iPhoneDeviceProfileResult Match(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException("分辨率必须大于 0");

            // 统一按竖屏计算
            int physicalWidth = Math.Min(width, height);
            int physicalHeight = Math.Max(width, height);

            iPhoneDeviceProfileResult? best = null;

            foreach (var p in Profiles)
            {
                double dprX = (double)physicalWidth / p.CssW;
                double dprY = (double)physicalHeight / p.CssH;
                double avgDpr = (dprX + dprY) / 2.0;

                // iPhone 基本就在 2~3 附近，给一点浮动空间
                if (avgDpr < 1.8 || avgDpr > 3.2)
                    continue;

                // 主分数1：横纵 DPR 越接近越好
                double score = Math.Abs(dprX - dprY);

                // 主分数2：偏向 iPhone 常见宽度
                score += GetWidthPenalty(p.CssW);

                // 主分数3：偏向 2x / 3x
                score += GetDprPenalty(avgDpr);

                // 主分数4：宽高比越接近越好
                double physicalRatio = (double)physicalHeight / physicalWidth;
                double cssRatio = (double)p.CssH / p.CssW;
                score += Math.Abs(physicalRatio - cssRatio) * 0.1;

                var result = new iPhoneDeviceProfileResult
                {
                    PhysicalWidth = physicalWidth,
                    PhysicalHeight = physicalHeight,
                    CssWidth = p.CssW,
                    CssHeight = p.CssH,
                    DeviceScaleFactor = NormalizeDpr((float)avgDpr),
                    DprX = dprX,
                    DprY = dprY,
                    Score = score
                };

                if (best == null || result.Score < best.Score)
                    best = result;
            }

            if (best == null)
            {
                // 兜底：优先按 iPhone 最常见 390 宽估算
                float dpr = (float)physicalWidth / 390f;
                dpr = NormalizeDpr(dpr);
                int cssHeight = (int)Math.Round(physicalHeight / dpr);

                return new iPhoneDeviceProfileResult
                {
                    PhysicalWidth = physicalWidth,
                    PhysicalHeight = physicalHeight,
                    CssWidth = 390,
                    CssHeight = cssHeight,
                    DeviceScaleFactor = dpr,
                    DprX = dpr,
                    DprY = dpr,
                    Score = 999
                };
            }

            return best;
        }

        private static double GetWidthPenalty(int cssWidth)
        {
            return cssWidth switch
            {
                390 => 0.0000,
                393 => 0.0005,
                430 => 0.0010,
                428 => 0.0012,
                414 => 0.0015,
                402 => 0.0018,
                375 => 0.0020,
                360 => 0.0025,
                _ => 0.0050
            };
        }

        private static double GetDprPenalty(double dpr)
        {
            double nearestDistance = CommonDprs.Min(x => Math.Abs(x - dpr));
            return nearestDistance * 0.02;
        }

        private static float NormalizeDpr(float dpr)
        {
            foreach (var common in CommonDprs)
            {
                if (Math.Abs(dpr - common) <= 0.12f)
                    return common;
            }

            return (float)Math.Round(dpr, 3, MidpointRounding.AwayFromZero);
        }
    }
}
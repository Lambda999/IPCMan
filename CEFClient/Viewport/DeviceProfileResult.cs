using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Viewport
{
    public sealed class DeviceProfileResult
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
}

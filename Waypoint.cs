using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    internal class Waypoint
    {
        public ushort Index { get; set; }
        public float[] Position { get; set; } = new float[3];
        public List<Waypoint> Paths { get; set; } = new();
        public bool Crouch { get; set; } = false;
        public bool Jump { get; set; } = false;
        public bool Ladder { get; set; } = false;
        public bool Interest { get; set; } = false;
        public bool Item { get; set; } = false;
    }
}

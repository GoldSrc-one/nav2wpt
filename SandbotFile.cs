using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    internal class SandbotFile : WaypointFile
    {
        public override string Extension => ".wpt";
        protected override string Magic => "Sandbot";
        protected override int Version => 1;

        protected override void WriteWaypoint(Waypoint waypoint)
        {
            ulong flags = 0;
            if (waypoint.Crouch)
                flags |= (1 << 3); //CROUCH
            if (waypoint.Jump)
                flags |= (1 << 18); //JUMP
            if (waypoint.Ladder)
                flags |= (1 << 4); //LADDER
            if (waypoint.Interest)
                flags |= (1 << 7); //HEALTH
            if (waypoint.Item)
                flags |= (1 << 7); //HEALTH
            Bytes.AddRange(BitConverter.GetBytes((ulong)flags));
            foreach (var axis in waypoint.Position)
                Bytes.AddRange(BitConverter.GetBytes(axis));
            Bytes.AddRange(BitConverter.GetBytes((uint)0)); //alignment
        }
    }
}

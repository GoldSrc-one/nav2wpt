using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    internal class FoxbotFile : WaypointFile
    {
        public override string Extension => ".fwp";
        protected override string Magic => "FoXBot";
        protected override int Version => 5;

        protected override void WriteWaypoint(Waypoint waypoint)
        {
            uint flags = 0;
            if (waypoint.Crouch)
                flags |= (1 << 3); //CROUCH
            if (waypoint.Jump)
                flags |= (1 << 15); //JUMP
            if (waypoint.Ladder)
                flags |= (1 << 4); //LADDER
            if (waypoint.Interest)
                flags |= (1 << 29); //DEFEND
            if (waypoint.Item)
                flags |= (1 << 7) | (1 << 8) | (1 << 9); //HEALTH+ARMOR+AMMO
            Bytes.AddRange(BitConverter.GetBytes((uint)flags));
            Bytes.Add((byte)0); //script_flags
            Bytes.AddRange(Enumerable.Repeat((byte)0, 3)); //alignment
            foreach (var axis in waypoint.Position)
                Bytes.AddRange(BitConverter.GetBytes(axis));
        }
    }
}

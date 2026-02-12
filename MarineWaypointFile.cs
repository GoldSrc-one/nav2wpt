using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    internal class MarineWaypointFile : WaypointFile
    {
        public override string Extension => ".wpt";
        protected override string Magic => "FAM_bot";
        protected override uint Version => 8;

        protected override void WriteHeader(string magic, uint version, uint flags, uint numWaypoints)
        {
            base.WriteHeader(magic, version, flags, numWaypoints);
            WriteFixedString(Author, 32);
            WriteFixedString("", 32);
        }

        protected override void WriteWaypoint(Waypoint waypoint)
        {
            int flags = 0;
            if (!waypoint.Crouch)
                flags |= (1 << 0); //STD
            if (waypoint.Crouch)
                flags |= (1 << 1); //CROUCH
            //if (waypoint.Jump)
            //    flags |= (1 << 3); //JUMP
            if (waypoint.Ladder)
                flags |= (1 << 10); //LADDER
            if (waypoint.Item)
                flags |= (1 << 6); //AMMOBOX

            Bytes.AddRange(BitConverter.GetBytes(flags));
            Bytes.AddRange(BitConverter.GetBytes((int)5));
            Bytes.AddRange(BitConverter.GetBytes((float)0.0f));
            Bytes.AddRange(BitConverter.GetBytes((int)5));
            Bytes.AddRange(BitConverter.GetBytes((float)0.0f));
            Bytes.AddRange(BitConverter.GetBytes((int)5));
            Bytes.AddRange(BitConverter.GetBytes((int)5));
            Bytes.AddRange(BitConverter.GetBytes((int)0));
            Bytes.AddRange(BitConverter.GetBytes((int)0));
            float radius = MathF.Max(12.5f, 0.5f * MathF.Min(waypoint.Size[0], waypoint.Size[1]));
            Bytes.AddRange(BitConverter.GetBytes(radius));
            foreach (var axis in waypoint.Position)
                Bytes.AddRange(BitConverter.GetBytes(axis));
        }

        protected override void WriteFooter()
        {
            Bytes.AddRange(Enumerable.Repeat((byte)0, 2080));
            WritePaths();
        }

        public override string AuthorFromFile(string filename)
        {
            var authorBytes = new byte[32];
            using var wf = File.OpenRead(filename);
            wf.Position = 52;
            wf.Read(authorBytes);
            var author = Encoding.ASCII.GetString(authorBytes.TakeWhile(b => b != 0).ToArray());
            return author;
        }
    }
}

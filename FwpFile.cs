using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    internal class FwpFile
    {
        IReadOnlyList<Waypoint> _waypoints;
        public FwpFile(IReadOnlyList<Waypoint> waypoints) {
            _waypoints = waypoints;
        }

        public void Write(string filePath)
        {
            var bytes = new List<byte>();
            var magic = Encoding.ASCII.GetBytes("FoXBot");
            bytes.AddRange(magic);
            bytes.AddRange(Enumerable.Repeat((byte)0, 8 - magic.Length));
            bytes.AddRange(BitConverter.GetBytes((uint)5));
            bytes.AddRange(BitConverter.GetBytes((uint)0));
            bytes.AddRange(BitConverter.GetBytes((uint)_waypoints.Count));
            var mapName = Encoding.ASCII.GetBytes(Path.GetFileNameWithoutExtension(filePath));
            bytes.AddRange(mapName);
            bytes.AddRange(Enumerable.Repeat((byte)0, 32 - mapName.Length));

            foreach (var waypoint in _waypoints)
            {
                uint flags = 0;
                if (waypoint.Interest)
                    flags |= (1 << 29); //DEFEND
                if (waypoint.Item)
                    flags |= (1 << 7) | (1 << 8) | (1 << 9); //HEALTH+ARMOR+AMMO
                bytes.AddRange(BitConverter.GetBytes((uint)flags));
                bytes.Add((byte)0); //script_flags
                bytes.AddRange(Enumerable.Repeat((byte)0, 3)); //alignment
                foreach (var axis in waypoint.Position)
                    bytes.AddRange(BitConverter.GetBytes(axis));
            }

            foreach (var waypoint in _waypoints)
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)waypoint.Paths.Count));
                foreach (var neighbor in waypoint.Paths)
                    bytes.AddRange(BitConverter.GetBytes(neighbor.Index));
            }

            var author = Encoding.ASCII.GetBytes(Author);
            bytes.AddRange(author);
            bytes.AddRange(Enumerable.Repeat((byte)0, 255 - author.Length));

            File.WriteAllBytes(filePath, bytes.ToArray());
        }

        public static string Author = $"{nameof(nav2wpt)} v{Assembly.GetExecutingAssembly().GetName().Version}";
    }
}

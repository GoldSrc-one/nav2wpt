using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    internal abstract class WaypointFile
    {
        public abstract string Extension { get; }
        protected abstract string Magic { get; }
        protected abstract int Version { get; }

        protected abstract void WriteWaypoint(Waypoint waypoint);


        private List<byte> _bytes = new();
        protected List<byte> Bytes => _bytes;
        private string _filePath = "";
        private IReadOnlyList<Waypoint> _waypoints = [];

        public void Write(string filePath, IReadOnlyList<Waypoint> waypoints)
        {
            _filePath = filePath;
            _waypoints = waypoints;
            WriteHeader(Magic, Version);

            foreach (var waypoint in _waypoints)
                WriteWaypoint(waypoint);

            WritePaths();

            var author = Encoding.ASCII.GetBytes(Author);
            _bytes.AddRange(author);
            _bytes.AddRange(Enumerable.Repeat((byte)0, 255 - author.Length));

            File.WriteAllBytes(_filePath, _bytes.ToArray());
        }

        protected void WriteHeader(string magic, int version)
        {
            var magicBytes = Encoding.ASCII.GetBytes(magic);
            _bytes.AddRange(magicBytes);
            _bytes.AddRange(Enumerable.Repeat((byte)0, 8 - magicBytes.Length));
            _bytes.AddRange(BitConverter.GetBytes((uint)version));
            _bytes.AddRange(BitConverter.GetBytes((uint)0));
            _bytes.AddRange(BitConverter.GetBytes((uint)_waypoints.Count));
            var mapName = Encoding.ASCII.GetBytes(Path.GetFileNameWithoutExtension(_filePath));
            _bytes.AddRange(mapName);
            _bytes.AddRange(Enumerable.Repeat((byte)0, 32 - mapName.Length));
        }

        protected void WritePaths()
        {
            foreach (var waypoint in _waypoints)
            {
                _bytes.AddRange(BitConverter.GetBytes((ushort)waypoint.Paths.Count));
                foreach (var neighbor in waypoint.Paths)
                    _bytes.AddRange(BitConverter.GetBytes(neighbor.Index));
            }
        }

        public static string Author = $"{nameof(nav2wpt)} v{Assembly.GetExecutingAssembly().GetName().Version}";
    }
}

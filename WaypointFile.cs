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
        protected abstract uint Version { get; }

        protected abstract void WriteWaypoint(Waypoint waypoint);


        private List<byte> _bytes = new();
        protected List<byte> Bytes => _bytes;
        private string _filePath = "";
        private IReadOnlyList<Waypoint> _waypoints = [];
        protected IReadOnlyList<Waypoint> Waypoints => _waypoints;

        protected void WriteFixedString(string text, int length)
        {
            var textBytes = Encoding.UTF8.GetBytes(text);
            _bytes.AddRange(textBytes);
            _bytes.AddRange(Enumerable.Repeat((byte)0, length - textBytes.Length));
        }

        public void Write(string filePath, IReadOnlyList<Waypoint> waypoints)
        {
            _filePath = filePath;
            _waypoints = waypoints;
            WriteHeader(Magic, Version, 0, (uint)_waypoints.Count);

            foreach (var waypoint in _waypoints)
                WriteWaypoint(waypoint);

            WriteFooter();

            File.WriteAllBytes(_filePath, _bytes.ToArray());
        }

        protected virtual void WriteHeader(string magic, uint version, uint flags, uint numWaypoints)
        {
            WriteFixedString(magic, 8);
            _bytes.AddRange(BitConverter.GetBytes(version));
            _bytes.AddRange(BitConverter.GetBytes(flags));
            _bytes.AddRange(BitConverter.GetBytes(numWaypoints));
            WriteFixedString(Path.GetFileNameWithoutExtension(_filePath), 32);
        }

        protected virtual void WriteFooter()
        {
            WritePaths();
            WriteFixedString(Author, 255);
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

        public virtual string AuthorFromFile(string filename)
        {
            var authorBytes = new byte[255];
            using var wf = File.OpenRead(filename);
            wf.Position = wf.Length - authorBytes.Length;
            wf.Read(authorBytes);
            var author = Encoding.ASCII.GetString(authorBytes.TakeWhile(b => b != 0).ToArray());
            return author;
        }
    }
}

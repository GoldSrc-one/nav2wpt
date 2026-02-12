using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    internal class MarinePathFile : WaypointFile
    {
        public override string Extension => ".pth";
        protected override string Magic => "FAM_bot";
        protected override uint Version => 8;

        private List<Waypoint[]> _paths = new();

        static Waypoint? FindFurthestWaypoint(List<Waypoint> waypoints, float[] position)
        {
            float furthestDistance = float.PositiveInfinity;
            Waypoint? furthestWaypoint = null;
            foreach (var waypoint in waypoints)
            {
                float distance = 0;
                for (int iAxis = 0; iAxis < 3; iAxis++)
                {
                    var d = waypoint.Position[iAxis] - position[iAxis];
                    distance += d * d;
                }
                if (distance < furthestDistance)
                    furthestWaypoint = waypoint;
            }
            return furthestWaypoint;
        }

        Waypoint[]? GetPath(Waypoint from, Waypoint to)
        {
            if (from == to)
                return null;

            var links = new Dictionary<Waypoint, Waypoint>();
            var openWaypoints = new Queue<Waypoint>();

            void EnqueueNeighbors(Waypoint waypoint)
            {
                foreach (var neighbor in waypoint.Paths.Where(n => n.Paths.Contains(waypoint)))
                {
                    if (links.ContainsKey(neighbor))
                        continue;

                    links[neighbor] = waypoint;
                    openWaypoints.Enqueue(neighbor);
                }
            }
            EnqueueNeighbors(from);

            while (openWaypoints.Count > 0)
            {
                var waypoint = openWaypoints.Dequeue();

                if (waypoint == to)
                {
                    var path = new List<Waypoint>();
                    path.Add(to);
                    path.Add(waypoint);
                    while (waypoint != from)
                    {
                        waypoint = links[waypoint];
                        path.Add(waypoint);
                    }
                    path.Reverse();
                    return path.ToArray();
                }

                EnqueueNeighbors(waypoint);
            }

            return null;
        }

        void ComputePaths()
        {
            _paths.Clear();

            float[] center = new float[3];
            foreach (var waypoint in Waypoints)
                for (int iAxis = 0; iAxis < 3; iAxis++)
                    center[iAxis] += waypoint.Position[iAxis] / Waypoints.Count;

            var freshWaypoints = Waypoints.ToList();

            while (_paths.Count < 511)
            {
                var from = FindFurthestWaypoint(freshWaypoints, center);
                if (from == null)
                    break;

                freshWaypoints.Remove(from);
                var to = FindFurthestWaypoint(freshWaypoints, from.Position);
                if (to == null)
                    break;

                freshWaypoints.Remove(to);

                var path = GetPath(from, to);
                if (path == null)
                    continue;

                foreach (var waypoint in path)
                    freshWaypoints.Remove(waypoint);

                _paths.Add(path);
            }
        }

        protected override void WriteHeader(string magic, uint version, uint flags, uint numWaypoints)
        {
            //ComputePaths();

            base.WriteHeader(magic, version, numWaypoints, (uint)_paths.Count);
            WriteFixedString(Author, 32);
            WriteFixedString("", 32);
        }

        protected override void WriteWaypoint(Waypoint waypoint)
        {
        }

        protected override void WriteFooter()
        {
            for (int iPath = 0; iPath < _paths.Count; iPath++)
            {
                Bytes.AddRange(BitConverter.GetBytes((int)iPath));
                var path = _paths[iPath];
                Bytes.AddRange(BitConverter.GetBytes((int)path.Length));
                Bytes.AddRange(BitConverter.GetBytes((int)0));
                foreach(var waypoint in path)
                    Bytes.AddRange(BitConverter.GetBytes((int)waypoint.Index));
            }
        }

        public override string AuthorFromFile(string filename)
        {
            return new MarineWaypointFile().AuthorFromFile(filename);
        }
    }
}

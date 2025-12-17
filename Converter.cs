using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    static class Converter
    {
        public static IReadOnlyList<Waypoint> GetWaypoints(string navPath, string bspPath)
        {
            var navFile = new NavFile(navPath);

            var waypoints = new List<Waypoint>();
            var usedAreas = new List<NavArea>();
            var jumpAreas = new Dictionary<uint, NavArea>();
            foreach (var navArea in navFile.NavAreas)
            {
                if ((navArea.Attributes & 2) == 2)
                {
                    jumpAreas.Add(navArea.Id, navArea);
                    continue;
                }

                var waypoint = new Waypoint();
                waypoint.Index = (ushort)waypoints.Count;
                waypoint.Position = navArea.Center;
                if ((navArea.Attributes & 1) == 1)
                    waypoint.Crouch = true;

                waypoint.Position[2] += waypoint.Crouch ? 18 : 36;

                waypoints.Add(waypoint);
                usedAreas.Add(navArea);
            }

            IReadOnlyList<int> GetNavAreaConnectionIndices(NavArea navArea)
            {
                var indices = new List<int>();
                foreach (var areaId in navArea.Connections)
                {
                    var areaIndex = usedAreas.FindIndex(a => a.Id == areaId);
                    if (areaIndex != -1)
                        indices.Add(areaIndex);
                }
                return indices;
            }

            for (int iWaypoint = 0; iWaypoint < waypoints.Count; iWaypoint++)
            {
                var navArea = usedAreas[iWaypoint];
                foreach (var connection in GetNavAreaConnectionIndices(navArea))
                    waypoints[iWaypoint].Paths.Add(waypoints[connection]);

                foreach (var areaId in navArea.Connections)
                {
                    if (!jumpAreas.TryGetValue(areaId, out NavArea? jumpArea))
                        continue;

                    waypoints[iWaypoint].Jump = true;
                    foreach (var connection in GetNavAreaConnectionIndices(jumpArea).Where(index => index != iWaypoint))
                        waypoints[iWaypoint].Paths.Add(waypoints[connection]);
                }
            }

            Waypoint GetClosestWaypoint(IEnumerable<Waypoint> waypoints, float[] position)
            {
                Waypoint closestWaypoint = waypoints.First();
                float closestDistance = float.PositiveInfinity;
                foreach (var waypoint in waypoints)
                {
                    float distance = 0;
                    for (int iAxis = 0; iAxis < 3; iAxis++)
                    {
                        float d = position[iAxis] - waypoint.Position[iAxis];
                        distance += d * d;
                    }
                    if (distance >= closestDistance)
                        continue;

                    closestDistance = distance;
                    closestWaypoint = waypoint;
                }
                return closestWaypoint;
            }

            var bspFile = new BspFile(bspPath);

            Entity? rescuePoint = null; //if there is no rescue point, pick a spawn
            if (bspFile.Entities.Any(entity => entity.Classname == "hostage_entity") && !bspFile.Entities.Any(entity => entity.Classname == "func_hostage_rescue" || entity.Classname == "info_hostage_rescue"))
            {
                var spawns = bspFile.Entities.Where(entity => entity.Classname == "info_player_start").ToArray();
                var center = new float[3];
                for (int iAxis = 0; iAxis < 3; iAxis++)
                    center[iAxis] = spawns.Average(entity => entity.Origin[iAxis]);

                rescuePoint = spawns.MinBy(entity =>
                {
                    float dist = 0;
                    for (int iAxis = 0; iAxis < 3; iAxis++)
                    {
                        float d = entity.Origin[iAxis] - center[iAxis];
                        dist += d * d;
                    }
                    return dist;
                });
            }

            var interests = new HashSet<string>() { "func_bomb_target", "info_bomb_target", "hostage_entity", "func_hostage_rescue", "info_hostage_rescue", "func_vip_safetyzone", "func_escapezone" };
            var items = new HashSet<string>() { "item_", "weapon_", "ammo_", "armoury_entity" };
            var baseWaypoints = waypoints.ToArray();
            foreach (var entity in bspFile.Entities)
            {
                bool interest = interests.Contains(entity.Classname) || entity == rescuePoint;
                bool item = items.Any(entity.Classname.StartsWith);
                if (!interest && !item)
                    continue;

                var center = new float[3];
                for (int iAxis = 0; iAxis < 3; iAxis++)
                    center[iAxis] = entity.Origin[iAxis] + (entity.Extents[iAxis] + entity.Extents[iAxis + 3]) / 2;

                var closestWaypoint = GetClosestWaypoint(baseWaypoints, center);

                const float maxZDist = 64;
                if (MathF.Abs(center[2] - closestWaypoint!.Position[2]) > maxZDist)
                    continue;

                var newWaypoint = new Waypoint();
                newWaypoint.Index = (ushort)waypoints.Count();
                newWaypoint.Position[0] = center[0];
                newWaypoint.Position[1] = center[1];
                newWaypoint.Position[2] = closestWaypoint.Position[2];
                newWaypoint.Crouch = closestWaypoint.Crouch;
                newWaypoint.Interest = interest;
                newWaypoint.Item = item;
                waypoints.Add(newWaypoint);

                closestWaypoint.Paths.Add(newWaypoint);
                newWaypoint.Paths.Add(closestWaypoint);
            }

            foreach (var entity in bspFile.Entities.Where(e => e.Classname == "func_ladder"))
            {
                var center = new float[3];
                for (int iAxis = 0; iAxis < 3; iAxis++)
                    center[iAxis] = entity.Origin[iAxis] + (entity.Extents[iAxis] + entity.Extents[iAxis + 3]) / 2;

                var bottom = new float[3];
                var top = new float[3];
                bottom[0] = center[0];
                bottom[1] = center[1];
                bottom[2] = entity.Origin[2] + entity.Extents[2] + 36;
                top[0] = center[0];
                top[1] = center[1];
                top[2] = entity.Origin[2] + entity.Extents[5] + 36;

                var bottomWaypoint = GetClosestWaypoint(baseWaypoints, bottom);
                var topWaypoint = GetClosestWaypoint(baseWaypoints, top);
                if (bottomWaypoint == topWaypoint)
                    continue;

                const float maxZDist = 64;
                if (MathF.Abs(bottom[2] - bottomWaypoint!.Position[2]) > maxZDist)
                    continue;

                bottom[2] = bottomWaypoint.Position[2];
                top[2] = topWaypoint.Position[2];

                var xd = bottomWaypoint.Position[0] - bottom[0];
                var yd = bottomWaypoint.Position[1] - bottom[1];
                var dd = MathF.Sqrt(xd * xd + yd * yd);
                xd /= dd;
                yd /= dd;
                bottom[0] += xd * 16;
                bottom[1] += yd * 16;
                top[0] += xd * 16;
                top[1] += yd * 16;

                var newBottomWaypoint = new Waypoint();
                newBottomWaypoint.Index = (ushort)waypoints.Count();
                newBottomWaypoint.Position = bottom;
                newBottomWaypoint.Ladder = true;
                newBottomWaypoint.Crouch = bottomWaypoint.Crouch;
                waypoints.Add(newBottomWaypoint);

                var newTopWaypoint = new Waypoint();
                newTopWaypoint.Index = (ushort)waypoints.Count();
                newTopWaypoint.Position = top;
                newTopWaypoint.Ladder = true;
                newTopWaypoint.Crouch = topWaypoint.Crouch;
                waypoints.Add(newTopWaypoint);

                bottomWaypoint.Paths.Add(newBottomWaypoint);
                newBottomWaypoint.Paths.Add(bottomWaypoint);

                newBottomWaypoint.Paths.Add(newTopWaypoint);
                newTopWaypoint.Paths.Add(newBottomWaypoint);

                newTopWaypoint.Paths.Add(topWaypoint);
                topWaypoint.Paths.Add(newTopWaypoint);
            }

            return waypoints;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace nav2wpt
{
    internal class NavArea
    {
        public uint Id { get; set; }
        public float[] Center { get; set; } = new float[3];
        public List<uint> Connections { get; set; } = new();
        public byte Attributes { get; set; } = 0;
    }

    internal class NavFile
    {
        public List<NavArea> NavAreas { get; set; } = new();
        public NavFile(string path) {
            var bytes = File.ReadAllBytes(path);

            //magic
            int pos = 0;
            if (BitConverter.ToUInt32(bytes, pos) != 0xFEEDFACE)
                throw new InvalidOperationException("Bad NAV magic!");
            pos += 4;

            //version
            if (BitConverter.ToUInt32(bytes, pos) != 5)
                throw new InvalidOperationException("Bad NAV version!");
            pos += 4;

            //bsp size
            var bspSize = BitConverter.ToUInt32(bytes, pos);
            pos += 4;

            //places
            var numPlaces = BitConverter.ToUInt16(bytes, pos);
            pos += 2;
            for (int iPlace = 0; iPlace < numPlaces; iPlace++)
            {
                var placeLen = BitConverter.ToUInt16(bytes, pos);
                pos += 2;
                pos += placeLen;
            }

            //areas
            var numAreas = BitConverter.ToUInt32(bytes, pos);
            pos += 4;
            for (int iArea = 0; iArea < numAreas; iArea++)
            {
                var navArea = new NavArea();

                var id = BitConverter.ToUInt32(bytes, pos);
                pos += 4;
                navArea.Id = id;

                var flags = bytes[pos];
                pos++;
                navArea.Attributes = flags;

                var extents = new float[8];
                for (int iExt = 0; iExt < 8; iExt++)
                {
                    extents[iExt] = BitConverter.ToSingle(bytes, pos);
                    pos += 4;
                }
                for (int iAxis = 0; iAxis < 3; iAxis++)
                    navArea.Center[iAxis] = (extents[iAxis] + extents[iAxis + 3]) / 2;

                for (int iDir = 0; iDir < 4; iDir++)
                {
                    var numConns = BitConverter.ToUInt32(bytes, pos);
                    pos += 4;
                    for (int iConn = 0; iConn < numConns; iConn++)
                    {
                        var connId = BitConverter.ToUInt32(bytes, pos);
                        pos += 4;
                        navArea.Connections.Add(connId);
                    }
                }
                var numSpots = bytes[pos];
                pos++;
                for (int iSpot = 0; iSpot < numSpots; iSpot++)
                {
                    var spotId = BitConverter.ToUInt32(bytes, pos);
                    pos += 4;
                    var spotPos = new float[3];
                    for (int i = 0; i < 3; i++)
                    {
                        spotPos[i] = BitConverter.ToSingle(bytes, pos);
                        pos += 4;
                    }
                    var spotFlags = bytes[pos];
                    pos++;
                }
                var numApprs = bytes[pos];
                pos++;
                for (int iAppr = 0; iAppr < numApprs; iAppr++)
                {
                    var apprHere = BitConverter.ToUInt32(bytes, pos);
                    pos += 4;
                    var apprPrev = BitConverter.ToUInt32(bytes, pos);
                    pos += 4;
                    var toHereHow = bytes[pos];
                    pos++;
                    var apprNext = BitConverter.ToUInt32(bytes, pos);
                    pos += 4;
                    var toNextHow = bytes[pos];
                    pos++;
                }
                var numEncs = BitConverter.ToUInt32(bytes, pos);
                pos += 4;
                for (int iEnc = 0; iEnc < numEncs; iEnc++)
                {
                    var encFrom = BitConverter.ToUInt32(bytes, pos);
                    pos += 4;
                    var fromDir = bytes[pos];
                    pos++;
                    var encTo = BitConverter.ToUInt32(bytes, pos);
                    pos += 4;
                    var toDir = bytes[pos];
                    pos++;
                    var numEncSpots = bytes[pos];
                    pos++;
                    for (int iSpot = 0; iSpot < numEncSpots; iSpot++)
                    {
                        var spotId = BitConverter.ToUInt32(bytes, pos);
                        pos += 4;
                        var t = bytes[pos];
                        pos++;
                    }
                }
                var place = BitConverter.ToUInt16(bytes, pos);
                pos += 2;

                NavAreas.Add(navArea);
            }
        }
    }
}

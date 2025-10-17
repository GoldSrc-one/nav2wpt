using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nav2wpt
{
    internal class Entity
    {
        public List<(string key, string value)> KeyValues { get; set; } = new();
        public string Classname { get; set; } = "";
        public float[] Origin { get; set; } = new float[3];
        public float[] Extents { get; set; } = new float[6];
    }

    internal class BspFile
    {
        public List<Entity> Entities = new List<Entity>();

        public BspFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            
            int pos = 0;
            var version = BitConverter.ToUInt32(bytes, pos);
            pos += 4;
            if (version != 30)
                throw new InvalidOperationException("Bad BSP version!");

            var lumps = new List<(uint offset, uint length)>();
            for (int iLump = 0; iLump < 15; iLump++)
            {
                var lumpOffset = BitConverter.ToUInt32(bytes, pos);
                pos += 4;
                var lumpLength = BitConverter.ToUInt32(bytes, pos);
                pos += 4;
                lumps.Add((lumpOffset, lumpLength));
            }

            var entityLump = lumps[0];
            var entityText = Encoding.ASCII.GetString(bytes, (int)entityLump.offset, (int)entityLump.length);
            ParseEntities(entityText);

            var modelsLump = lumps[14];
            foreach (var entity in Entities) {
                var model = entity.KeyValues.FirstOrDefault(kv => kv.key == "model").value ?? "";
                if (!model.StartsWith('*'))
                    continue;

                var modelIndex = int.Parse(model.Substring(1));
                var modelPos = (int)(modelsLump.offset + modelIndex * 64);
                var extents = new float[6];
                for (int iExt = 0; iExt < 6; iExt++)
                    entity.Extents[iExt] = BitConverter.ToSingle(bytes, modelPos + iExt * 4);
            }
        }

        void ParseEntities(string entityText) {
            bool inEntity = false;
            bool inString = false;
            List<char> currentString = new List<char>();
            List<string> strings = new List<string>();
            foreach(var c in entityText) {
                if (!inEntity) {
                    if (c == '{')
                        inEntity = true;
                    continue;
                }

                if (!inString)
                {
                    if (c == '"')
                        inString = true;

                    if (c == '}')
                    {
                        var entity = new Entity();
                        for (int iString = 0; iString < strings.Count - 1; iString += 2)
                            entity.KeyValues.Add((strings[iString], strings[iString + 1]));
                        entity.Classname = entity.KeyValues.FirstOrDefault(kv => kv.key == "classname").value ?? "";
                        if (entity.KeyValues.Any(kv => kv.key == "origin"))
                        {
                            entity.Origin = entity.KeyValues.First(kv => kv.key == "origin").value.Split(' ').Select(float.Parse).ToArray();
                            if (entity.Origin.Length != 3)
                                throw new InvalidOperationException("Bad entity origin!");
                        }
                        Entities.Add(entity);
                        strings.Clear();
                        inEntity = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = false;
                    strings.Add(new string(currentString.ToArray()));
                    currentString.Clear();
                    continue;
                }
                currentString.Add(c);
            }
        }
    }
}

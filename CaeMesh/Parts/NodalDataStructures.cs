﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    public class AvgData
    {
        public Dictionary<int, AvgNodalData> Nodes;


        // Constructors
        public AvgData()
        {
            Nodes = new Dictionary<int, AvgNodalData>();
        }
        public AvgData(AvgData avgData)
            : this()
        {
            foreach (var entr in avgData.Nodes)
            {
                Nodes.Add(entr.Key, new AvgNodalData(entr.Value));
            }
        }


        // Methods
        public void AddRange(Dictionary<int, AvgNodalData> nodes)
        {
            foreach (var entry in nodes) Nodes.Add(entry.Key, entry.Value);
        }
        public Dictionary<int, double> GetAveragedValues()
        {
            Dictionary<int, double> values = new Dictionary<int, double>();
            double value;
            double areaSum;
            //
            foreach (var nodeEntry in Nodes)
            {
                areaSum = 0;
                value = 0;
                foreach (var elementEntry in nodeEntry.Value.Elements)
                {
                    foreach (var tuple in elementEntry.Value.Data)
                    {
                        if (tuple.Item1 != 0)
                        {
                            value += tuple.Item1 * tuple.Item2;
                            areaSum += tuple.Item2;
                        }
                    }
                }
                if (areaSum != 0)
                {
                    value /= areaSum;
                    values.Add(nodeEntry.Key, value);
                }
            }
            //
            return values;
        }
    }
    //
    [Serializable]
    public class AvgNodalData
    {
        public Dictionary<int, AvgNodalElementData> Elements;


        // Constructors
        public AvgNodalData()
        {
            Elements = new Dictionary<int, AvgNodalElementData>();
        }
        public AvgNodalData(AvgNodalData avgNodalSurfaceData)
            : this()
        {
            foreach (var entry in avgNodalSurfaceData.Elements)
            {
                Elements.Add(entry.Key, new AvgNodalElementData(entry.Value));
            }
        }
    }
    //
    [Serializable]
    public class AvgNodalElementData
    {
        // Value, area, normal
        public List<Tuple<double, double, Vec3D>> Data;


        // Constructors
        public AvgNodalElementData()
        {
            Data = new List<Tuple<double, double, Vec3D>>();
        }
        public AvgNodalElementData(AvgNodalElementData avgNodalSurfaceElementData)
            : this()
        {
            foreach (var tuple in avgNodalSurfaceElementData.Data)
            {
                Data.Add(new Tuple<double, double, Vec3D>(tuple.Item1, tuple.Item2, tuple.Item3));
            }
        }
    }
    //
    public class AvgEntryData
    {
        public int[] NodeIds;
        public int SurfaceId;
        public int ElementId;
        public double Area;
        public Vec3D Normal;
    }
}

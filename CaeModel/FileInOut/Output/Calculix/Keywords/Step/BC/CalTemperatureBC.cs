﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalTemperatureBC : CalculixKeyword
    {
        // Variables                                                                                                                
        private TemperatureBC _temperatureBC;
        private string _nodeSetNameOfSurface;


        // Properties                                                                                                               
        public override object GetBase { get { return _temperatureBC; } }


        // Constructor                                                                                                              
        public CalTemperatureBC(TemperatureBC temperatureBC, string nodeSetNameOfSurface)
        {
            _temperatureBC = temperatureBC;
            _nodeSetNameOfSurface = nodeSetNameOfSurface;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _temperatureBC.Name);
            sb.AppendLine("*Boundary");
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            // *Boundary
            // 6975, 11, 11, 100        node id, start DOF, end DOF, value
            // Node set
            string regionName;
            if (_temperatureBC.RegionType == CaeGlobals.RegionTypeEnum.NodeSetName)
            {
                regionName = _temperatureBC.RegionName;
            }
            // Surface
            else if (_temperatureBC.RegionType == CaeGlobals.RegionTypeEnum.SurfaceName)
            {
                if (_nodeSetNameOfSurface == null) throw new ArgumentException();
                regionName = _nodeSetNameOfSurface;
            }
            else throw new NotSupportedException();
            //
            sb.AppendFormat("{0}, 11, 11, {1}{2}", regionName, _temperatureBC.Temperature, Environment.NewLine);
            //
            return sb.ToString();
        }
    }
}

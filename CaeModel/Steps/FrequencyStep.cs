﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace CaeModel
{
    [Serializable]
    public class FrequencyStep : Step, ISerializable
    {
        // Variables                                                                                                                
        private double _numOfFrequencies;       //ISerializable
        private bool _storage;                  //ISerializable


        // Properties                                                                                                               
        public double NumOfFrequencies
        {
            get { return _numOfFrequencies; }
            set 
            {
                if (value <= 1) throw new Exception("The number of frequencies must be larger than 0.");
                _numOfFrequencies = value;
            }
        }
        public bool Storage { get { return _storage; } set { _storage = value; } }


        // Constructors                                                                                                             
        public FrequencyStep(string name)
            :base(name)
        {
            _perturbation = true;
            //
            _numOfFrequencies = 10;
            _storage = false;
            //
            AddFieldOutput(new NodalFieldOutput("NF-Output-1", NodalFieldVariable.U | NodalFieldVariable.RF));
            AddFieldOutput(new ElementFieldOutput("EF-Output-1", ElementFieldVariable.E | ElementFieldVariable.S));
        }
        //ISerializable
        public FrequencyStep(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            int count = 0;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_numOfFrequencies":
                        _numOfFrequencies = (double)entry.Value; count++; break;
                    case "_storage":
                        _storage = (bool)entry.Value; count++; break;
                }
            }
            if (count != 2) throw new NotSupportedException();
        }


        // Methods                                                                                                                  
        public override bool IsBoundaryConditionSupported(BoundaryCondition boundaryCondition)
        {
            if (boundaryCondition is FixedBC || boundaryCondition is DisplacementRotation)
                return true;
            else if (boundaryCondition is SubmodelBC || boundaryCondition is TemperatureBC)
                return false;
            else throw new NotSupportedException();
        }
        public override bool IsLoadSupported(Load load)
        {
            if (load is CLoad || load is MomentLoad || load is DLoad || load is STLoad || load is ShellEdgeLoad ||
                load is GravityLoad || load is CentrifLoad || load is PreTensionLoad)
                return false;
            else throw new NotSupportedException();
        }
        public override bool IsDefinedFieldSupported(DefinedField definedField)
        {
            if (definedField is DefinedTemperature) return false;
            else throw new NotSupportedException();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_numOfFrequencies", _numOfFrequencies, typeof(double));
            info.AddValue("_storage", _storage, typeof(bool));
        }
    }
}

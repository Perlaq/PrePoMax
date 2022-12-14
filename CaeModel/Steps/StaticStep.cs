using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace CaeModel
{
    [Serializable]
    public class StaticStep : Step, ISerializable
    {
        // Variables                                                                                                                
        private double _timePeriod;                 //ISerializable
        private double _initialTimeIncrement;       //ISerializable
        private double _minTimeIncrement;           //ISerializable
        private double _maxTimeIncrement;           //ISerializable
        private bool _direct;                       //ISerializable


        // Properties                                                                                                               
        public double TimePeriod 
        {
            get { return _timePeriod; }
            set 
            {
                if (value <= 0) throw new Exception("The time period value must be positive.");
                _timePeriod = value;
            }
        }
        public double InitialTimeIncrement
        {
            get { return _initialTimeIncrement; }
            set
            {
                if (value <= 0) throw new Exception("The initial time increment value must be positive.");
                _initialTimeIncrement = value;
            }
        }
        public double MinTimeIncrement
        {
            get { return _minTimeIncrement; }
            set
            {
                if (value <= 0) throw new Exception("The min time increment value must be positive.");
                _minTimeIncrement = value;
            }
        }
        public double MaxTimeIncrement
        {
            get { return _maxTimeIncrement; }
            set
            {
                if (value <= 0) throw new Exception("The max time increment value must be positive.");
                _maxTimeIncrement = value;
            }
        }
        public bool Direct { get { return _direct; } set { _direct = value; } }


        // Constructors                                                                                                             
        public StaticStep(string name)
            :base(name)
        {
            _timePeriod = 1;
            _initialTimeIncrement = 1;
            _minTimeIncrement = 1E-5;
            _maxTimeIncrement = 1E30;
            _direct = false;
            //
            AddFieldOutput(new NodalFieldOutput("NF-Output-1", NodalFieldVariable.U | NodalFieldVariable.RF));
            AddFieldOutput(new ElementFieldOutput("EF-Output-1", ElementFieldVariable.E | ElementFieldVariable.S));
        }
        
        //ISerializable
        public StaticStep(SerializationInfo info, StreamingContext context)
            :base(info, context)
        {            
            // Compatibility for version v.0.5.3
            _direct = false;
            //
            int count = 0;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_timePeriod":
                        _timePeriod = (double)entry.Value; count++; break;
                    case "_initialTimeIncrement":
                        _initialTimeIncrement = (double)entry.Value; count++; break;
                    case "_minTimeIncrement":
                        _minTimeIncrement = (double)entry.Value; count++; break;
                    case "_maxTimeIncrement":
                        _maxTimeIncrement = (double)entry.Value; count++; break;
                    case "_direct":
                        _direct = (bool)entry.Value; break;
                }
            }
            if (count != 4) throw new NotSupportedException();
        }


        // Methods                                                                                                                  

        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_timePeriod", _timePeriod, typeof(double));
            info.AddValue("_initialTimeIncrement", _initialTimeIncrement, typeof(double));
            info.AddValue("_minTimeIncrement", _minTimeIncrement, typeof(double));
            info.AddValue("_maxTimeIncrement", _maxTimeIncrement, typeof(double));
            info.AddValue("_direct", _direct, typeof(bool));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace CaeModel
{
    [Serializable]
    public class BuckleStep : Step, ISerializable
    {
        // Variables                                                                                                                
        private int _numOfBucklingFactors;      //ISerializable
        private double _accuracy;               //ISerializable


        // Properties                                                                                                               
        public int NumOfBucklingFactors
        {
            get { return _numOfBucklingFactors; }
            set
            {
                _numOfBucklingFactors = value;
                if (_numOfBucklingFactors < 1) _numOfBucklingFactors = 1;
            }
        }
        public double Accuracy
        {
            get { return _accuracy; }
            set
            {
                _accuracy = value;

                if (_numOfBucklingFactors < 1) _numOfBucklingFactors = 1;
            }
        }


        // Constructors                                                                                                             
        public BuckleStep(string name)
            :base(name)
        {
            //_perturbation = false;

            _numOfBucklingFactors = 1;
            _accuracy = 0.01;

            AddFieldOutput(new NodalFieldOutput("NF-Output-1", NodalFieldVariable.U | NodalFieldVariable.RF));
            AddFieldOutput(new ElementFieldOutput("EF-Output-1", ElementFieldVariable.E | ElementFieldVariable.S));
        }

        //ISerializable
        public BuckleStep(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            int count = 0;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_numOfBucklingFactors":
                        _numOfBucklingFactors = (int)entry.Value; count++; break;
                    case "_accuracy":
                        _accuracy = (double)entry.Value; count++; break;                   
                }
            }
            if (count != 2) throw new NotSupportedException();
        }


        // Methods                                                                                                                  

        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_numOfBucklingFactors", _numOfBucklingFactors, typeof(int));
            info.AddValue("_accuracy", _accuracy, typeof(double));
        }
    }
}

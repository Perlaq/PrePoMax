using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalStaticStep : CalculixKeyword
    {
        // Variables                                                                                                                
        private StaticStep _step;


        // Constructor                                                                                                              
        public CalStaticStep(StaticStep step)
        {
            _step = step;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string direct = _step.Direct ? ", Direct" : "";
            //SOLVER=ITERATIVE CHOLESKY
            //SOLVER=ITERATIVE SCALING
            return string.Format("*Static{0}{1}", direct, Environment.NewLine);
        }
        public override string GetDataString()
        {
            if (_step.Nlgeom)
            {
                string minMax = _step.Direct ? "" : string.Format(", {0}, {1}", _step.MinTimeIncrement, _step.MaxTimeIncrement);

                return string.Format("{0}, {1}{2}{3}", _step.InitialTimeIncrement, _step.TimePeriod, minMax, Environment.NewLine);
            }
            else return "";
        }
    }
}

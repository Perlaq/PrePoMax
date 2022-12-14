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
    internal class CalBuckleStep : CalculixKeyword
    {
        // Variables                                                                                                                
        private BuckleStep _step;


        // Constructor                                                                                                              
        public CalBuckleStep(BuckleStep step)
        {
            _step = step;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Buckle{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}, {1}{2}", _step.NumOfBucklingFactors, _step.Accuracy, Environment.NewLine);
        }
    }
}

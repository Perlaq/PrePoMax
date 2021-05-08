﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalFrequencyStep : CalculixKeyword
    {
        // Variables                                                                                                                
        private FrequencyStep _step;


        // Constructor                                                                                                              
        public CalFrequencyStep(FrequencyStep step)
        {
            _step = step;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string solver = _step.SolverType == SolverTypeEnum.Default ? "" : ", Solver=" + _step.SolverType.GetDisplayedName();
            string storage = _step.Storage ? ", Storage=Yes" : "";
            return string.Format("*Frequency{0}{1}{2}", solver, storage, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _step.NumOfFrequencies, Environment.NewLine);
        }
    }
}

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
    internal class CalPlastic : CalculixKeyword
    {
        // Variables                                                                                                                
        private Plastic _plastic;


        // Constructor                                                                                                              
        public CalPlastic(Plastic plastic)
        {
            _plastic = plastic;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string hardening = "";
            if (_plastic.Hardening != PlasticHardening.Isotropic) hardening = ", Hardening=" + _plastic.Hardening;
            return string.Format("*Plastic{0}{1}", hardening, Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _plastic.StressStrain.GetLength(0); i++)
            {
                sb.AppendFormat("{0}, {1}{2}", _plastic.StressStrain[i][0], _plastic.StressStrain[i][1], Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}

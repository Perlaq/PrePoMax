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
    internal class CalFriction : CalculixKeyword
    {
        // Variables                                                                                                                
        private Friction _friction;


        // Constructor                                                                                                              
        public CalFriction(Friction friction)
        {
            _friction = friction;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Friction{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_friction.Coefficient);
            if (!double.IsNaN(_friction.StikSlope)) sb.AppendFormat(", {0}", _friction.StikSlope);
            sb.AppendLine();
            return sb.ToString();
        }
    }
}

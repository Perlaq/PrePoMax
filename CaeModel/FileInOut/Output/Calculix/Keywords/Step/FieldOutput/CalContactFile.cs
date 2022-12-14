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
    internal class CalContactFile : CalculixKeyword
    {
        // Variables                                                                                                                
        private ContactFieldOutput _contactFieldOutput;


        // Constructor                                                                                                              
        public CalContactFile(ContactFieldOutput contactFieldOutput)
        {
            _contactFieldOutput = contactFieldOutput;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string frequency = _contactFieldOutput.Frequency > 1 ? ", Frequency=" + _contactFieldOutput.Frequency : "";
            //
            return string.Format("*Contact file{0}{1}", frequency, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _contactFieldOutput.Variables.ToString(), Environment.NewLine);
        }
    }
}

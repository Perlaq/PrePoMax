using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeResults
{
    [Serializable]
    public enum StepType
    {
        None,
        Static,
        Frequency,
        Buckling
    }

    [Serializable]
    public class FieldData : CaeGlobals.NamedClass
    {
        // Variables                                                                                                                
        public string Component;
        public int UserDefinedBlockId;
        public StepType Type;
        public float Time;
        public int StepId;
        public int StepIncrementId;


        // Constructors                                                                                                              
        public FieldData(string name)
            :base()
        {
            _checkName = false;     // the name may contain other cahracters - do not use constructor with name
            Name = name;
            Component = null;
            UserDefinedBlockId = -1;
            Type = StepType.None;
            Time = -1;
            StepId = -1;
            StepIncrementId = -1;
        }
        public FieldData(string name, string component, int stepId, int stepIncrementId)
           : base(name)
        {
            Name = name;
            Component = component;
            UserDefinedBlockId = -1;
            Type = StepType.None;
            Time = -1;
            StepId = stepId;
            StepIncrementId = stepIncrementId;
        }
        public FieldData(FieldData fieldData)
            : base(fieldData.Name)
        {
            Component = fieldData.Component;
            UserDefinedBlockId = fieldData.UserDefinedBlockId;
            Type = fieldData.Type;
            Time = fieldData.Time;
            StepId = fieldData.StepId;
            StepIncrementId = fieldData.StepIncrementId;
        }


        // Static methods                                                                                                           
        public static void WriteToFile(FieldData fieldData, System.IO.BinaryWriter bw)
        {
            if (fieldData == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);

                bw.Write(fieldData.Name);
                if (fieldData.Component == null)
                {
                    bw.Write((int)0);
                }
                else
                {
                    bw.Write((int)1);
                    bw.Write(fieldData.Component);
                }

                bw.Write(fieldData.UserDefinedBlockId);
                bw.Write((int)fieldData.Type);
                bw.Write(fieldData.Time);
                bw.Write(fieldData.StepId);
                bw.Write(fieldData.StepIncrementId);
            }
        }
        public static FieldData ReadFromFile(System.IO.BinaryReader br)
        {
            int fieldDataExists = br.ReadInt32();
            if (fieldDataExists == 1)
            {
                FieldData fieldData = new FieldData(br.ReadString());       // read the name
                
                int componentExists = br.ReadInt32();
                if (componentExists == 1) fieldData.Component = br.ReadString();

                fieldData.UserDefinedBlockId = br.ReadInt32();
                fieldData.Type = (StepType)br.ReadInt32();
                fieldData.Time = br.ReadSingle();
                fieldData.StepId = br.ReadInt32();
                fieldData.StepIncrementId = br.ReadInt32();
                return fieldData;
            }
            return null;
        }


        // Methods                                                                                                                  
        public string GetHashKey()
        {
            return Name + "_" + StepId.ToString() + "_" + StepIncrementId.ToString();
        }
        public bool Equals(FieldData data)
        {
            return  Name == data.Name &&
                    Component == data.Component &&
                    //UserDefinedBlockId == data.UserDefinedBlockId &&
                    //Type == data.Type &&
                    StepId == data.StepId &&
                    StepIncrementId == data.StepIncrementId;
        }
    }
}

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
    internal class CalNode : CalculixKeyword
    {
        // Variables                                                                                                                
        IDictionary<string, int[]> _referencePointsNodeIds;
        IDictionary<string, FeReferencePoint> _referencePoints;
        IDictionary<int, FeNode> _nodes;


        // Constructor                                                                                                              
        public CalNode(FeModel model, IDictionary<string, int[]> referencePointsNodeIds)
        {
            _referencePoints = model.Mesh.ReferencePoints;
            _nodes = model.Mesh.Nodes;
            _referencePointsNodeIds = referencePointsNodeIds;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Node{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            FeNode node;
            foreach (var entry in _nodes)
            {
                node = entry.Value;
                sb.AppendFormat("{0}, {1:E8}, {2:E8}, {3:E8}", node.Id, node.X, node.Y, node.Z).AppendLine();
            }
            //
            FeReferencePoint rp;
            foreach (var entry in _referencePointsNodeIds)
            {
                if (_referencePoints.TryGetValue(entry.Key, out rp))
                {
                    sb.AppendFormat("{0}, {1:E8}, {2:E8}, {3:E8}", entry.Value[0], rp.X, rp.Y, rp.Z).AppendLine();
                    sb.AppendFormat("{0}, {1:E8}, {2:E8}, {3:E8}", entry.Value[1], rp.X, rp.Y, rp.Z).AppendLine();
                }
                else
                {
                    sb.AppendFormat("{0}, {1:E8}, {2:E8}, {3:E8}", entry.Value[0], 0, 0, 0).AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public abstract class FeElement2D : FeElement
    {
        // Properties                                                                                                               
        
            
        // Constructors                                                                                                             
        public FeElement2D(int id, int[] nodeIds)
            : base(id, nodeIds)
        {
        }
        public FeElement2D(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        abstract public int[][] GetAllVtkCells();
    }
}

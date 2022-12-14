using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class RigidBody : Constraint, IMasterSlaveMultiRegion
    {
        // Variables                                                                                                                
        private RegionTypeEnum _masterRegionType;
        private string _referencePointName;
        private RegionTypeEnum _regionType;
        private string _regionName;
        private int[] _creationIds;
        private Selection _creationData;


        // Properties                                                                                                               
        public RegionTypeEnum MasterRegionType { get { return _masterRegionType; } set { _masterRegionType = value; } }
        public string MasterRegionName { get { return _referencePointName; } set { _referencePointName = value; } }
        public RegionTypeEnum SlaveRegionType { get { return _regionType; } set { _regionType = value; } }
        public string SlaveRegionName { get { return _regionName; } set { _regionName = value; } }
        //
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public string ReferencePointName { get { return _referencePointName; } set { _referencePointName = value; } }
        //
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }


        // Constructors                                                                                                             
        public RigidBody(string name, string referencePointName, string regionName, RegionTypeEnum regionType)
            : base(name)
        {
            _masterRegionType = RegionTypeEnum.ReferencePointName;
            _referencePointName = referencePointName;
            _regionName = regionName;
            _regionType = regionType;
            _creationIds = null;
            _creationData = null;
        }


        // Methods                                                                                                                  
    }
}

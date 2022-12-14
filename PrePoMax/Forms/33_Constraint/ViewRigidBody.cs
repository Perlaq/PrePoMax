using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.Drawing;

namespace PrePoMax
{
    [Serializable]
    public class ViewRigidBody : ViewConstraint
    {
        // Variables                                                                                                                
        private CaeModel.RigidBody _rigidBody;
        private string _selectionHidden;


        // Properties                                                                                                               
        public override string Name { get { return _rigidBody.Name; } set { _rigidBody.Name = value; } }
        //
        [CategoryAttribute("Control point")]
        [OrderedDisplayName(0, 10, "Reference point")]
        [DescriptionAttribute("Select the reference point for the creation of the rigid body definition.")]
        [Id(1, 2)]
        public string ReferencePointName { get { return _rigidBody.ReferencePointName; } set { _rigidBody.ReferencePointName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the rigid body definition.")]
        [Id(1, 3)]
        public override string SlaveRegionType { get { return base.SlaveRegionType; } set { base.SlaveRegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        [Id(2, 3)]
        public string SelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Node set")]
        [DescriptionAttribute("Select the node set for the creation of the rigid body definition.")]
        [Id(3, 3)]
        public string NodeSetName { get { return _rigidBody.RegionName; } set { _rigidBody.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the rigid body definition.")]
        [Id(4, 3)]
        public string SurfaceName { get { return _rigidBody.RegionName; } set { _rigidBody.RegionName = value; } }
        //
        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Select constraint color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 10)]
        public Color Color { get { return _rigidBody.MasterColor; } set { _rigidBody.MasterColor = value; } }


        // Constructors                                                                                                             
        public ViewRigidBody(CaeModel.RigidBody rigidBody)
        {
            // The order is important
            _rigidBody = rigidBody;
            //
            Dictionary<RegionTypeEnum, string> masterRegionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            masterRegionTypePropertyNamePairs.Add(RegionTypeEnum.ReferencePointName, nameof(ReferencePointName));
            //
            Dictionary<RegionTypeEnum, string> slaveRegionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            base.SetBase(_rigidBody, masterRegionTypePropertyNamePairs, slaveRegionTypePropertyNamePairs);
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override CaeModel.Constraint GetBase()
        {
            return _rigidBody;
        }
        public void PopululateDropDownLists(string[] referencePointNames, string[] nodeSetNames, string[] surfaceNames)
        {
            base.DynamicCustomTypeDescriptor.PopulateProperty(nameof(ReferencePointName), referencePointNames);
            // Master
            Dictionary<RegionTypeEnum, string[]> masterRegionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            masterRegionTypeListItemsPairs.Add(RegionTypeEnum.ReferencePointName, referencePointNames);
            // Slave
            Dictionary<RegionTypeEnum, string[]> slaveRegionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            slaveRegionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            slaveRegionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            slaveRegionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            //
            base.PopululateDropDownLists(masterRegionTypeListItemsPairs, slaveRegionTypeListItemsPairs);
        }
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            // Hide SelectionHidden
            DynamicTypeDescriptor.CustomPropertyDescriptor cpd;
            if (base.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString())
            {
                cpd = base.DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden));
                cpd.SetIsBrowsable(false);
            }
        }
    }

}

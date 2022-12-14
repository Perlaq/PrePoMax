using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewFeReferencePoint : ViewMultiRegion
    {
        // Variables                                                                                                                
        private FeReferencePoint _referencePoint;
        private int _numOfNodeSets;
        private int _numOfSurfaces;


        // Properties                                                                                                               
        [Category("Data")]
        [DisplayName("Name")]
        [Description("Name of the reference point.")]
        [Id(1, 1)]
        public string Name { get { return _referencePoint.Name; } set { _referencePoint.Name = value; } }
        //
        [Category("Region")]
        [OrderedDisplayName(1, 10, "Create by/from")]
        [Description("Select the method for the creation of the reference point.")]
        [Id(1, 2)]
        public string CreatedFrom
        {
            get { return _referencePoint.CreatedFrom.ToString(); }
            set
            {
                if (Enum.TryParse(value, out FeReferencePointCreatedFrom createdFrom))
                {
                    _referencePoint.CreatedFrom = createdFrom;
                    SetPropertiesVisibility();
                }
            }
        }
        //
        [Category("Region")]
        [OrderedDisplayName(2, 10, "Region type")]
        [Description("Select the region type for the creation of the reference point.")]
        [Id(2, 2)]
        public override string RegionType { get { return base.RegionType; } set { base.RegionType = value; } }
        //
        [Category("Region")]
        [OrderedDisplayName(3, 10, "Node set")]
        [Description("Select the node set for the creation of the reference point.")]
        [Id(3, 2)]
        public string NodeSetName { get { return _referencePoint.RegionName; } set { _referencePoint.RegionName = value; } }
        //
        [Category("Region")]
        [OrderedDisplayName(4, 10, "Surface")]
        [Description("Select the surface for the creation of the reference point.")]
        [Id(4, 2)]
        public string SurfaceName { get { return _referencePoint.RegionName; } set { _referencePoint.RegionName = value; } }
        //
        [Category("Coordinates")]
        [DisplayName("X")]
        [Description("X coordinate of the reference point.")]
        [TypeConverter(typeof(CaeGlobals.StringLengthConverter))]
        [Id(2, 3)]
        public double X { get { return _referencePoint.X; } set { _referencePoint.X = value; } }
        //
        [Category("Coordinates")]
        [DisplayName("Y")]
        [Description("Y coordinate of the reference point.")]
        [TypeConverter(typeof(CaeGlobals.StringLengthConverter))]
        [Id(3, 3)]
        public double Y { get { return _referencePoint.Y; } set { _referencePoint.Y = value; } }
        //
        [Category("Coordinates")]
        [DisplayName("Z")]
        [Description("Z coordinate of the reference point.")]
        [TypeConverter(typeof(CaeGlobals.StringLengthConverter))]
        [Id(4, 3)]
        public double Z { get { return _referencePoint.Z; } set { _referencePoint.Z = value; } }
        //
        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Select reference point color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 10)]
        public System.Drawing.Color Color { get { return _referencePoint.Color; } set { _referencePoint.Color = value; } }


        // Constructors                                                                                                             
        public ViewFeReferencePoint(FeReferencePoint referencePoint)
        {
            // the order is important
            _referencePoint = referencePoint;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(this.NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(this.SurfaceName));
            base.SetBase(_referencePoint, regionTypePropertyNamePairs);
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public FeReferencePoint GetBase()
        {
            return _referencePoint;
        }

        public void PopululateDropDownLists(string[] nodeSetNames, string[] surfaceNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            base.PopululateDropDownLists(regionTypeListItemsPairs);
            //
            _numOfNodeSets = nodeSetNames.Length;
            _numOfSurfaces = surfaceNames.Length;
            //
            CustomPropertyDescriptor cpd;
            // CreatedFrom
            cpd = base.DynamicCustomTypeDescriptor.GetProperty(nameof(CreatedFrom));
            cpd.StatandardValues.Clear();
            cpd.StatandardValues.Add(new StandardValueAttribute(FeReferencePointCreatedFrom.Selection.ToString()));
            if (_numOfNodeSets + _numOfSurfaces > 0)
            {
                cpd.StatandardValues.Add(new StandardValueAttribute(FeReferencePointCreatedFrom.BoundingBoxCenter.ToString()));
                cpd.StatandardValues.Add(new StandardValueAttribute(FeReferencePointCreatedFrom.CenterOfGravity.ToString()));
            }
            //
            SetPropertiesVisibility();
        }
       
        private void SetPropertiesVisibility()
        {
            DynamicCustomTypeDescriptor dctd = base.DynamicCustomTypeDescriptor;
            //
            if (CreatedFrom == FeReferencePointCreatedFrom.Selection.ToString())
            {
                dctd.GetProperty(nameof(RegionType)).SetIsBrowsable(false);
                dctd.GetProperty(nameof(NodeSetName)).SetIsBrowsable(false);
                dctd.GetProperty(nameof(SurfaceName)).SetIsBrowsable(false);
                dctd.GetProperty(nameof(X)).SetIsReadOnly(false);
                dctd.GetProperty(nameof(Y)).SetIsReadOnly(false);
                dctd.GetProperty(nameof(Z)).SetIsReadOnly(false);
            }
            else
            {
                dctd.GetProperty(nameof(RegionType)).SetIsBrowsable(true);
                //
                if (_numOfNodeSets > 0 && _referencePoint.RegionType == RegionTypeEnum.NodeSetName)
                    RegionType = RegionTypeEnum.NodeSetName.ToFriendlyString();
                else
                    RegionType = RegionTypeEnum.SurfaceName.ToFriendlyString();
                //
                if (_numOfSurfaces > 0 && _referencePoint.RegionType == RegionTypeEnum.SurfaceName)
                    RegionType = RegionTypeEnum.SurfaceName.ToFriendlyString();
                else
                    RegionType = RegionTypeEnum.NodeSetName.ToFriendlyString();
                //
                dctd.GetProperty(nameof(X)).SetIsReadOnly(true);
                dctd.GetProperty(nameof(Y)).SetIsReadOnly(true);
                dctd.GetProperty(nameof(Z)).SetIsReadOnly(true);
            }
        }
    }
}

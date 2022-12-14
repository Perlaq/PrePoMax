using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public class ViewGravityLoad : ViewLoad
    {
        // Variables                                                                                                                
        private CaeModel.GravityLoad _gLoad;


        // Properties                                                                                                               
        public override string Name { get { return _gLoad.Name; } set { _gLoad.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Part")]
        [DescriptionAttribute("Select the part for the creation of the load.")]
        [Id(3, 2)]
        public string PartName { get { return _gLoad.RegionName; } set { _gLoad.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Element set")]
        [DescriptionAttribute("Select the element set for the creation of the load.")]
        [Id(4, 2)]
        public string ElementSetName { get { return _gLoad.RegionName; } set { _gLoad.RegionName = value; } }
        //
        [CategoryAttribute("Gravity components")]
        [OrderedDisplayName(0, 10, "F1")]
        [DescriptionAttribute("Gravity component in the direction of the first axis.")]
        [TypeConverter(typeof(CaeGlobals.StringAccelerationConverter))]
        [Id(1, 3)]
        public double F1 { get { return _gLoad.F1; } set { _gLoad.F1 = value; } }
        //
        [CategoryAttribute("Gravity components")]
        [OrderedDisplayName(1, 10, "F2")]
        [DescriptionAttribute("Gravity component in the direction of the second axis.")]
        [TypeConverter(typeof(CaeGlobals.StringAccelerationConverter))]
        [Id(2, 3)]
        public double F2 { get { return _gLoad.F2; } set { _gLoad.F2 = value; } }
        //
        [CategoryAttribute("Gravity components")]
        [OrderedDisplayName(2, 10, "F3")]
        [DescriptionAttribute("Gravity component in the direction of the third axis.")]
        [TypeConverter(typeof(CaeGlobals.StringAccelerationConverter))]
        [Id(3, 3)]
        public double F3 { get { return _gLoad.F3; } set { _gLoad.F3 = value; } }
        //
        [CategoryAttribute("Gravity magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("The magnitude of the gravity load.")]
        [TypeConverter(typeof(CaeGlobals.StringAccelerationConverter))]
        [Id(1, 4)]
        public double Flength
        {
            get { return Math.Sqrt(_gLoad.F1 * _gLoad.F1 + _gLoad.F2 * _gLoad.F2 + _gLoad.F3 * _gLoad.F3); }
            set
            {
                if (value <= 0)
                    throw new Exception("The magnitude of the force must be greater than 0.");

                double len = Math.Sqrt(_gLoad.F1 * _gLoad.F1 + _gLoad.F2 * _gLoad.F2 + _gLoad.F3 * _gLoad.F3);
                double r;
                if (len == 0) r = 0;
                else r = value / len;
                _gLoad.F1 *= r;
                _gLoad.F2 *= r;
                _gLoad.F3 *= r;
            }
        }
        //
        public override System.Drawing.Color Color { get { return _gLoad.Color; } set { _gLoad.Color = value; } }


        // Constructors                                                                                                             
        public ViewGravityLoad(CaeModel.GravityLoad gLoad)
        {
            // The order is important
            _gLoad = gLoad;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.PartName, nameof(PartName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ElementSetName, nameof(ElementSetName));
            //
            base.SetBase(_gLoad, regionTypePropertyNamePairs);
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override CaeModel.Load GetBase()
        {
            return _gLoad;
        }
        public void PopululateDropDownLists(string[] partNames, string[] elementSetNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.PartName, partNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.ElementSetName, elementSetNames);
            base.PopululateDropDownLists(regionTypeListItemsPairs);
        }
    }

}

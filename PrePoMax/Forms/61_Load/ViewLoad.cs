using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using System.Drawing.Design;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewLoad: ViewMultiRegion
    {
        // Variables                                                                                                                
        private string _selectionHidden;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the load.")]
        [Id(1, 1)]
        public abstract string Name { get; set; }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the load.")]
        [Id(1, 2)]
        public override string RegionType { get { return base.RegionType; } set { base.RegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        [Id(2, 2)]
        public string SelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }
        //
        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Select load color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 10)]
        public abstract System.Drawing.Color Color { get; set; }


        // Constructors                                                                                                             


        // Methods                                                                                                                  
        public abstract CaeModel.Load GetBase();
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            // Hide SelectionHidden
            DynamicTypeDescriptor.CustomPropertyDescriptor cpd;
            if (base.RegionType == RegionTypeEnum.Selection.ToFriendlyString())
            {
                cpd = base.DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden));
                cpd.SetIsBrowsable(false);
            }
        }
    }
}

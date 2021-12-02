﻿using System;
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
    public class TranslateParameters
    {
        // Variables                                                                                                                      
        private DynamicCustomTypeDescriptor _dctd = null;
        private ItemSetData _startPointItemSetData;
        private ItemSetData _endPointItemSetData;
        private double[] _startPoint;
        private double[] _endPoint;
        private bool _copy;
        private bool _twoD;


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Operation")]
        [DescriptionAttribute("Select the move/copy operation.")]
        [Id(1, 1)]
        public bool Copy { get { return _copy; } set { _copy = value; } }
        //
        [Category("Start point coordinates")]
        [OrderedDisplayName(0, 10, "Select the start point")]
        [DescriptionAttribute("Select the start point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 2)]
        public ItemSetData StartPointItemSet
        {
            get { return _startPointItemSetData; }
            set
            {
                if (value != _startPointItemSetData)
                    _startPointItemSetData = value;
            }
        }
        //
        [Category("Start point coordinates")]
        [OrderedDisplayName(1, 10, "X")]
        [Description("X coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 2)]
        public double X1 { get { return _startPoint[0]; } set { _startPoint[0] = value; } }
        //
        [Category("Start point coordinates")]
        [OrderedDisplayName(2, 10, "Y")]
        [Description("Y coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 2)]
        public double Y1 { get { return _startPoint[1]; } set { _startPoint[1] = value; } }
        //
        [Category("Start point coordinates")]
        [OrderedDisplayName(3, 10, "Z")]
        [Description("Z coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 2)]
        public double Z1
        { 
            get { return _startPoint[2]; } 
            set 
            {
                _startPoint[2] = value;
                if (_twoD) _startPoint[2] = 0;
            }
        }
        //
        [Category("End point coordinates")]
        [OrderedDisplayName(0, 10, "Select the end point")]
        [DescriptionAttribute("Select the end point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 3)]
        public ItemSetData EndPointItemSet
        {
            get { return _endPointItemSetData; }
            set
            {
                if (value != _endPointItemSetData)
                    _endPointItemSetData = value;
            }
        }
        //
        [Category("End point coordinates")]
        [OrderedDisplayName(1, 10, "X")]
        [Description("X coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double X2 { get { return _endPoint[0]; } set { _endPoint[0] = value; } }
        //
        [Category("End point coordinates")]
        [OrderedDisplayName(2, 10, "Y")]
        [Description("Y coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double Y2 { get { return _endPoint[1]; } set { _endPoint[1] = value; } }
        //
        [Category("End point coordinates")]
        [OrderedDisplayName(3, 10, "Z")]
        [Description("Z coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double Z2
        {
            get { return _endPoint[2]; }
            set
            {
                _endPoint[2] = value;
                if (_twoD) _endPoint[2] = 0;
            }
        }


        // Constructors                                                                                                             
        public TranslateParameters(CaeModel.ModelSpaceEnum modelSpace)
        {
            Clear();
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            _startPointItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            _endPointItemSetData = new ItemSetData();   // needed to display ItemSetData.ToString()
            _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            //
            _dctd.RenameBooleanProperty(nameof(Copy), "Copy and translate", "Translate");
            //
            if (modelSpace == CaeModel.ModelSpaceEnum.Three_D) { _twoD = false; }
            else if (modelSpace == CaeModel.ModelSpaceEnum.Two_D)
            {
                _twoD = true;
                Z1 = 0;
                Z2 = 0;
                _dctd.GetProperty(nameof(Z1)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(Z2)).SetIsBrowsable(false);
            }
            else throw new NotSupportedException();
        }


        // Methods                                                                                                                  
        public void Clear()
        {
            _copy = false;
            _startPoint = new double[3];
            _endPoint = new double[] { 0, 0, 0};
            if (_twoD)
            {
                Z1 = 0;
                Z2 = 0;
            }
        }

    }
}

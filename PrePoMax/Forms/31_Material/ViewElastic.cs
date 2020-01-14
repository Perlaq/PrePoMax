﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;

namespace PrePoMax.PropertyViews
{
    [Serializable]
    public class ViewElastic : IViewMaterialProperty
    {
        // Variables                                                                                                                
        private CaeModel.Elastic _elastic;


        // Properties                                                                                                               
        [Browsable(false)]
        public string Name
        {
            get { return "Elastic"; }
        }

        [Browsable(false)]
        public CaeModel.MaterialProperty Base
        {
            get { return _elastic; }
        }

        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 2, "Young's modulus")]
        [DescriptionAttribute("The value of the Young's modulus.")]
        public double YoungsModulus { get { return _elastic.YoungsModulus; } set { _elastic.YoungsModulus = value; } }

        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 2, "Poisson's ratio")]
        [DescriptionAttribute("The value of the Poisson's ratio.")]
        public double PoissonsRatio { get { return _elastic.PoissonsRatio; } set { _elastic.PoissonsRatio = value; } }


        // Constructors                                                                                                             
        public ViewElastic(CaeModel.Elastic elastic)
        {
            _elastic = elastic;
        }

        // Methods                                                                                                                  
    }
}
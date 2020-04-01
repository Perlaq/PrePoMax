﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewHistoryOutput : ViewMultiRegion
    {
        // Variables                                                                                                                
        private string _selectionHidden;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the history output.")]
        public abstract string Name { get; set; }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Frequency")]
        [DescriptionAttribute("Integer N, which indicates that only results of every N-th increment will be stored.")]
        public abstract int Frequency { get; set; }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Region type")]
        [DescriptionAttribute("Select the region type which will be used for the section definition.")]
        public override string RegionType { get { return base.RegionType; } set { base.RegionType = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        public string SelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }


        // Constructors                                                                                                             


        // Methods
        public abstract CaeModel.HistoryOutput GetBase();
    }
}

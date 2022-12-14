using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;
using CaeGlobals;

namespace vtkControl
{
    [Serializable]
    public class vtkMaxColorSpectrum
    {
        // Variables                                                                                                                
        private vtkColorSpectrumType _type;
        private vtkColorSpectrumMinMaxType _minMaxType;
        private double _minUserValue;
        private double _maxUserValue;
        private System.Drawing.Color _minColor;
        private System.Drawing.Color _maxColor;
        private int _numberOfColors;


        // Properties                                                                                                               
        public vtkColorSpectrumType Type { get { return _type; } set { _type = value; } }
        public vtkColorSpectrumMinMaxType MinMaxType { get { return _minMaxType; } set { _minMaxType = value; } }
        public double MinUserValue
        {
            get { return _minUserValue; }
            set
            {
                if (value < _maxUserValue) _minUserValue = value;
                else throw new Exception("The min value must be smaller than the max value.");
            }
        }
        public double MaxUserValue
        {
            get { return _maxUserValue; }
            set
            {
                if (value > _minUserValue) _maxUserValue = value;
                else throw new Exception("The max value must be larger than the min value.");
            }
        }
        public System.Drawing.Color MinColor { get { return _minColor; } set { _minColor = value; } }
        public System.Drawing.Color MaxColor { get { return _maxColor; } set { _maxColor = value; } }
        public int NumberOfColors
        {
            get { return _numberOfColors; }
            set
            {
                _numberOfColors = value;
                if (_numberOfColors < 2) _numberOfColors = 2;
                if (_numberOfColors > 24) _numberOfColors = 24;
            }
        }


        // Constructors                                                                                                             
        public vtkMaxColorSpectrum()
        {
            _type = vtkColorSpectrumType.CoolWarm;
            _minMaxType = vtkColorSpectrumMinMaxType.Automatic;
            _minUserValue = 0;
            _maxUserValue = 1;
            _minColor = System.Drawing.Color.LightGray;
            _maxColor = System.Drawing.Color.DarkGray;
            _numberOfColors = 9;
        }


        // Methods                                                                                                                  
        public void DeepCopy(vtkMaxColorSpectrum source)
        {
            _type = source.Type;
            _minMaxType = source.MinMaxType;
            _minUserValue = source.MinUserValue;
            _maxUserValue = source.MaxUserValue;
            _minColor = source.MinColor;
            _maxColor = source.MaxColor;
            _numberOfColors = source.NumberOfColors;
        }

    }
}

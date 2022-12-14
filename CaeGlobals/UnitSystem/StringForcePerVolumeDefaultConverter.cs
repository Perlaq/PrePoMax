using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using UnitsNet.Units;
using UnitsNet;

namespace CaeGlobals
{
    public class StringForcePerVolumeDefaultConverter : TypeConverter
    {
        // Variables                                                                                                                
        protected static ForceUnit _forceUnit = ForceUnit.Newton;
        protected static VolumeUnit _volumeUnit = VolumeUnit.CubicMeter;
        //
        protected ArrayList values;
        protected string _default = "Default";
        protected static double _initialValue = 0;


        // Properties                                                                                                               
        public static string SetForceUnit { set { _forceUnit = Force.ParseUnit(value); } }
        public static string SetVolumeUnit { set { _volumeUnit = Volume.ParseUnit(value); } }
        public static string SetInitialValue { set { _initialValue = ConvertForcePerVolume(value); } }


        // Constructors                                                                                                             
        public StringForcePerVolumeDefaultConverter()
        {
            // Initializes the standard values list with defaults.
            values = new ArrayList(new double[] { double.NaN, _initialValue });
        }


        // Methods                                                                                                                  
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        // Returns a StandardValuesCollection of standard value objects.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Passes the local integer array.
            StandardValuesCollection svc = new StandardValuesCollection(values);
            return svc;
        }

        // Returns true for a sourceType of string to indicate that 
        // conversions from string to integer are supported. (The 
        // GetStandardValues method requires a string to native type 
        // conversion because the items in the drop-down list are 
        // translated to string.)
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            else return base.CanConvertFrom(context, sourceType);
        }

        // If the type of the value to convert is string, parses the string 
        // and returns the integer to set the value of the property to. 
        // This example first extends the integer array that supplies the 
        // standard values collection if the user-entered value is not 
        // already in the array.
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // Convert from string
            if (value is string valueString)
            {
                double valueDouble;
                if (String.Equals(value, _default)) valueDouble = double.NaN;
                else if (!double.TryParse(valueString, out valueDouble))
                {
                    valueDouble = ConvertForcePerVolume(valueString);
                }
                return valueDouble;
            }
            else return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            try
            {
                if (destinationType == typeof(string))
                {
                    if (value is double valueDouble)
                    {
                        if (double.IsNaN(valueDouble)) return _default;
                        else
                        {
                            return value + " " + Force.GetAbbreviation(_forceUnit) +
                                           "/" + Volume.GetAbbreviation(_volumeUnit);
                        }
                    }
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }
            catch
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
        //
        private static double ConvertForcePerVolume(string valueWithUnitString)
        {
            string error = "Unable to parse quantity. Expected the form \"{value} {unit abbreviation}" +
                           "\", such as \"5.5 m\". The spacing is optional.";
            valueWithUnitString = valueWithUnitString.Trim().Replace(" ", "");
            //
            string[] tmp = valueWithUnitString.Split('/');
            if (tmp.Length != 2) throw new FormatException(error);
            Force force = Force.Parse(tmp[0]).ToUnit(_forceUnit);
            //
            VolumeUnit volumeUnit = Volume.ParseUnit(tmp[1]);
            Volume volume = Volume.From(1, volumeUnit).ToUnit(_volumeUnit);
            double value = force.Value / volume.Value;
            return value;
        }
    }


}
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using UnitsNet;
using UnitsNet.Units;

namespace CaeGlobals
{
    public class StringEnergyConverter : TypeConverter
    {
        // Variables                                                                                                                
        protected static EnergyUnit _energyUnit = EnergyUnit.Joule;
        protected static string _inlb = "in·lb";


        // Properties                                                                                                               
        public static string SetUnit
        {
            set
            {
                if (value == _inlb) _energyUnit = (EnergyUnit)100;
                else { _energyUnit = Energy.ParseUnit(value); }
            }
        }


        // Constructors                                                                                                             
        public StringEnergyConverter()
        {
        }


        // Methods                                                                                                                  
        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            else return base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // Convert from string
            if (value is string valueString)
            {
                double valueDouble;
                //
                if (!double.TryParse(valueString, out valueDouble))
                {
                    double scale = 1;
                    valueString = valueString.Trim().Replace(" ", "");
                    // Check if it is given in unsupported units
                    if (valueString.Contains(_inlb))
                    {
                        valueString = valueString.Replace(_inlb, "ft·lb");
                        scale = 1.0 / 12.0;
                    }
                    // Check if it must be converted to unsupported units
                    if ((int)_energyUnit == 100)
                    {
                        Energy Energy = Energy.Parse(valueString).ToUnit(EnergyUnit.FootPound);
                        valueDouble = scale * Energy.Value * 12.0;
                    }
                    else
                    {
                        Energy Energy = Energy.Parse(valueString).ToUnit(_energyUnit);
                        valueDouble = scale * Energy.Value;
                    }
                }
                //
                return valueDouble;
            }
            else return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // Convert to string
            try
            {
                if (destinationType == typeof(string))
                {
                    if (value is double valueDouble)
                    {
                        if ((int)_energyUnit == 100) return value.ToString() + " " + _inlb;
                        else return value.ToString() + " " + Energy.GetAbbreviation(_energyUnit);
                    }
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }
            catch
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

    }

}
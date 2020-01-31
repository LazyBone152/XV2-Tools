using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Core.Input;

namespace EEPK_Organiser.Misc
{
    static public class InputValidation
    {
        /// <summary>
        /// Validates entered input and removes any non-numeric characters. 
        /// </summary>
        public static void InputValidator_QqAtt_IntUpDown(object sender, KeyEventArgs e)
        {
            IntegerUpDown _textBox = (IntegerUpDown)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        //Do nothing
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }


                }
                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    return;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    Int64 value = Convert.ToInt64(_textBox.Text);

                    if (value > 5)
                    {
                        _textBox.Text = "5";
                    }
                    else if (value < -5)
                    {
                        _textBox.Text = "-5";
                    }
                }
            }
        }

        public static void InputValidator_UInt8_99Max_IntUpDown(object sender, KeyEventArgs e)
        {
            IntegerUpDown _textBox = (IntegerUpDown)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }
                }

                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    _textBox.Text = String.Empty;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    //Limited to between -256 and 255.
                    int value = Convert.ToInt32(_textBox.Text);

                    if (value > 99)
                    {
                        _textBox.Text = "99";
                    }
                    else if (value < 0)
                    {
                        _textBox.Text = "0";
                    }
                }
            }
        }

        public static void InputValidator_UInt8_125Max_IntUpDown(object sender, KeyEventArgs e)
        {
            IntegerUpDown _textBox = (IntegerUpDown)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }
                }

                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    _textBox.Text = String.Empty;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    //Limited to between -256 and 255.
                    int value = Convert.ToInt32(_textBox.Text);

                    if (value > 125)
                    {
                        _textBox.Text = "125";
                    }
                    else if (value < 0)
                    {
                        _textBox.Text = "0";
                    }
                }
            }
        }


        public static void InputValidator_UInt8_IntUpDown(object sender, KeyEventArgs e)
        {
            IntegerUpDown _textBox = (IntegerUpDown)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }
                }

                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    _textBox.Text = String.Empty;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    //Limited to between -256 and 255.
                    int value = Convert.ToInt32(_textBox.Text);

                    if (value > 255)
                    {
                        _textBox.Text = "255";
                    }
                    else if (value < 0)
                    {
                        _textBox.Text = "0";
                    }
                }
            }
        }

        public static void InputValidator_UInt16_IntUpDown(object sender, KeyEventArgs e)
        {
            IntegerUpDown _textBox = (IntegerUpDown)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }


                }

                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    _textBox.Text = String.Empty;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    Int64 value = Convert.ToInt64(_textBox.Text);

                    if (value > UInt16.MaxValue)
                    {
                        _textBox.Text = UInt16.MaxValue.ToString();
                    }
                    else if (value < UInt16.MinValue)
                    {
                        _textBox.Text = UInt16.MinValue.ToString();
                    }
                }
            }
        }

        public static void InputValidator_UInt32IntUpDown(object sender, KeyEventArgs e)
        {
            IntegerUpDown _textBox = (IntegerUpDown)sender;
            
            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }
                }
                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    _textBox.Text = String.Empty;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    Int64 value = Convert.ToInt64(_textBox.Text);

                    if (value > UInt32.MaxValue)
                    {
                        _textBox.Text = UInt32.MaxValue.ToString();
                    }
                    else if (value < UInt32.MinValue)
                    {
                        _textBox.Text = UInt32.MinValue.ToString();
                    }
                }
            }
        }


        public static void InputValidator_Str_Size32(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;

            if (_textBox.Text.Length > 32)
            {
                _textBox.Text = _textBox.Text.Remove(32, _textBox.Text.Length - 32);
            }

        }

        public static void InputValidator_Str_Size64(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;

            if (_textBox.Text.Length > 64)
            {
                _textBox.Text = _textBox.Text.Remove(64, _textBox.Text.Length - 64);
            }

        }


        public static void InputValidator_RgbInt(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        //Do nothing
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }
                }
            }

            if(_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
            {
                _textBox.Text = "-0";
                return;
            }

            if(_textBox.Text.Length > 0)
            {
                
                //Range validation
                //Limited to between -256 and 255.
                int value = Convert.ToInt32(_textBox.Text);

                if (value > 255)
                {
                    _textBox.Text = "255";
                }
                else if (value < -256)
                {
                    _textBox.Text = "-256";
                }
            }
            
            

        }

        /// <summary>
        /// Validates entered input and removes any non-numeric characters. 
        /// </summary>
        public static void InputValidator_Int16(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        //Do nothing
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }
                    
                }

                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    return;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    Int64 value = Convert.ToInt64(_textBox.Text);

                    if (value > Int16.MaxValue)
                    {
                        _textBox.Text = Int16.MaxValue.ToString();
                    }
                    else if (value < Int16.MinValue)
                    {
                        _textBox.Text = Int16.MinValue.ToString();
                    }
                }
            }
        }

        public static void InputValidator_UInt16(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;
            

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }

                    
                }

                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    _textBox.Text = String.Empty;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    Int64 value = Convert.ToInt64(_textBox.Text);

                    if (value > UInt16.MaxValue)
                    {
                        _textBox.Text = UInt16.MaxValue.ToString();
                    }
                    else if (value < UInt16.MinValue)
                    {
                        _textBox.Text = UInt16.MinValue.ToString();
                    }
                }
            }
        }


        /// <summary>
        /// Validates entered input and removes any non-numeric characters. 
        /// </summary>
        public static void InputValidator_Int32(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        //Do nothing
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2) {
                        if (_textBox.Text[i + 1] == '-' && i == 0) {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }

                    
                }
                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    return;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    Int64 value = Convert.ToInt64(_textBox.Text);

                    if (value > Int32.MaxValue)
                    {
                        _textBox.Text = Int32.MaxValue.ToString();
                    }
                    else if (value < Int32.MinValue)
                    {
                        _textBox.Text = Int32.MinValue.ToString();
                    }
                }
            }
        }

        public static void InputValidator_UInt32(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }
                }
                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    _textBox.Text = String.Empty;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    Int64 value = Convert.ToInt64(_textBox.Text);

                    if (value > UInt32.MaxValue)
                    {
                        _textBox.Text = UInt32.MaxValue.ToString();
                    }
                    else if (value < UInt32.MinValue)
                    {
                        _textBox.Text = UInt32.MinValue.ToString();
                    }
                }
            }
        }

        public static void InputValidator_UInt8(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;

            if (_textBox.Text.Length > 0)
            {
                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '-' && i == 0)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }
                }

                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    _textBox.Text = String.Empty;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    //Limited to between -256 and 255.
                    int value = Convert.ToInt32(_textBox.Text);

                    if (value > 255)
                    {
                        _textBox.Text = "255";
                    }
                    else if (value < 0)
                    {
                        _textBox.Text = "0";
                    }
                }
            }
        }


        /// <summary>
        /// Validates entered input to float standard.
        /// </summary>
        public static void InputValidator_Float(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;
            bool hasDecimal = false;

            if (_textBox.Text.Length > 0)
            {
                for(int i = 0; i < _textBox.Text.Length; i++)
                {
                    if(char.IsLetter(_textBox.Text[i]) && _textBox.Text[i] != '.' && _textBox.Text[i] != '-' && !char.IsNumber(_textBox.Text[i]))
                    {
                        //Invalid character
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if(_textBox.Text[i] == '-' && i != 0)
                    {
                        //Minus sign only allowed at position 0
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text[i] == '.')
                    {
                        //Only one decimal allowed
                        if (hasDecimal)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                        else
                        {
                            hasDecimal = true;
                        }
                    }

                }


            }
        }


        /// <summary>
        /// If string is empty or has invalid input, it will be defaulted to "0", otherwise it will return it unchanged. Optionally a default value can be defined as the second argument.
        /// </summary>
        public static string InputValidator_DefaultIfEmpty(string input, string defaultValue = "0") {
            if (String.IsNullOrWhiteSpace(input) || input == "-" || input == ".")
            {
                return defaultValue;
            }
            else {
                return input;
            }

        }

        public static void InputValidator_Float_Old(object sender, EventArgs e)
        {
            TextBox _textBox = (TextBox)sender;

            if (_textBox.Text.Length > 0)
            {
                int decimalLocation = 0;
                int decimals = 0;

                for (int i = 0; i < _textBox.Text.Length; i++)
                {
                    if (_textBox.Text[i] == '.' && decimals == 0 && i != decimalLocation)
                    {
                        decimals++;
                    }
                    else if (_textBox.Text[i] == '-' && i == 0)
                    {
                        //TextBox has minus sign in correct position, so allow it (only one of these are allowed)
                        decimalLocation = 1;
                    }
                    else if (Char.IsNumber(_textBox.Text[i]) == false)
                    {
                        _textBox.Text = _textBox.Text.Remove(i, 1);
                    }
                    else if (_textBox.Text.Length >= i + 2)
                    {
                        if (_textBox.Text[i + 1] == '-' && i == 0)
                        {
                            _textBox.Text = _textBox.Text.Remove(i, 1);
                        }
                    }

                }

                if (_textBox.Text.Length == 1 && _textBox.Text[0] == '-')
                {
                    return;
                }

                if (_textBox.Text.Length > 0)
                {

                    //Range validation
                    double value = Convert.ToDouble(_textBox.Text);

                    if (value > float.MaxValue)
                    {
                        _textBox.Text = float.MaxValue.ToString();
                    }
                    else if (value < float.MinValue)
                    {
                        _textBox.Text = float.MinValue.ToString();
                    }
                }

            }
        }

    }
}

using System;

namespace LB_Mod_Installer.Binding
{
    public struct BindingValue
    {
        public Function Function { get; set; }
        public string[] Arguments { get; set; }

        public string GetArgument1()
        {
            if (Arguments != null)
            {
                if (Arguments.Length > 0)
                {
                    return Arguments[0].ToLower().Trim();
                }
            }

            return String.Empty;
        }

        public string GetArgument2()
        {
            if (Arguments != null)
            {
                if (Arguments.Length > 1)
                {
                    return Arguments[1].ToLower().Trim();
                }
            }

            return String.Empty;
        }

        public string GetArgument3()
        {
            if (Arguments != null)
            {
                if (Arguments.Length > 2)
                {
                    return Arguments[2].ToLower().Trim();
                }
            }

            return String.Empty;
        }

        public string GetArgument4()
        {
            if (Arguments != null)
            {
                if (Arguments.Length > 3)
                {
                    return Arguments[3].ToLower().Trim();
                }
            }

            return String.Empty;
        }

        public bool HasArgument(int argCount = 1)
        {
            if (Arguments != null)
            {
                if (Arguments.Length >= argCount)
                {
                    return true;
                }
            }
            return false;
        }

        public ErrorHandling GetErrorHandlingType()
        {
            if (Function == Function.Error)
            {
                string args = GetArgument1().ToLower();
                switch (args)
                {
                    case "skip":
                        return ErrorHandling.Skip;
                    case "stop":
                        return ErrorHandling.Stop;
                    case "usedefaultvalue":
                    case "default":
                    case "defaultvalue":
                        return ErrorHandling.UseDefaultValue;
                    default:
                        throw new Exception(String.Format("{0} is not a valid Error argument.", args));
                }

            }
            else
            {
                throw new Exception(String.Format("Function {0} cannot access the ErrorHandling type.", Function));
            }
        }
    }


}

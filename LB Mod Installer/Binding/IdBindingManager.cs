using LB_Mod_Installer.Installer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xv2CoreLib;
using Xv2CoreLib.BCS;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;

namespace LB_Mod_Installer.Binding
{

    public struct AliasValue
    {
        public string Alias { get; set; }
        public int ID { get; set; }
    }

    public struct BindingValue
    {
        public Function Function { get; set; }
        public string[] Arguments { get; set; }

        public string GetArgument1()
        {
            if(Arguments != null)
            {
                if(Arguments.Length > 0)
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

        public bool HasArgument(int arg = 1)
        {
            if(Arguments != null)
            {
                if(Arguments.Length >= arg)
                {
                    return true;
                }
            }
            return false;
        }

        public ErrorHandling GetErrorHandlingType()
        {
            if(Function == Function.Error)
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

    public enum Function
    {
        AutoID,
        SetAlias,
        AliasLink,
        SkillID1,
        SkillID2,
        CharaID,
        Error,
        DefaultValue,
        X2MSkillID1,
        X2MSkillID2,
        AutoPartSet,
        Format, 
        Increment,
        X2MSkillPath
    }

    public enum ErrorHandling
    {
        Skip,
        Stop,
        UseDefaultValue
    }

    public class IdBindingManager
    {
        public const int NullTokenInt = 1280070990;
        public const string NullTokenStr = "1280070990";

        private const string CUS_PATH = "system/custom_skill.cus";
        private const string CMS_PATH = "system/char_model_spec.cms";
        private const string HUM_BCS_PATH = "chara/HUM/HUM.bcs";
        private const string HUF_BCS_PATH = "chara/HUF/HUF.bcs";
        private const string MAM_BCS_PATH = "chara/MAM/MAM.bcs";
        private const string MAF_BCS_PATH = "chara/MAF/MAF.bcs";
        private const string FRI_BCS_PATH = "chara/FRI/FRI.bcs";
        private const string NMC_BCS_PATH = "chara/NMC/NMC.bcs";
        
        private enum CusIdType
        {
            ID1,
            ID2
        }

        public enum CusSkillType
        {
            Super,
            Ultimate,
            Evasive,
            Blast,
            Awoken
        }

        Install_NEW install;
        List<int> usedPartSets = new List<int>();
        private List<AliasValue> Aliases = new List<AliasValue>();
        private X2MHelper _X2MHelper = new X2MHelper();

        public IdBindingManager(Install_NEW install)
        {
            this.install = install;
        }


        #region Parse
        public string ParseInstallPath(string installPath, string sourcePath)
        {
            return ParseStringBinding<IInstallable>(installPath, $"Binding for FilePath entry. SourcePath is \"{sourcePath}\".", GeneralInfo.InstallerXml, null, null, false);
        }

        /// <summary>
        /// Parse the bindings on all string properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="installList">The entries that are being installed (that have the bindings).</param>
        /// <param name="binaryList">The entries that are in the binary list (that we are going to install into).</param>
        /// <param name="filePath">The file path. Used for tracking.</param>
        /// <param name="usedIDs">(Optional) A list of all IDs that are used. This overwrites the default behaviour of checking the Index property on installList and binaryList entries when calculating AutoIDs.</param>
        /// 
        public void ParseProperties<T>(IList<T> installList, IList<T> binaryList, string filePath, List<string> usedIDs = null) where T : IInstallable
        {
            if (installList == null) return;

            //Safeguard against there not being an existing binary file (very unlikely...)
            if (binaryList == null) binaryList = new List<T>();

            //Parse every single string on classes that implement IInstallable
            foreach (var installEntry in installList)
            {
                ParseProperties_RecursiveSingle(installEntry, filePath, installList, binaryList, usedIDs, true);
            }

            RemoveNullTokenEntries(installList);
        }

        private void ParseProperties_RecursiveList<T>(IEnumerable list, string filePath, IList<T> installList, IList<T> binaryList, List<string> usedIDs = null) where T : IInstallable
        {
            foreach (var obj in list)
            {
                if (obj != null)
                    ParseProperties_RecursiveSingle<T>(obj, filePath, installList, binaryList, usedIDs);
            }
        }

        private void ParseProperties_RecursiveSingle<T>(object obj, string filePath, IList<T> installList, IList<T> binaryList, List<string> usedIDs = null, bool allowAutoId = false) where T : IInstallable
        {
            //This property needs to have its props parsed.
            PropertyInfo[] childProps = obj.GetType().GetProperties();

            foreach (var childProp in childProps)
            {
                if (childProp.GetSetMethod() != null && childProp.GetGetMethod() != null)
                {
                    if (childProp.PropertyType == typeof(string))
                    {
                        var autoIdAttr = (BindingAutoId[])childProp.GetCustomAttributes(typeof(BindingAutoId), false);
                        var stringBindingAttr = (BindingString[])childProp.GetCustomAttributes(typeof(BindingString), false);
                        object value = childProp.GetValue(obj);

                        if (value != null)
                        {
                            if (stringBindingAttr.Length > 0)
                            {
                                //Has BindingString attribute.
                                childProp.SetValue(obj, ParseStringBinding((string)value, string.Format("{0}", childProp.Name), filePath, installList, binaryList, false, ushort.MaxValue, usedIDs).ToString());
                            }
                            else if (autoIdAttr.Length > 0)
                            {
                                //Has BindingAutoId attribute.
                                if (allowAutoId)
                                    childProp.SetValue(obj, ParseBinding((string)value, string.Format("{0}", childProp.Name), filePath, installList, binaryList, true, autoIdAttr[0].MaxId, usedIDs).ToString());
                            }
                            else
                            {
                                childProp.SetValue(obj, ParseBinding<T>((string)value, string.Format("{0}", childProp.Name), filePath, null, null, false, ushort.MaxValue, usedIDs).ToString());
                            }
                        }
                    }
                    else if (childProp.PropertyType.IsClass)
                    {
                        object value = childProp.GetValue(obj);
                        var bindingSubClassAtr = (BindingSubClass[])childProp.GetCustomAttributes(typeof(BindingSubClass), false);
                        var bindingSubListAtr = (BindingSubList[])childProp.GetCustomAttributes(typeof(BindingSubList), false);

                        if (bindingSubClassAtr.Length > 0 && value != null)
                        {
                            ParseProperties_RecursiveSingle<T>(value, filePath, installList, binaryList, usedIDs);
                        }
                        if (bindingSubListAtr.Length > 0 && value != null)
                        {
                            if (value is IEnumerable list)
                                ParseProperties_RecursiveList<T>(list, filePath, installList, binaryList, usedIDs);
                        }
                    }
                }
            }
        }

        #endregion

        #region Binding
        private string ParseStringBinding<T>(string str, string comment, string filePath, IEnumerable<T> entries1, IEnumerable<T> entries2, bool allowAutoId = true, ushort maxId = ushort.MaxValue, List<string> usedIds = null) where T : IInstallable
        {
            //New and improved binding processing method.
            //Now supports multiple bindings within a single string

            while (HasBinding(str))
            {
                int startPos = str.IndexOf('{');
                int endPos = str.IndexOf('}');

                string startStr = str.Substring(0, startPos);
                string endStr = str.Substring(endPos + 1);
                string binding = str.Substring(startPos, endPos - startPos + 1);

                binding = ParseBinding(binding, comment, filePath, entries1, entries2, allowAutoId, maxId, usedIds);

                if (HasBinding(binding))
                    throw new InvalidDataException($"ProcessStringBinding: unexpected result. Binding was not successfuly parsed (binding={binding}).");

                str = $"{startStr}{binding}{endStr}";
            }

            return str;
        }

        private string ParseBinding<T>(string binding, string comment, string filePath, IEnumerable<T> entries1, IEnumerable<T> entries2, bool allowAutoId = true, ushort maxId = ushort.MaxValue, List<string> usedIds = null) where T : IInstallable
        {
            if (IsBinding(binding))
            {
                string originalBinding = binding;
                string Path = String.Empty;
                int ID = -1;
                int defaultValue = 0;
          
                List<BindingValue> bindings = ProcessBinding(binding, comment, originalBinding);
                bindings = ValidateBindings(bindings, comment, originalBinding);

                ErrorHandling errorHandler = ErrorHandling.Stop;
                string formating = "0";
                int increment = 0;

                foreach (var b in bindings)
                {
                    switch (b.Function)
                    {
                        case Function.Format:
                            formating = b.GetArgument1();
                            break;
                        case Function.SetAlias:
                            Aliases.Add(new AliasValue() { ID = ID + increment, Alias = b.GetArgument1() });
                            break;
                        case Function.AliasLink:
                            ID = GetAliasId(b.GetArgument1(), comment);
                            break;
                        case Function.SkillID1:
                            {
                                CusSkillType skillType = GetSkillType(b.GetArgument2());
                                ID = GetSkillId(CusIdType.ID1, skillType, b.GetArgument1());
                                break;
                            }
                        case Function.SkillID2:
                            {
                                CusSkillType skillType = GetSkillType(b.GetArgument2());
                                ID = GetSkillId(CusIdType.ID2, skillType, b.GetArgument1());
                                break;
                            }
                        case Function.CharaID:
                            ID = GetCharaId(b.GetArgument1());
                            break;
                        case Function.AutoID:
                            if (!allowAutoId) throw new Exception(String.Format("The AutoID binding function is not available for this value. ({0})", comment));

                            int minIndex = (!String.IsNullOrWhiteSpace(b.GetArgument1())) ? int.Parse(b.GetArgument1()) : 0;
                            int maxIndex = (!String.IsNullOrWhiteSpace(b.GetArgument2())) ? int.Parse(b.GetArgument2()) : maxId;
                            if (maxIndex > maxId) maxIndex = maxId; //If maxIndex (declared in binding) is greater than maxId (declared on Property), then set maxIndex to maxId (which is the highest possible value)

                            int nextID = GetUnusedIndex(entries1, entries2, minIndex, maxIndex, usedIds);

                            if (nextID == NullTokenInt && errorHandler == ErrorHandling.Stop)
                            {
                                GeneralInfo.SpecialFailState = GeneralInfo.SpecialFailStates.AutoIdBindingFailed;
                                throw new Exception(String.Format("An ID could not be allocated in {2}. Install failed. \n\nBinding: {1}\nProperty: {0}", comment, binding, filePath));
                            }

                            ID = nextID;
                            break;
                        case Function.Error:
                            errorHandler = b.GetErrorHandlingType();
                            break;
                        case Function.DefaultValue:
                            defaultValue = int.Parse(b.GetArgument1());
                            break;
                        case Function.X2MSkillID1:
                            {
                                CusSkillType skillType = GetSkillType(b.GetArgument2());
                                int id1 = _X2MHelper.GetX2MSkillID1(b.GetArgument1(), skillType);

                                if (id1 == NullTokenInt && errorHandler == ErrorHandling.Stop)
                                {
                                    GeneralInfo.SpecialFailState = GeneralInfo.SpecialFailStates.X2MNotFound;
                                    throw new Exception(String.Format("Required X2M skill not found. Install failed. \nBinding: {1}\n({0})", comment, binding, filePath));
                                }

                                ID = id1;
                                break;
                            }
                        case Function.X2MSkillID2:
                            {
                                CusSkillType skillType = GetSkillType(b.GetArgument2());
                                int id2 = _X2MHelper.GetX2MSkillID2(b.GetArgument1(), skillType);

                                if (id2 == NullTokenInt && errorHandler == ErrorHandling.Stop)
                                {
                                    GeneralInfo.SpecialFailState = GeneralInfo.SpecialFailStates.X2MNotFound;
                                    throw new Exception(String.Format("Required X2M skill not found. Install failed. \nBinding: {1}\n({0})", comment, binding, filePath));
                                }

                                ID = id2;
                                break;
                            }
                        case Function.AutoPartSet:
                            {
                                if (!allowAutoId) throw new Exception(String.Format("The AutoPartSet binding function is not available for this value. ({0})", comment));
                                int min = (b.HasArgument(1)) ? int.Parse(b.GetArgument1()) : 0;
                                int max = (b.HasArgument(2)) ? int.Parse(b.GetArgument2()) : 999;
                                int nextId = GetFreePartSet(min, max);

                                if (nextId == NullTokenInt && errorHandler == ErrorHandling.Stop)
                                {
                                    GeneralInfo.SpecialFailState = GeneralInfo.SpecialFailStates.AutoIdBindingFailed;
                                    throw new Exception(String.Format("A PartSet ID could not be allocated in {2}. Install failed. \n\nBinding: {1}\nProperty: {0}", comment, binding, filePath));
                                }

                                ID = nextId;
                            }
                            break;
                        case Function.Increment:
                            if (!b.HasArgument()) throw new Exception($"No argument found on Increment binding!");

                            if(!int.TryParse(b.GetArgument1(), out increment))
                                throw new Exception($"Error while parsing the argument on Increment binding. (Binding: {binding})");
                            
                            break;
                        case Function.X2MSkillPath:

                            {
                                CusSkillType skillType = GetSkillType(b.GetArgument2());
                                string x2mSkillPath = _X2MHelper.GetX2MSkillPath(b.GetArgument1(), skillType);

                                if (x2mSkillPath == NullTokenStr && errorHandler == ErrorHandling.Stop)
                                {
                                    GeneralInfo.SpecialFailState = GeneralInfo.SpecialFailStates.X2MNotFound;
                                    throw new Exception(String.Format("Required X2M skill not found. Install failed. \nBinding: {1}\n({0})", comment, binding, filePath));
                                }

                                Path = x2mSkillPath;
                                break;
                            }
                    }
                }

                //Generic error handling code
                if (ID == NullTokenInt && errorHandler == ErrorHandling.Stop)
                {
                    GeneralInfo.SpecialFailState = GeneralInfo.SpecialFailStates.IdBindingFailed;
                    throw new Exception(String.Format("An ID could not be assigned according to the binding. Install failed. \nBinding: {1}\n({0})", comment, binding));
                }
                else if(ID == NullTokenInt && errorHandler == ErrorHandling.UseDefaultValue)
                {
                    ID = defaultValue;
                }
                // Path wasn't used
                if (Path == String.Empty)
                    return ApplyFormatting(ID + increment, formating);
                else  //a special case for returning a Path to use in installPath
                    return Path;
            }
            else
            {
                //Not a binding.
                return binding;
            }
        }

        private List<BindingValue> ProcessBinding(string binding, string comment, string originalBinding)
        {
            //Remove spaces
            binding = binding.Trim(' ');

            //Bracket validation
            if (binding[0] != '{') throw new FormatException(String.Format("Cannot find the opening bracket on the binding \"{0}\"\n({1})", originalBinding, comment));
            if (binding[binding.Length - 1] != '}') throw new FormatException(String.Format("Cannot find the closing bracket on the binding \"{0}\"\n({1})", originalBinding, comment));
            if(binding.Count(f => f == '{') > 1) throw new FormatException(String.Format("More than one opening bracket was found on the binding \"{0}\"\n({1})", originalBinding, comment));
            if (binding.Count(f => f == '}') > 1) throw new FormatException(String.Format("More than one closing bracket was found on the binding \"{0}\"\n({1})", originalBinding, comment));

            //Remove brackets
            binding = binding.Trim('{', '}');

            //Parse the bindings
            List<BindingValue> bindings = new List<BindingValue>();
            string[] splitBindings = binding.Split(',');//Regex.Split(binding, @"(?<!,[^(]+\([^)]+),");

            if (splitBindings.Length == 0) throw new FormatException(String.Format("Invalid binding: {0}\n({1})", originalBinding, comment));

            for (int i = 0; i < splitBindings.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(splitBindings[i])) throw new FormatException(String.Format("Param {2} is invalid on binding {0}.\n({1})", originalBinding, comment, i));
                //Split the function up
                var _functionSplit = splitBindings[i].Split('=');
                string function = _functionSplit[0];
                string argument = String.Empty;
                if (_functionSplit.Length == 2) argument = _functionSplit[1];
                string[] arguments = argument.Trim().Trim('(', ')').Trim().ToLower().Split(';');

                //Validation
                if (_functionSplit.Length > 2) throw new FormatException(String.Format("Invalid binding argument: {0} (Full binding: {1})\n({1})", splitBindings[i], originalBinding, comment));

                //To lowercase
                function = function.ToLower().Trim(' ');
                argument = argument.ToLower().Trim(' ');

                switch (function)
                {
                    case "autoid":
                        bindings.Add(new BindingValue() {  Function = Function.AutoID, Arguments = arguments });
                        break;
                    case "setalias":
                        bindings.Add(new BindingValue() { Function = Function.SetAlias, Arguments = arguments });
                        break;
                    case "aliaslink":
                        bindings.Add(new BindingValue() { Function = Function.AliasLink, Arguments = arguments });
                        break;
                    case "skillid1":
                        bindings.Add(new BindingValue() { Function = Function.SkillID1, Arguments = arguments });
                        break;
                    case "skillid2":
                        bindings.Add(new BindingValue() { Function = Function.SkillID2, Arguments = arguments });
                        break;
                    case "charaid":
                        bindings.Add(new BindingValue() { Function = Function.CharaID, Arguments = arguments });
                        break;
                    case "error":
                        bindings.Add(new BindingValue() { Function = Function.Error, Arguments = arguments });
                        break;
                    case "defaultvalue":
                        bindings.Add(new BindingValue() { Function = Function.DefaultValue, Arguments = arguments });
                        break;
                    case "x2mskillid1":
                        bindings.Add(new BindingValue() { Function = Function.X2MSkillID1, Arguments = arguments });
                        break;
                    case "x2mskillid2":
                        bindings.Add(new BindingValue() { Function = Function.X2MSkillID2, Arguments = arguments });
                        break;
                    case "autopartset":
                        bindings.Add(new BindingValue() { Function = Function.AutoPartSet, Arguments = arguments });
                        break;
                    case "format":
                        bindings.Add(new BindingValue() { Function = Function.Format, Arguments = arguments });
                        break;
                    case "increment":
                        bindings.Add(new BindingValue() { Function = Function.Increment, Arguments = arguments });
                        break;
                    case "x2mskillpath":
                        bindings.Add(new BindingValue() { Function = Function.X2MSkillPath, Arguments = arguments });
                        break;

                    default:
                        throw new FormatException(String.Format("Invalid ID Binding Function (Function = {0}, Argument = {1})\nFull binding: {2}", function, argument, originalBinding));
                }


            }

            return bindings;
        }

        private List<BindingValue> ValidateBindings(List<BindingValue> bindings, string comment, string originalBinding)
        {
            //Ensures the bindings are valid, and orders them correctly so the alias function comes last (if present)

            //Entries must be ordered like this: Error > ID > Increment > Alias
            MoveFunctionToStart(bindings, Function.Error);
            MoveFunctionToLast(bindings, Function.Increment);
            MoveFunctionToLast(bindings, Function.SetAlias);

            //Validate functions
            bool hasIdBinding = false;
            bool hasAliasBinding = false;
            bool hasErrorBinding = false;
            bool hasDefaultValueBinding = false;
            bool hasFormatBinding = false;
            bool hasIncrementBinding = false;

            for (int i = 0; i < bindings.Count; i++)
            {
                switch (bindings[i].Function)
                {
                    case Function.SetAlias:
                        if (hasAliasBinding) throw new Exception(String.Format("More than one instance of {0} found. Binding parse failed.\n({1})", Function.SetAlias, comment));
                        hasAliasBinding = true;
                        break;
                    case Function.Error:
                        if (hasErrorBinding) throw new Exception(String.Format("More than one instance of {0} found. Binding parse failed.\n({1})", Function.Error, comment));
                        hasErrorBinding = true;
                        break;
                    case Function.DefaultValue:
                        if (hasDefaultValueBinding) throw new Exception(String.Format("More than one instance of {0} found. Binding parse failed.\n({1})", Function.DefaultValue, comment));
                        hasDefaultValueBinding = true;
                        break;
                    case Function.Format:
                        if (hasFormatBinding) throw new Exception(String.Format("More than one instance of {0} found. Binding parse failed.\n({1})", Function.Format, comment));
                        hasFormatBinding = true;
                        break;
                    case Function.Increment:
                        if (hasIncrementBinding) throw new Exception(String.Format("More than one instance of {0} found. Binding parse failed.\n({1})", Function.Increment, comment));
                        hasIncrementBinding = true;
                        break;
                    default:
                        if (hasIdBinding) throw new Exception(String.Format("More than one instance of an ID binding found within the same binding. Binding parse failed.\n({0})", comment));
                        hasIdBinding = true;
                        break;
                }
            }

            if (hasIdBinding == false && hasAliasBinding == true) throw new Exception("The SetAlias binding cannot be used without also a ID binding.");
            if (hasIdBinding == false && hasErrorBinding == true) throw new Exception("The Error binding cannot be used without also a ID binding.");

            //Validate arguments
            for (int i = 0; i < bindings.Count; i++)
            {
                switch (bindings[i].Function)
                {
                    case Function.AutoID:
                        //Can have no arguments or have arguments
                        break;
                    case Function.AliasLink:
                    case Function.SetAlias:
                    case Function.CharaID:
                    case Function.Error:
                    case Function.DefaultValue:
                        //Must have an argument
                        if (!bindings[i].HasArgument()) throw new Exception(String.Format("The {0} binding function takes a string argument, but none was found.\n({1})", bindings[i].Function, comment));
                        break;
                    case Function.SkillID1:
                    case Function.SkillID2:
                    case Function.X2MSkillID1:
                    case Function.X2MSkillID2:
                    case Function.X2MSkillPath:
                        if (bindings[i].Arguments.Length < 2)
                        {
                            throw new Exception(String.Format("The {0} binding function takes 2 string arguments, but only {1} were found. \n({2})", bindings[i].Function, bindings[i].Arguments.Length, comment));
                        }
                        break;
                }

            }

            return bindings;
        }


        //Helpers
        private bool IsBinding(string binding)
        {
            if (string.IsNullOrWhiteSpace(binding)) return false;
            binding = binding.Trim();
            if (binding[0] == '{' || binding[binding.Length - 1] == '}') return true;
            return false;
        }

        private bool HasBinding(string binding)
        {
            if (string.IsNullOrWhiteSpace(binding)) return false;
            return (binding.Contains('{') && binding.Contains('}'));
        }

        private void MoveFunctionToLast(List<BindingValue> bindings, Function func)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].Function == func)
                {
                    var function = bindings[i];
                    bindings.RemoveAt(i);
                    bindings.Add(function);
                    break;
                }
            }
        }

        private void MoveFunctionToStart(List<BindingValue> bindings, Function func)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].Function == func)
                {
                    var error = bindings[i];
                    bindings.RemoveAt(i);
                    bindings.Insert(0, error);
                    break;
                }
            }
        }


        #endregion

        #region Function Helpers
        private int GetAliasId(string alias, string comment)
        {
            foreach(var a in Aliases)
            {
                if (a.Alias == alias) return a.ID;
            }

            throw new Exception(String.Format("Could not find the alias: {0}. Binding parse failed. ({1})", alias, comment));
        }

        /// <summary>
        /// Tries to find a unused ID betwen min and max within the two specified lists. Returns the NullToken if an ID cannot be allocated.
        /// </summary>
        /// <returns></returns>
        public int GetUnusedIndex<T>(IEnumerable<T> entries1, IEnumerable<T> entries2, int minIndex = 0, int maxIndex = -1, List<string> usedIds = null) where T : IInstallable
        {
            //todo: review this code
            if (maxIndex == -1) maxIndex = UInt16.MaxValue - 1;
            List<int> UsedIndexes = new List<int>();

            //Create UsedIndexes
            if(usedIds != null)
            {
                foreach (var usedId in usedIds)
                {
                    int value = 0;
                    if (int.TryParse(usedId, out value))
                    {
                        UsedIndexes.Add(value);
                    }
                }
            }
            else
            {
                if (entries1 != null)
                {
                    foreach (var e in entries1)
                    {
                        try
                        {
                            UsedIndexes.Add(e.SortID);
                        }
                        catch
                        {

                        }
                    }
                }

                if (entries2 != null)
                {
                    foreach (var e in entries2)
                    {
                        try
                        {
                            UsedIndexes.Add(e.SortID);
                        }
                        catch
                        {

                        }
                    }
                }

            }
            

            //Create UnusedIndexes
            int idx = 0;
            while (true)
            {
                //If maxIndex has been reached
                if (idx > maxIndex && maxIndex != -1)
                {
                    return NullTokenInt;
                }

                //If the index is not used the idx is greater than minIndex
                if (!UsedIndexes.Contains(idx) && idx >= minIndex)
                {
                    if(usedIds != null)
                    {
                        usedIds.Add(idx.ToString());
                    }
                    return idx;
                }
                idx++;
                if (idx > ushort.MaxValue) return -1; //Safe-guard code (very unlikely case)
            }
        }
        
        private CusSkillType GetSkillType(string argument)
        {
            switch (argument.ToLower())
            {
                case "super":
                    return CusSkillType.Super;
                case "ultimate":
                    return CusSkillType.Ultimate;
                case "evasive":
                    return CusSkillType.Evasive;
                case "blast":
                    return CusSkillType.Blast;
                case "awoken":
                    return CusSkillType.Awoken;
                default:
                    return CusSkillType.Super;
            }
        }
        
        private int GetSkillId(CusIdType idType, CusSkillType skillType, string shortName)
        {
            CUS_File cusFile = (CUS_File)install.GetParsedFile<CUS_File>(CUS_PATH);

            List<Skill> skills = null;
            switch (skillType)
            {
                case CusSkillType.Super:
                    skills = cusFile.SuperSkills;
                    break;
                case CusSkillType.Ultimate:
                    skills = cusFile.UltimateSkills;
                    break;
                case CusSkillType.Evasive:
                    skills = cusFile.EvasiveSkills;
                    break;
                case CusSkillType.Blast:
                    skills = cusFile.BlastSkills;
                    break;
                case CusSkillType.Awoken:
                    skills = cusFile.AwokenSkills;
                    break;
            }

            foreach(var skill in skills)
            {
                if(skill.Str_00.ToLower() == shortName)
                {
                    switch (idType)
                    {
                        case CusIdType.ID1:
                            return int.Parse(skill.Index);
                        case CusIdType.ID2:
                            return int.Parse(skill.I_10);
                    }
                }
            }

            return NullTokenInt; //Skill wasn't found
        }

        private int GetCharaId(string shortName)
        {
            CMS_File cmsFile = (CMS_File)install.GetParsedFile<CMS_File>(CMS_PATH);

            foreach (var chara in cmsFile.CMS_Entries)
            {
                if (chara.Str_04.ToLower() == shortName) return int.Parse(chara.Index);
            }

            return NullTokenInt; //Chara wasn't found, so defaulting to 0
        }

        private int GetFreePartSet(int min, int max)
        {
            int id = min;

            while (IsPartSetUsed(id) && id < max)
            {
                id++;
            }

            if (IsPartSetUsed(id))
                return NullTokenInt;

            usedPartSets.Add(id);
            return id;
        }

        private bool IsPartSetUsed(int partSet)
        {
            if (usedPartSets.Contains(partSet)) return true;
            if (((BCS_File)install.GetParsedFile<BCS_File>(HUM_BCS_PATH)).PartSets.Any(x => x.ID == partSet)) return true;
            if (((BCS_File)install.GetParsedFile<BCS_File>(HUF_BCS_PATH)).PartSets.Any(x => x.ID == partSet)) return true;
            if (((BCS_File)install.GetParsedFile<BCS_File>(MAM_BCS_PATH)).PartSets.Any(x => x.ID == partSet)) return true;
            if (((BCS_File)install.GetParsedFile<BCS_File>(MAF_BCS_PATH)).PartSets.Any(x => x.ID == partSet)) return true;
            if (((BCS_File)install.GetParsedFile<BCS_File>(FRI_BCS_PATH)).PartSets.Any(x => x.ID == partSet)) return true;
            if (((BCS_File)install.GetParsedFile<BCS_File>(NMC_BCS_PATH)).PartSets.Any(x => x.ID == partSet)) return true;
            return false;
        }
        
        private string ApplyFormatting(int value, string formatting)
        {
            switch (formatting)
            {
                case "0":
                    return value.ToString();
                case "1":
                    return value.ToString("D1");
                case "2":
                    return value.ToString("D2");
                case "3":
                    return value.ToString("D3");
                case "4":
                    return value.ToString("D4");
                case "5":
                    return value.ToString("D5");
                case "6":
                    return value.ToString("D6");
                case "7":
                    return value.ToString("D7");
                case "8":
                    return value.ToString("D8");
                case "9":
                    return value.ToString("D9");
                case "10":
                    return value.ToString("D10");
                default:
                    throw new Exception("ApplyFormatting: Invalid formating. Please use a number between 1 and 10.");
            }
        }


        #endregion

        #region Misc

        /// <summary>
        /// Removes all entries that has a NullToken (caused by a failed AutoID or X2M binding).
        /// </summary>
        private void RemoveNullTokenEntries<T>(IList<T> installList) where T : IInstallable
        {
            List<T> toDelete = new List<T>();

            foreach (var e in installList)
            {
                if (HasNullToken(e)) toDelete.Add(e);
            }

            foreach (var e in toDelete)
            {
                installList.Remove(e);
            }

            //installList.RemoveAll(e => HasNullToken(e));
        }

        private bool HasNullToken<T>(T entry) where T : IInstallable
        {
            PropertyInfo[] properties = entry.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(string))
                {
                    if ((string)prop.GetValue(entry) == NullTokenStr)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

    }


}

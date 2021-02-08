using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.Resource.UndoRedo
{
    /// <summary>
    /// Invokes methods on an object when Undo or Redo is called.
    /// </summary>
    public class UndoActionDelegate : IUndoRedo
    {
        public string Message { get; set; }
        public bool doLast { get; set; }

        private object instance;
        private string undoFunctionName;
        private string redoFunctionName;
        private object[] args = new object[0];

        Type objType;
        bool isStatic = false;

        public UndoActionDelegate(object _instance, string _undoFunctionName, string _redoFunctionName, bool _doLast, string _message = "", object[] args = null)
        {
            instance = _instance;
            undoFunctionName = _undoFunctionName;
            redoFunctionName = _redoFunctionName;
            Message = _message;
            doLast = _doLast;

            if (args != null)
                this.args = args;
        }

        public UndoActionDelegate(object _instance, string _functionName, bool _doLast, string _message = "", object[] args = null)
        {
            instance = _instance;
            undoFunctionName = _functionName;
            redoFunctionName = _functionName;
            Message = _message;
            doLast = _doLast;

            if (args != null)
                this.args = args;
        }

        /// <summary>
        /// Call a static method.
        /// </summary>
        public UndoActionDelegate(Type type, string _functionName, bool _doLast, string _message = "", object[] args = null)
        {
            objType = type;
            isStatic = true;
            undoFunctionName = _functionName;
            redoFunctionName = _functionName;
            Message = _message;
            doLast = _doLast;

            if (args != null)
                this.args = args;
        }



        public void Undo()
        {
            if (isStatic)
            {
                objType?.GetMethod(undoFunctionName)?.Invoke(null, args);
            }
            else
            {
                instance.GetType()?.GetMethod(undoFunctionName)?.Invoke(instance, args);
            }
        }

        public void Redo()
        {
            if (isStatic)
            {
                objType?.GetMethod(redoFunctionName)?.Invoke(instance, args);
            }
            else
            {
                instance.GetType()?.GetMethod(redoFunctionName)?.Invoke(instance, args);
            }
        }
    }
}

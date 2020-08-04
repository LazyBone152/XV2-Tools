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

        public UndoActionDelegate(object _instance, string _undoFunctionName, string _redoFunctionName, bool _doLast, string _message = "")
        {
            instance = _instance;
            undoFunctionName = _undoFunctionName;
            redoFunctionName = _redoFunctionName;
            Message = _message;
            doLast = _doLast;
        }

        public UndoActionDelegate(object _instance, string _functionName, bool _doLast, string _message = "")
        {
            instance = _instance;
            undoFunctionName = _functionName;
            redoFunctionName = _functionName;
            Message = _message;
            doLast = _doLast;
        }

        public void Undo()
        {
            instance.GetType()?.GetMethod(undoFunctionName)?.Invoke(instance, new object[0]);
        }

        public void Redo()
        {
            instance.GetType()?.GetMethod(redoFunctionName)?.Invoke(instance, new object[0]);
        }
    }
}

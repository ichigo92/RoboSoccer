using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using SSLRig.Core.Infrastructure.Communication;
using System.Windows.Forms;

namespace SSLRig.RigUI.Common
{
    public static class DialogMap
    {
        public static Type GetMappedDialog(Type obj)
        {
            try
            {
                string dialogType = ConfigurationManager.AppSettings.Get(obj.AssemblyQualifiedName);
                return Type.GetType(dialogType);
            }
            catch
            {
                return null;
            }

        }

        public static void MapDialog(Type obj, Type dialog)
        {
            ConfigurationManager.AppSettings.Set(obj.AssemblyQualifiedName, dialog.AssemblyQualifiedName);
        }

        public static void RegisterDialogs()
        {
            //MapDialog(typeof(GRSimSender), typeof(DlgGRSimConfig));
            //MapDialog(typeof(XBeeSender), typeof(DlgXBeeConfig));
            //MapDialog(typeof(SSLVisionReceiver), typeof(DlgSSLVisionConfig));
        }
    }

    public static class Interfacing
    {
        private static IEnumerable<Type> TypesImplementingInterface(Type desiredType)
        {
            return AppDomain
                   .CurrentDomain
                   .GetAssemblies()
                   .SelectMany(assembly => assembly.GetTypes())
                   .Where(type => desiredType.IsAssignableFrom(type));
        }

        private static bool IsRealClass(Type testType)
        {
            return !(testType.IsAbstract || testType.IsGenericTypeDefinition || testType.IsInterface);
        }

        public static List<Type> GetImplementations(Type desiredType)
        {
            List<Type> typeList = null;
            IEnumerator<Type> typeEnumerator = TypesImplementingInterface(desiredType).GetEnumerator();
            if (typeEnumerator != null)
            {
                typeList = new List<Type>();
                while (typeEnumerator.MoveNext())
                {
                    if (IsRealClass(typeEnumerator.Current))
                        typeList.Add(typeEnumerator.Current);
                }
            }
            return typeList;
        }
    }

    public static class MessageBoxes
    {
        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ShowInfo(string message, string Title)
        {
            MessageBox.Show(message, Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

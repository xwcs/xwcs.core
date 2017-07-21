using DevExpress.XtraBars.Docking2010.Views.Widget;
using System.Reflection;


namespace xwcs.core
{
    public static class WidgetViewExtensions
    {
        public static MethodInfo GetMoveCoreInfo(this WidgetView view)
        {
            IDocumentGroup ig = view.DocumentGroup;
            return (ig.Items.GetType().GetMethod("MoveCore", BindingFlags.Instance | BindingFlags.NonPublic));
        }
        public static void MoveDocument(this WidgetView view, Document document, int index)
        {
            MethodInfo method = view.GetMoveCoreInfo();
            method.Invoke(view.DocumentGroup.Items, new object[] { index, document });
        }
    }
}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Plugin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal class MinMax : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document document = UIdoc.Document;

            var selectedElementID = UIdoc.Selection.GetElementIds().First();
            if (selectedElementID == null) { return Result.Succeeded; }

            Element selectedElement = document.GetElement(selectedElementID);
            XYZ min, max;
            try
            {
                var boundingBox = selectedElement.get_BoundingBox(document.ActiveView);
                min = boundingBox.Min;
                max = boundingBox.Max;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Result.Failed;
            }
            string result = string.Format("Min:{0} \n Max:{1}", min.ToString(), max.ToString());
            MessageBox.Show(result, "Результат");

            return Result.Succeeded;
        }
    }
}

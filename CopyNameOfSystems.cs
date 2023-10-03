using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Windows;

namespace Plugin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal class CopyNameOfSystems : IExternalCommand
    {
        static Document document;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            document = commandData.Application.ActiveUIDocument.Document;

            var airDucts = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_DuctCurves).WhereElementIsNotElementType().ToArray();
            var ductAccessorys = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_DuctAccessory).WhereElementIsNotElementType().ToArray();
            var ductTerminals = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_DuctTerminal).WhereElementIsNotElementType().ToArray();
            var flexDuctCurves = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_FlexDuctCurves).WhereElementIsNotElementType().ToArray();
            var mechanicalEquipment = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_MechanicalEquipment).WhereElementIsNotElementType().ToArray();
            var ductFittings = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_DuctFitting).WhereElementIsNotElementType().ToArray();
            var ductInsulations = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_DuctInsulations).WhereElementIsNotElementType().ToArray();

            SetName(airDucts);
            SetName(ductAccessorys);
            SetName(ductTerminals);
            SetName(flexDuctCurves);
            SetNameMechanicalEquipment(mechanicalEquipment);
            SetName(ductFittings);
            SetName(ductInsulations);

            return Result.Succeeded;
        }

        private void SetName(Element[] elements)
        {
            Transaction transaction = new Transaction(document, "Set Name");
            transaction.Start();
            for (int i = 0; i < elements.Length; i++)
            {
                string newName = elements[i].get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString();
                elements[i].ParametersMap.get_Item("ИмяСистемы").Set(newName);
            }
            transaction.Commit();
        }

        private void SetNameMechanicalEquipment(Element[] elements)
        {
            Transaction transaction = new Transaction(document, "Set Name");
            transaction.Start();
            foreach( Element element in elements )
            {
                string currentName = element.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM).AsString();
                var splitedName = currentName.Split(' ');
                string newName = currentName.Substring(0, currentName.Length - splitedName.Last().Length - 1);
                element.ParametersMap.get_Item("ИмяСистемы").Set(newName);
            }
            transaction.Commit();
        }
    }
}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugin
{

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AnnotationRunner : IExternalCommand
    {
        static Document doc;
        static UIDocument uidoc;

        static List<XYZ> saveEnd, saveElbow;
        static List<FamilySymbol> familySymbols;
        static List<int> symbolsLengths;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Initialize();

            uidoc = commandData.Application.ActiveUIDocument;
            doc = uidoc.Document;
            var tags = GetSelectedTags();
            SavePosition(tags);
            Transaction transaction = new Transaction(doc, "Shuffle");
            transaction.Start();
            ZerificateElements(tags);
            transaction.Commit();
            var lengths = MeasureElements(tags);

            transaction.Start();
            ReturnElements(tags);
            transaction.Commit();

            transaction.Start();
            MakeChanges(tags, lengths);
            transaction.Commit();

            return Result.Succeeded;
        }

        static public void Initialize()
        {
            saveEnd = new List<XYZ>();
            saveElbow = new List<XYZ>();
            familySymbols = new List<FamilySymbol>();
            symbolsLengths = new List<int>();
        }

        static public List<IndependentTag> GetSelectedTags()
        {
            var result = new List<IndependentTag>();
            var elementsIds = uidoc.Selection.GetElementIds();
            foreach (ElementId elementId in elementsIds)
            {
                var newElement = doc.GetElement(elementId);
                if (newElement.GetType() == typeof(IndependentTag))
                    result.Add(newElement as IndependentTag);
            }
            return result;
        }

        static public void SavePosition(List<IndependentTag> tags)
        {
            var familySymbolIds = new List<ElementId>();
            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (tag.ParametersMap.get_Item("Тип выноски").AsValueString() == "С присоединенным концом")
                    saveEnd.Add(null);
                else
                    saveEnd.Add(tag.LeaderEnd);

                if (tag.HasElbow)
                    saveElbow.Add(tag.LeaderElbow);
                else
                    saveElbow.Add(null);
                familySymbolIds.Add(tag.GetTypeId());
            }

            familySymbolIds = familySymbolIds.Distinct().ToList();
            for (int i = 0; i < familySymbolIds.Count; i++)
            {
                FamilySymbol familySymbol = doc.GetElement(familySymbolIds[i]) as FamilySymbol;
                var map = familySymbol.ParametersMap;
                familySymbols.Add(familySymbol);
                symbolsLengths.Add(Convert.ToInt32(map.get_Item("Длина полки").AsValueString()));
            }
        }

        static public void ZerificateElements(List<IndependentTag> tags)
        {
            foreach (FamilySymbol familySymbol in familySymbols)
            {
                var map = familySymbol.ParametersMap;
                map.get_Item("Длина полки").SetValueString("1");
            }

            foreach (IndependentTag tag in tags)
            {
                var newPos = tag.TagHeadPosition;
                if (tag.ParametersMap.get_Item("Тип выноски").AsValueString() == "С присоединенным концом")
                {
                    tag.ParametersMap.get_Item("Тип выноски").Set(1);
                }

                tag.LeaderEnd = newPos;
                if (tag.HasElbow)
                    tag.LeaderElbow = newPos;
            }
        }

        static public List<int> MeasureElements(List<IndependentTag> tags)
        {
            var res = new List<int>();
            for (int i = 0; i < tags.Count; i++)
            {
                double revitUnits;
                var tag = tags[i];
                var box = tag.get_BoundingBox(doc.ActiveView);
                if (box.Max.Z - box.Min.Z > 0.0001)
                {
                    revitUnits = box.Max.X - box.Min.X + box.Max.Z - box.Min.Z;
                    revitUnits *= 0.866; //sqrt(3)^-1 * 2
                }
                else
                    revitUnits = box.Max.X - box.Min.X;
                revitUnits = revitUnits * 304.8 / 100;
                int num = (int)revitUnits;
                if (num % 5 == 0)
                    res.Add(num);
                else
                    res.Add(5 * ((num / 5) + 1));
            }

            return res;
        }

        static public void ReturnElements(List<IndependentTag> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (saveEnd[i] != null)
                    tag.LeaderEnd = saveEnd[i];
                else
                {
                    tag.ParametersMap.get_Item("Тип выноски").Set(0);
                }

                if (saveElbow[i] != null)
                    tag.LeaderElbow = saveElbow[i];
            }

            for (int i = 0; i < familySymbols.Count; i++)
            {
                var familySymbol = familySymbols[i];
                familySymbol.ParametersMap.get_Item("Длина полки").SetValueString(symbolsLengths[i].ToString());
            }
        }
        static public string FindNameFormat(string name)
        {
            var list = new List<char>() { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            int i, j, z;

            j = name.Length - 1;

            while (j >= 0 && !list.Contains<char>(name[j]))
                j -= 1;
            if (j < 0)
                return name + " {0}";

            i = j;
            do
                i -= 1;
            while (i >= 0 && list.Contains<char>(name[i]));

            string res = "";
            for (z = 0; z < i + 1; z++)
                res += name[z];
            res += "{0}";
            for (z = j + 1; z < name.Length; z++)
                res += name[z];

            return res;
        }
        static public void MakeChanges(List<IndependentTag> tags, List<int> lengths)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                var typeId = tag.GetTypeId();
                var type = doc.GetElement(typeId) as FamilySymbol;

                var map = type.ParametersMap;
                var param = Convert.ToInt32(map.get_Item("Длина полки").AsValueString());
                if (param != lengths[i])
                {
                    var newTypeName = string.Format(FindNameFormat(type.Name), lengths[i]);

                    ISet<ElementId> allIds = type.Family.GetFamilySymbolIds();
                    FamilySymbol correctFamilySymbol = null;
                    foreach (ElementId id in allIds)
                    {
                        var familySymbol = doc.GetElement(id) as FamilySymbol;
                        if (familySymbol.Name == newTypeName)
                        {
                            correctFamilySymbol = familySymbol;
                            break;
                        }
                    }
                    if (correctFamilySymbol != null)
                    {
                        tag.ChangeTypeId(correctFamilySymbol.Id);
                    }
                    else
                    {
                        var newFamilySymblol = type.Duplicate(newTypeName);
                        newFamilySymblol.ParametersMap.get_Item("Длина полки").SetValueString(lengths[i].ToString());
                        tag.ChangeTypeId(newFamilySymblol.Id);
                    }
                }
            }
        }
    }
}

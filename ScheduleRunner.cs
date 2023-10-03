using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Plugin
{

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ScheduleRunner : IExternalCommand
    {
        static UIDocument UIdoc;
        public static Autodesk.Revit.DB.Document doc;
        static Transaction transaction;
        static ViewSchedule mainScheduleView, currentScheduleView;
        static string scheduleNameFormat, sheetNumberFormat;
        static ViewSheet sheet;
        static ScheduleField scheduleField;
        static ScheduleSheetInstance scheduleSheetInstance;
        static string ProjectDivision;

        static XYZ firstPosition = new XYZ(-0.0541338582679627, 0.846456692913388, 0);
        static XYZ secondPosition = new XYZ(-0.592191601050115, 0.846456692913388, 0);
        static XYZ currentPostion;
        static int up = 0, down = 0, num = 1;
        static double len = 0.64;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIdoc = commandData.Application.ActiveUIDocument;
            doc = UIdoc.Document;
            transaction = new Transaction(doc, "Main");
            mainScheduleView = doc.ActiveView as ViewSchedule;

            var elementFilter = new FilteredElementCollector(doc);
            var l = elementFilter.OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_TitleBlocks).ToElements();

            var fields = GetFields(mainScheduleView);
            if (fields == null)
            {
                MessageBox.Show(
                    "Для корректной работы необходимо выбрать лист со спецификацией в активном виде.\n" +
                    "Попробуйте выделить одну из ячеек и повторить попытку.", 
                    "Ошибка:не удалось выбрать спецификацию");
                return Result.Failed;
            }
            var menu = new ChooseMenu(l, fields);
            menu.ShowDialog();

            if (menu.HaveNull())
                return Result.Succeeded;

            scheduleNameFormat = string.Format("{0} {1} {2}", mainScheduleView.Name, menu.GetPostfix(), "{0}");
            scheduleField = menu.GetField();
            sheetNumberFormat = string.Format("{0}{1}", menu.GetUNICode(), "{0}");
            ProjectDivision = menu.GetProjectDivision();

            int amountOfSections = menu.GetAmount();
            currentPostion = firstPosition;
            bool firstIteration = true;
            while (up <= amountOfSections)
            {
                if (firstIteration)
                {
                    CreateNewSheet(menu.GetFirstSelectedElement());
                }
                else
                    CreateNewSheet(menu.GetSecondSelectedElement());

                num += 1;
                int step = 15;
                var filters = currentScheduleView.Definition.GetFilters();
                while (step >= 1 & up <= amountOfSections)
                {
                    transaction.Start();
                    filters[filters.Count - 1].SetValue((up + step).ToString());
                    currentScheduleView.Definition.SetFilters(filters);
                    transaction.Commit();

                    var BB = scheduleSheetInstance.get_BoundingBox(sheet);
                    if (BB.Max.Y - BB.Min.Y > len)
                    {
                        transaction.Start();
                        filters[filters.Count - 1].SetValue(up.ToString());
                        currentScheduleView.Definition.SetFilters(filters);
                        transaction.Commit();
                        step /= 2;
                    }
                    else
                        up += step;
                }
                if (firstIteration)
                {
                    firstIteration = false;
                    currentPostion = secondPosition;
                    len = 0.76;
                }
                down = up;
            }

            return Result.Succeeded;
        }


        static List<ScheduleField> GetFields(ViewSchedule viewSchedule)
        {
            var activeFields = new List<ScheduleFiel‌d>();

            ScheduleDefinition definition;
            try
            {
                definition = viewSchedule.Definition;
            }
            catch (Exception ex)
            {
                return null;
            }

            for (int i = 0; i < viewSchedule.Definition.GetFieldCount(); i++)
            {
                var newField = mainScheduleView.Definition.GetField(mainScheduleView.Definition.GetFieldId(i));
                activeFields.Add(newField);
            }
            return activeFields;
        }

        static void CreateNewView()
        {
            currentScheduleView = doc.GetElement(mainScheduleView.Duplicate(ViewDuplicateOption.Duplicate)) as ViewSchedule;
            currentScheduleView.Name = string.Format(scheduleNameFormat, num);
            currentScheduleView.Definition.AddFilter(new ScheduleFilter(scheduleField.FieldId, ScheduleFilterType.GreaterThan, down.ToString()));
            currentScheduleView.Definition.AddFilter(new ScheduleFilter(scheduleField.FieldId, ScheduleFilterType.LessThanOrEqual, up.ToString()));
        }

        static void CreateNewSheet(Element element)
        {
            transaction.Start();
            sheet = ViewSheet.Create(doc, element.Id);
            CreateNewView();
            scheduleSheetInstance = ScheduleSheetInstance.Create(doc, sheet.Id, currentScheduleView.Id, currentPostion);
            var param = sheet.ParametersMap.get_Item("ADSK_Штамп Раздел проекта").Set(ProjectDivision);
            try
            {
                sheet.SheetNumber = string.Format(sheetNumberFormat, num);
            }
            catch (Exception) { }

            transaction.Commit();
        }
    }
}

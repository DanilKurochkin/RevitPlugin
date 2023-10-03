using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Plugin
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class ChooseMenu : Window
    {
        Element firstSelectedElement, secondSelectedElement;
        string postfix, UNICode, ProjectDivisonText;
        ScheduleField scheduleField;
        int amount;

        public ChooseMenu(IList<Element> elements, IList<ScheduleField> fields)
        {
            InitializeComponent();

            var unpackedFields = new List<ScheduleFieldName>();
            for (int i = 0; i < fields.Count; i++)
            {
                unpackedFields.Add(new ScheduleFieldName(fields[i]));
            }

            var unpackedElements = new List<TemplateName>();
            for (int i = 0; i < elements.Count; i++)
            {
                unpackedElements.Add(new TemplateName(elements[i]));
            }

            First.ItemsSource = unpackedElements;
            First.DisplayMemberPath = "Name";
            Second.ItemsSource = unpackedElements;
            Second.DisplayMemberPath = "Name";
            Fields.ItemsSource = unpackedFields;
            Fields.DisplayMemberPath = "Name";
        }
        private void Submit(object sender, RoutedEventArgs e)
        {
            var firstSel = First.SelectedItem as TemplateName;
            firstSelectedElement = firstSel.element;
            var secondSel = Second.SelectedItem as TemplateName;
            secondSelectedElement = secondSel.element;
            var fieldsSel = Fields.SelectedItem as ScheduleFieldName;
            scheduleField = fieldsSel.field;
            postfix = Postfix.Text;
            UNICode = UNICodeTextBox.Text;
            ProjectDivisonText = ProjectDivision.Text;

            amount = Convert.ToInt32(Amount.Text);
            //start = Convert.ToInt32(Start.Text);
            this.Close();
        }

        public class ScheduleFieldName
        {
            public ScheduleField field;
            public string Name { get; set; }
            public ScheduleFieldName(ScheduleField scheduleField)
            {
                field = scheduleField;
                Name = scheduleField.GetName();
            }
        }

        public class TemplateName
        {
            public Element element;
            public string Name { get; set; }
            public TemplateName(Element element)
            {
                this.element = element;
                var elFamInst = element as FamilySymbol;
                Name = string.Format("{0} : {1}", elFamInst.FamilyName, element.Name);
            }
        }

        public Element GetFirstSelectedElement() { return firstSelectedElement; }
        public Element GetSecondSelectedElement() { return secondSelectedElement; }
        public string GetPostfix() { return postfix; }
        public string GetUNICode() { return UNICode; }
        public ScheduleField GetField() { return scheduleField; }
        private void SubmitCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public int GetAmount() { return amount; }
        public string GetProjectDivision() { return ProjectDivisonText; }
        public bool HaveNull()
        {
            if (firstSelectedElement == null)
                return true;
            if (secondSelectedElement == null)
                return true;
            if (scheduleField == null)
                return true;

            return false;
        }
    }
}

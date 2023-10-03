using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
using Plugin.Properties;
using System.Drawing;
using System.Drawing.Imaging;

namespace Plugin
{
    internal class MainApplication : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel panel = RibbonPanel(application);
            string thisAssemblyPass = Assembly.GetExecutingAssembly().Location;

            if (panel.AddItem(new PushButtonData("Авто-выноски", "Авто-выноски", thisAssemblyPass, "Plugin.AnnotationRunner"))
                is PushButton buttonAnnotation)
            {
                buttonAnnotation.ToolTip = "Исправление длины среди выделенных выносок";
                buttonAnnotation.LargeImage = Convert(Resources.annotation);
                buttonAnnotation.Image = Convert(Resources.annotation16);
            }

            if (panel.AddItem(new PushButtonData("На листы", "Спецификацию на листы", thisAssemblyPass, "Plugin.ScheduleRunner"))
                is PushButton buttonSchedule)
            {
                buttonSchedule.ToolTip = "Вынос спецификации на выбранные листы с автоматической нумерацией";
                buttonSchedule.LargeImage = Convert(Resources.schedule);
                buttonSchedule.Image = Convert(Resources.schedule16);
            }

            if (panel.AddItem(new PushButtonData("MIN->MAX", "MIN->MAX", thisAssemblyPass, "Plugin.MinMax"))
                is PushButton buttonMinMax)
            {
                buttonMinMax.ToolTip = "Определить параметры MIN MAX выделенного элемента";
                buttonMinMax.LargeImage = Convert(Resources.minmaxi);
                buttonMinMax.Image = Convert(Resources.minmaxi16);
            }

            if (panel.AddItem(new PushButtonData("Копировать имена", "Копировать имена", thisAssemblyPass, "Plugin.CopyNameOfSystems"))
                is PushButton buttonCopy)
            {
                buttonCopy.ToolTip = "Задать правильный параметр ИмяСистемы всем элементам в проекте";
                buttonCopy.LargeImage = Convert(Resources.copy32);
                buttonCopy.Image = Convert(Resources.copy16);
            }

            return Result.Succeeded;
        }

        public RibbonPanel RibbonPanel(UIControlledApplication a)
        {
            string tab = "IKP";
            RibbonPanel ribbonPannel = null;

            try
            {
                a.CreateRibbonTab(tab);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                a.CreateRibbonPanel(tab, "Основная панель");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            List<RibbonPanel> panels = a.GetRibbonPanels(tab);
            foreach (RibbonPanel p in panels.Where(p => p.Name == "Основная панель")) 
            {
                ribbonPannel = p;
            }

            
            return ribbonPannel;
        }

        private BitmapImage Convert(Bitmap bitmap)
        {
            using(var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitMapImage = new BitmapImage();
                bitMapImage.BeginInit();
                bitMapImage.StreamSource = memory;
                bitMapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitMapImage.EndInit();

                return bitMapImage;
            }
        }
    }
}

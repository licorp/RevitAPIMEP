using System;
using System.Collections.Generic;
using System.Windows;
using Autodesk.Revit.UI;

namespace Quoc_MEP
{
    /// <summary>
    /// Service để tạo và quản lý Dockable Panes
    /// </summary>
    public class DockablePaneCreatorService
    {
        private readonly UIControlledApplication _application;
        private readonly Dictionary<Guid, FrameworkElement> _registeredPanes;

        public DockablePaneCreatorService(UIControlledApplication application)
        {
            _application = application;
            _registeredPanes = new Dictionary<Guid, FrameworkElement>();
        }

        public void Initialize()
        {
            // Empty - initialization not needed
        }

        public void Register(Guid guid, FrameworkElement page)
        {
            if (!_registeredPanes.ContainsKey(guid))
            {
                _application.RegisterDockablePane(new DockablePaneId(guid), page.GetType().Name, page as IDockablePaneProvider);
                _registeredPanes.Add(guid, page);
            }
        }

        public void Register(Guid guid, string title, FrameworkElement page)
        {
            if (!_registeredPanes.ContainsKey(guid))
            {
                _application.RegisterDockablePane(new DockablePaneId(guid), title, page as IDockablePaneProvider);
                _registeredPanes.Add(guid, page);
            }
        }

        public DockablePane Get(Guid guid, UIApplication uiapp)
        {
            try
            {
                if (uiapp == null)
                    return null;
                    
                return uiapp.GetDockablePane(new DockablePaneId(guid));
            }
            catch
            {
                return null;
            }
        }

        public FrameworkElement GetFrameworkElement(Guid guid)
        {
            if (_registeredPanes.ContainsKey(guid))
            {
                return _registeredPanes[guid];
            }
            return null;
        }

        public void Dispose()
        {
            _registeredPanes.Clear();
        }
    }

    /// <summary>
    /// Extension methods for DockablePane
    /// </summary>
    public static class DockablePaneExtension
    {
        public static bool TryShow(this DockablePane dockablePane)
        {
            try
            {
                if (dockablePane != null && !dockablePane.IsShown())
                {
                    dockablePane.Show();
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool TryHide(this DockablePane dockablePane)
        {
            try
            {
                if (dockablePane != null && dockablePane.IsShown())
                {
                    dockablePane.Hide();
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool TryIsShown(this DockablePane dockablePane)
        {
            try
            {
                return dockablePane?.IsShown() ?? false;
            }
            catch
            {
                return false;
            }
        }

        public static string TryGetTitle(this DockablePane dockablePane)
        {
            try
            {
                return dockablePane?.GetTitle() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

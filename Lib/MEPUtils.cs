using System;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace Quoc_MEP
{
    /// <summary>
    /// Selection filter for MEP elements (pipes, ducts, fittings, accessories)
    /// </summary>
    public class MEPSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem?.Category?.Name == null) return false;
            
            string categoryName = elem.Category.Name;
            foreach (string allowedCategory in Constants.MEP_CATEGORIES)
            {
                if (categoryName.Equals(allowedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    /// <summary>
    /// Utilities for working with MEP elements and rotation axes
    /// </summary>
    public static class MEPUtils
    {
        /// <summary>
        /// Get rotation axis line from an element
        /// </summary>
        public static Line GetRotationAxisFromElement(Element element)
        {
            if (element == null) return null;

            try
            {
                // Method 1: Try LocationCurve (for pipes and ducts)
                if (element.Location is LocationCurve locationCurve)
                {
                    Curve curve = locationCurve.Curve;
                    if (curve is Line line)
                    {
                        Trace.WriteLine($"Got axis from LocationCurve (Line) for element {element.Id}");
                        return line;
                    }
                    else if (curve != null)
                    {
                        // Convert curve to line using start and end points
                        XYZ startPoint = curve.GetEndPoint(0);
                        XYZ endPoint = curve.GetEndPoint(1);
                        Line resultLine = Line.CreateBound(startPoint, endPoint);
                        Trace.WriteLine($"Converted curve to line for element {element.Id}");
                        return resultLine;
                    }
                }
                
                // Method 2: Try LocationPoint with vertical axis (for fittings)
                if (element.Location is LocationPoint locationPoint)
                {
                    XYZ center = locationPoint.Point;
                    XYZ axisDirection = XYZ.BasisZ; // Vertical axis
                    XYZ startPoint = center - axisDirection * Constants.AXIS_LENGTH_EXTENSION;
                    XYZ endPoint = center + axisDirection * Constants.AXIS_LENGTH_EXTENSION;
                    Line resultLine = Line.CreateBound(startPoint, endPoint);
                    Trace.WriteLine($"Created vertical axis from LocationPoint for element {element.Id}");
                    return resultLine;
                }
                
                // Method 3: Fallback using bounding box center
                BoundingBoxXYZ bbox = element.get_BoundingBox(null);
                if (bbox != null)
                {
                    XYZ center = (bbox.Min + bbox.Max) * 0.5;
                    XYZ axisDirection = XYZ.BasisZ;
                    XYZ startPoint = center - axisDirection * Constants.AXIS_LENGTH_EXTENSION;
                    XYZ endPoint = center + axisDirection * Constants.AXIS_LENGTH_EXTENSION;
                    Line resultLine = Line.CreateBound(startPoint, endPoint);
                    Trace.WriteLine($"Created axis from bounding box for element {element.Id}");
                    return resultLine;
                }

                Trace.WriteLine($"Could not determine axis for element {element.Id}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error getting rotation axis from element {element.Id}: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Validate if an angle is within reasonable bounds
        /// </summary>
        public static bool IsValidRotationAngle(double angle)
        {
            return !double.IsNaN(angle) && !double.IsInfinity(angle) && angle >= -360 && angle <= 360;
        }

        /// <summary>
        /// Convert angle from degrees to radians
        /// </summary>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Convert angle from radians to degrees
        /// </summary>
        public static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        /// <summary>
        /// Check if element can be rotated (not read-only, not pinned, etc.)
        /// </summary>
        public static bool CanElementBeRotated(Element element)
        {
            if (element == null) return false;

            try
            {
                // Check if element is pinned
                if (element.Pinned) return false;

                // Additional checks can be added here
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

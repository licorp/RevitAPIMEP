using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;

namespace Quoc_MEP
{
    /// <summary>
    /// Helper class để xử lý việc kết nối các MEP family
    /// </summary>
    public static class ConnectionHelper
    {
        /// <summary>
        /// Unpin một element nếu nó đang bị pin
        /// </summary>
        /// <param name="doc">Document</param>
        /// <param name="element">Element cần unpin</param>
        public static void UnpinElementIfPinned(Document doc, Element element)
        {
            try
            {
                if (element.Pinned)
                {
                    element.Pinned = false;
                }
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Di chuyển và kết nối hai MEP element
        /// </summary>
        /// <param name="doc">Document</param>
        /// <param name="sourceElement">Element nguồn (sẽ được di chuyển)</param>
        /// <param name="destElement">Element đích</param>
        /// <returns>True nếu thành công</returns>
        public static bool MoveAndConnect(Document doc, Element sourceElement, Element destElement)
        {
            try
            {
                // Lấy connector gần nhất từ cả hai element
                var sourceConnector = GetNearestAvailableConnector(sourceElement, destElement);
                var destConnector = GetNearestAvailableConnector(destElement, sourceElement);

                if (sourceConnector == null || destConnector == null)
                {
                    return false;
                }

                // Tính toán vector di chuyển
                XYZ moveVector = destConnector.Origin - sourceConnector.Origin;

                // Di chuyển source element
                ElementTransformUtils.MoveElement(doc, sourceElement.Id, moveVector);

                // Cập nhật lại connector sau khi di chuyển
                sourceConnector = GetNearestAvailableConnector(sourceElement, destElement);

                // Thực hiện kết nối
                if (sourceConnector != null && destConnector != null)
                {
                    // Kiểm tra compatibility trước khi kết nối
                    if (AreConnectorsCompatible(sourceConnector, destConnector))
                    {
                        sourceConnector.ConnectTo(destConnector);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Di chuyển, căn chỉnh và kết nối hai MEP element
        /// </summary>
        /// <param name="doc">Document</param>
        /// <param name="sourceElement">Element nguồn (sẽ được di chuyển)</param>
        /// <param name="destElement">Element đích</param>
        /// <returns>True nếu thành công</returns>
        public static bool MoveConnectAndAlign(Document doc, Element sourceElement, Element destElement)
        {
            try
            {
                // Lấy connector gần nhất từ cả hai element
                var sourceConnector = GetNearestAvailableConnector(sourceElement, destElement);
                var destConnector = GetNearestAvailableConnector(destElement, sourceElement);

                if (sourceConnector == null || destConnector == null)
                {
                    return false;
                }

                // Tính toán transformation để căn chỉnh
                Transform transform = CalculateAlignmentTransform(sourceConnector, destConnector);

                // Áp dụng transformation
                ElementTransformUtils.MoveElement(doc, sourceElement.Id, transform.Origin);
                
                // Quay element nếu cần
                if (!transform.BasisX.IsAlmostEqualTo(XYZ.BasisX) || !transform.BasisY.IsAlmostEqualTo(XYZ.BasisY))
                {
                    Line rotationAxis = Line.CreateBound(transform.Origin, transform.Origin + XYZ.BasisZ);
                    double rotationAngle = CalculateRotationAngle(sourceConnector.CoordinateSystem, destConnector.CoordinateSystem);
                    if (Math.Abs(rotationAngle) > 0.001) // Chỉ quay nếu góc quay có ý nghĩa
                    {
                        ElementTransformUtils.RotateElement(doc, sourceElement.Id, rotationAxis, rotationAngle);
                    }
                }

                // Cập nhật lại connector sau khi transform
                sourceConnector = GetNearestAvailableConnector(sourceElement, destElement);

                // Thực hiện kết nối
                if (sourceConnector != null && destConnector != null)
                {
                    if (AreConnectorsCompatible(sourceConnector, destConnector))
                    {
                        sourceConnector.ConnectTo(destConnector);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ngắt kết nối các connector của một MEP element
        /// </summary>
        /// <param name="doc">Document</param>
        /// <param name="element">Element cần ngắt kết nối</param>
        /// <returns>True nếu thành công</returns>
        public static bool DisconnectElement(Document doc, Element element)
        {
            try
            {
                ConnectorManager connectorManager = GetConnectorManager(element);
                if (connectorManager == null) return false;

                bool hasDisconnected = false;
                
                foreach (Connector connector in connectorManager.Connectors)
                {
                    if (connector.IsConnected)
                    {
                        // Lấy danh sách connector được kết nối
                        var connectedConnectors = new List<Connector>();
                        foreach (Connector connectedConnector in connector.AllRefs)
                        {
                            if (connectedConnector.Owner.Id != element.Id)
                            {
                                connectedConnectors.Add(connectedConnector);
                            }
                        }

                        // Ngắt kết nối
                        foreach (var connectedConnector in connectedConnectors)
                        {
                            connector.DisconnectFrom(connectedConnector);
                            hasDisconnected = true;
                        }
                    }
                }

                return hasDisconnected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra element có connector được kết nối không
        /// </summary>
        public static bool HasConnectedConnectors(Element element)
        {
            try
            {
                ConnectorManager connectorManager = GetConnectorManager(element);
                if (connectorManager == null) return false;

                foreach (Connector connector in connectorManager.Connectors)
                {
                    if (connector.IsConnected)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Unpin element nếu cần thiết
        /// </summary>
        public static bool UnpinElementIfNeeded(Element element)
        {
            try
            {
                if (element.Pinned)
                {
                    element.Pinned = false;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy connector gần nhất và khả dụng từ element
        /// </summary>
        private static Connector GetNearestAvailableConnector(Element element, Element targetElement)
        {
            try
            {
                ConnectorManager connectorManager = GetConnectorManager(element);
                if (connectorManager == null) return null;

                Connector nearestConnector = null;
                double minDistance = double.MaxValue;

                // Lấy vị trí trung tâm của target element để tính khoảng cách
                XYZ targetLocation = GetElementLocation(targetElement);
                if (targetLocation == null) return null;

                foreach (Connector connector in connectorManager.Connectors)
                {
                    // Chỉ lấy connector chưa được kết nối
                    if (connector.IsConnected) continue;

                    double distance = connector.Origin.DistanceTo(targetLocation);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestConnector = connector;
                    }
                }

                return nearestConnector;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy ConnectorManager từ element
        /// </summary>
        private static ConnectorManager GetConnectorManager(Element element)
        {
            // Mechanical elements
            if (element is MEPCurve mepCurve)
                return mepCurve.ConnectorManager;

            // Family instance (equipment, fixtures, etc.)
            if (element is FamilyInstance familyInstance)
                return familyInstance.MEPModel?.ConnectorManager;

            return null;
        }

        /// <summary>
        /// Lấy vị trí của element
        /// </summary>
        private static XYZ GetElementLocation(Element element)
        {
            if (element.Location is LocationPoint locationPoint)
                return locationPoint.Point;

            if (element.Location is LocationCurve locationCurve)
                return locationCurve.Curve.Evaluate(0.5, true); // Lấy điểm giữa

            return null;
        }

        /// <summary>
        /// Kiểm tra hai connector có tương thích để kết nối không
        /// </summary>
        private static bool AreConnectorsCompatible(Connector connector1, Connector connector2)
        {
            try
            {
                // Kiểm tra domain (Electrical, HVAC, Piping)
                if (connector1.Domain != connector2.Domain)
                    return false;

                // Kiểm tra kích thước
                if (Math.Abs(connector1.Radius - connector2.Radius) > 0.001)
                    return false;

                // Kiểm tra flow direction (In vs Out)
                if (connector1.Direction == FlowDirectionType.In && connector2.Direction == FlowDirectionType.In)
                    return false;

                if (connector1.Direction == FlowDirectionType.Out && connector2.Direction == FlowDirectionType.Out)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tính toán transform để căn chỉnh connector
        /// </summary>
        private static Transform CalculateAlignmentTransform(Connector sourceConnector, Connector destConnector)
        {
            Transform transform = Transform.Identity;
            
            // Tính vector di chuyển
            XYZ moveVector = destConnector.Origin - sourceConnector.Origin;
            transform.Origin = moveVector;

            return transform;
        }

        /// <summary>
        /// Tính toán góc quay cần thiết để căn chỉnh
        /// </summary>
        private static double CalculateRotationAngle(Transform sourceCS, Transform destCS)
        {
            try
            {
                // Tính góc giữa hai vector hướng
                XYZ sourceDirection = sourceCS.BasisZ;
                XYZ destDirection = destCS.BasisZ.Negate(); // Negate vì cần hướng ngược lại để kết nối

                double dotProduct = sourceDirection.DotProduct(destDirection);
                return Math.Acos(Math.Max(-1.0, Math.Min(1.0, dotProduct)));
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Kiểm tra xem hai element có được kết nối với nhau không
        /// </summary>
        /// <param name="element1">Element đầu tiên</param>
        /// <param name="element2">Element thứ hai</param>
        /// <returns>True nếu hai element được kết nối với nhau</returns>
        public static bool AreElementsConnected(Element element1, Element element2)
        {
            try
            {
                ConnectorManager connectorManager1 = GetConnectorManager(element1);
                if (connectorManager1 == null) return false;

                foreach (Connector connector1 in connectorManager1.Connectors)
                {
                    if (connector1.IsConnected)
                    {
                        foreach (Connector connectedConnector in connector1.AllRefs)
                        {
                            if (connectedConnector.Owner.Id == element2.Id)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ngắt kết nối giữa hai element cụ thể
        /// </summary>
        /// <param name="doc">Document</param>
        /// <param name="element1">Element đầu tiên</param>
        /// <param name="element2">Element thứ hai</param>
        /// <returns>True nếu ngắt kết nối thành công</returns>
        public static bool DisconnectTwoElements(Document doc, Element element1, Element element2)
        {
            try
            {
                ConnectorManager connectorManager1 = GetConnectorManager(element1);
                if (connectorManager1 == null) return false;

                bool disconnected = false;

                foreach (Connector connector1 in connectorManager1.Connectors)
                {
                    if (connector1.IsConnected)
                    {
                        // Tìm connector của element2 được kết nối với connector1
                        Connector connectorToDisconnect = null;
                        foreach (Connector connectedConnector in connector1.AllRefs)
                        {
                            if (connectedConnector.Owner.Id == element2.Id)
                            {
                                connectorToDisconnect = connectedConnector;
                                break;
                            }
                        }

                        // Nếu tìm thấy kết nối, ngắt nó
                        if (connectorToDisconnect != null)
                        {
                            connector1.DisconnectFrom(connectorToDisconnect);
                            disconnected = true;
                        }
                    }
                }

                return disconnected;
            }
            catch
            {
                return false;
            }
        }
    }
}

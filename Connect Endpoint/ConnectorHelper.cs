using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;

namespace Quoc_MEP
{
    public static class ConnectorHelper
    {
        /// <summary>
        /// Lấy connector khả dụng (chưa kết nối) từ một MEP element
        /// </summary>
        /// <param name="element">MEP element cần lấy connector</param>
        /// <returns>Connector khả dụng đầu tiên hoặc null nếu không tìm thấy</returns>
        public static Connector GetAvailableConnector(Element element)
        {
            try
            {
                ConnectorSet connectors = GetConnectorSet(element);
                if (connectors == null) return null;

                // Tìm connector chưa kết nối (không có AllRefs hoặc AllRefs rỗng)
                foreach (Connector connector in connectors)
                {
                    if (connector != null && IsConnectorAvailable(connector))
                    {
                        return connector;
                    }
                }

                // Nếu không tìm thấy connector chưa kết nối, trả về connector đầu tiên
                foreach (Connector connector in connectors)
                {
                    if (connector != null)
                    {
                        return connector;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy tất cả connector từ một MEP element
        /// </summary>
        /// <param name="element">MEP element</param>
        /// <returns>Danh sách các connector</returns>
        public static List<Connector> GetAllConnectors(Element element)
        {
            List<Connector> connectorList = new List<Connector>();
            
            try
            {
                ConnectorSet connectors = GetConnectorSet(element);
                if (connectors != null)
                {
                    foreach (Connector connector in connectors)
                    {
                        if (connector != null)
                        {
                            connectorList.Add(connector);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors
            }

            return connectorList;
        }

        /// <summary>
        /// Lấy ConnectorSet từ một element
        /// </summary>
        /// <param name="element">Element cần lấy ConnectorSet</param>
        /// <returns>ConnectorSet hoặc null</returns>
        private static ConnectorSet GetConnectorSet(Element element)
        {
            try
            {
                // Xử lý cho Pipe
                if (element is Pipe pipe)
                {
                    return pipe.ConnectorManager?.Connectors;
                }

                // Xử lý cho Duct
                if (element is Duct duct)
                {
                    return duct.ConnectorManager?.Connectors;
                }

                // Xử lý cho FlexPipe
                if (element is FlexPipe flexPipe)
                {
                    return flexPipe.ConnectorManager?.Connectors;
                }

                // Xử lý cho FlexDuct
                if (element is FlexDuct flexDuct)
                {
                    return flexDuct.ConnectorManager?.Connectors;
                }

                // Xử lý cho FamilyInstance (fittings, equipment, etc.)
                if (element is FamilyInstance familyInstance)
                {
                    return familyInstance.MEPModel?.ConnectorManager?.Connectors;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra connector có khả dụng (chưa kết nối) không
        /// </summary>
        /// <param name="connector">Connector cần kiểm tra</param>
        /// <returns>True nếu connector khả dụng</returns>
        private static bool IsConnectorAvailable(Connector connector)
        {
            try
            {
                // Kiểm tra xem connector có kết nối nào không
                ConnectorSet allRefs = connector.AllRefs;
                
                // Nếu AllRefs null hoặc rỗng thì connector chưa kết nối
                if (allRefs == null)
                    return true;

                int connectionCount = 0;
                foreach (Connector connectedConnector in allRefs)
                {
                    // Bỏ qua chính connector đó
                    if (connectedConnector.Owner.Id != connector.Owner.Id)
                    {
                        connectionCount++;
                    }
                }

                return connectionCount == 0;
            }
            catch (Exception)
            {
                // Nếu có lỗi, coi như connector khả dụng
                return true;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết về connector
        /// </summary>
        /// <param name="connector">Connector cần lấy thông tin</param>
        /// <returns>Chuỗi mô tả connector</returns>
        public static string GetConnectorInfo(Connector connector)
        {
            if (connector == null) return "Null connector";

            try
            {
                string info = $"Connector ID: {connector.Id}\n";
                info += $"Position: {connector.Origin}\n";
                info += $"Direction: {connector.CoordinateSystem?.BasisZ}\n";
                info += $"Domain: {connector.Domain}\n";
                info += $"ConnectorType: {connector.ConnectorType}\n";
                info += $"Owner: {connector.Owner?.Name} (ID: {connector.Owner?.Id})\n";
                
                if (connector.Radius > 0)
                    info += $"Radius: {connector.Radius}\n";
                
                if (connector.Width > 0 && connector.Height > 0)
                    info += $"Size: {connector.Width} x {connector.Height}\n";

                return info;
            }
            catch (Exception ex)
            {
                return $"Error getting connector info: {ex.Message}";
            }
        }

        /// <summary>
        /// Lấy connector đã kết nối từ element (để làm target position)
        /// </summary>
        /// <param name="element">Element cần tìm connected connector</param>
        /// <returns>Connected connector hoặc null</returns>
        public static Connector GetConnectedConnector(Element element)
        {
            Logger.LogMethodEntry(nameof(GetConnectedConnector), element?.Id);
            
            try
            {
                ConnectorSet connectors = GetConnectorSet(element);
                if (connectors == null) return null;

                foreach (Connector connector in connectors)
                {
                    if (connector.IsConnected)
                    {
                        Logger.LogInfo($"Found connected connector: {GetConnectorInfo(connector)}");
                        return connector;
                    }
                }

                Logger.LogWarning("No connected connector found");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error getting connected connector");
                return null;
            }
        }

        /// <summary>
        /// Lấy connector chưa kết nối từ element (để làm endpoint)
        /// </summary>
        /// <param name="element">Element cần tìm unconnected connector</param>
        /// <returns>Unconnected connector hoặc null</returns>
        public static Connector GetUnconnectedConnector(Element element)
        {
            Logger.LogMethodEntry(nameof(GetUnconnectedConnector), element?.Id);
            
            try
            {
                ConnectorSet connectors = GetConnectorSet(element);
                if (connectors == null) return null;

                foreach (Connector connector in connectors)
                {
                    if (!connector.IsConnected)
                    {
                        Logger.LogInfo($"Found unconnected connector: {GetConnectorInfo(connector)}");
                        return connector;
                    }
                }

                Logger.LogWarning("No unconnected connector found");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error getting unconnected connector");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra hai connector có tương thích để kết nối không
        /// </summary>
        /// <param name="connector1">Connector thứ nhất</param>
        /// <param name="connector2">Connector thứ hai</param>
        /// <returns>True nếu có thể kết nối</returns>
        public static bool AreConnectorsCompatible(Connector connector1, Connector connector2)
        {
            Logger.LogMethodEntry(nameof(AreConnectorsCompatible));
            
            if (connector1 == null || connector2 == null) 
            {
                Logger.LogWarning("One or both connectors are null");
                Logger.LogMethodExit(nameof(AreConnectorsCompatible), false);
                return false;
            }

            try
            {
                Logger.LogInfo($"Checking compatibility: Connector1 Domain={connector1.Domain}, Type={connector1.ConnectorType}");
                Logger.LogInfo($"Checking compatibility: Connector2 Domain={connector2.Domain}, Type={connector2.ConnectorType}");

                // Kiểm tra domain (Piping, HVAC, Electrical)
                if (connector1.Domain != connector2.Domain) 
                {
                    Logger.LogWarning($"Domain mismatch: {connector1.Domain} vs {connector2.Domain}");
                    Logger.LogMethodExit(nameof(AreConnectorsCompatible), false);
                    return false;
                }

                // Kiểm tra connector type (End, Side, etc.)
                // Thường thì End connector có thể kết nối với End connector
                bool compatible = (connector1.ConnectorType == ConnectorType.End && connector2.ConnectorType == ConnectorType.End);

                Logger.LogInfo($"Connectors compatible: {compatible}");
                Logger.LogMethodExit(nameof(AreConnectorsCompatible), compatible);
                return compatible;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error checking connector compatibility");
                Logger.LogMethodExit(nameof(AreConnectorsCompatible), false);
                return false;
            }
        }

        /// <summary>
        /// Thực hiện kết nối giữa hai connectors
        /// </summary>
        /// <param name="connector1">Connector thứ nhất</param>
        /// <param name="connector2">Connector thứ hai</param>
        /// <returns>True nếu kết nối thành công</returns>
        public static bool ConnectConnectors(Connector connector1, Connector connector2)
        {
            Logger.LogMethodEntry(nameof(ConnectConnectors));
            
            if (!AreConnectorsCompatible(connector1, connector2))
            {
                Logger.LogError("Connectors are not compatible for connection");
                Logger.LogMethodExit(nameof(ConnectConnectors), false);
                return false;
            }

            try
            {
                // Kiểm tra xem đã kết nối chưa
                if (connector1.IsConnected)
                {
                    Logger.LogInfo("Connector1 is already connected, disconnecting first");
                    DisconnectConnector(connector1);
                }

                if (connector2.IsConnected)
                {
                    Logger.LogInfo("Connector2 is already connected, disconnecting first");
                    DisconnectConnector(connector2);
                }

                // Thực hiện kết nối
                connector1.ConnectTo(connector2);
                
                Logger.LogInfo("Successfully connected connectors");
                Logger.LogMethodExit(nameof(ConnectConnectors), true);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error connecting connectors");
                Logger.LogMethodExit(nameof(ConnectConnectors), false);
                return false;
            }
        }

        /// <summary>
        /// Ngắt kết nối của một connector
        /// </summary>
        /// <param name="connector">Connector cần ngắt kết nối</param>
        public static void DisconnectConnector(Connector connector)
        {
            Logger.LogMethodEntry(nameof(DisconnectConnector));
            
            try
            {
                if (connector?.IsConnected == true)
                {
                    ConnectorSet allRefs = connector.AllRefs;
                    if (allRefs != null)
                    {
                        foreach (Connector connectedConnector in allRefs)
                        {
                            if (connectedConnector.Owner.Id != connector.Owner.Id)
                            {
                                Logger.LogInfo($"Disconnecting from connector on element ID: {connectedConnector.Owner.Id}");
                                connector.DisconnectFrom(connectedConnector);
                                break;
                            }
                        }
                    }
                }
                Logger.LogMethodExit(nameof(DisconnectConnector));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error disconnecting connector");
            }
        }
    }
}
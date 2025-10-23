using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace Quoc_MEP
{
    public static class PipeHelper
    {
        /// <summary>
        /// Cập nhật endpoint của pipe để kết nối với connector của đối tượng target
        /// </summary>
        /// <param name="pipe">Pipe cần cập nhật endpoint</param>
        /// <param name="targetConnector">Connector của đối tượng target</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public static bool UpdatePipeEndpoint(Pipe pipe, Connector targetConnector)
        {
            Logger.LogMethodEntry(nameof(UpdatePipeEndpoint), pipe?.Id, targetConnector?.Owner?.Id);
            
            if (pipe == null || targetConnector == null) 
            {
                Logger.LogError("Pipe hoặc targetConnector là null");
                Logger.LogMethodExit(nameof(UpdatePipeEndpoint), false);
                return false;
            }

            try
            {
                Logger.LogInfo($"Starting pipe endpoint update for pipe ID: {pipe.Id}");
                Logger.LogInfo($"Target connector info: {ConnectorHelper.GetConnectorInfo(targetConnector)}");

                // Lấy các connector của pipe
                ConnectorSet pipeConnectors = pipe.ConnectorManager?.Connectors;
                if (pipeConnectors == null) 
                {
                    Logger.LogError("Pipe không có ConnectorManager hoặc Connectors");
                    Logger.LogMethodExit(nameof(UpdatePipeEndpoint), false);
                    return false;
                }

                Logger.LogInfo($"Pipe có {pipeConnectors.Size} connectors");

                // Tìm connector gần nhất với target connector
                Connector nearestPipeConnector = FindNearestConnector(pipeConnectors, targetConnector.Origin);
                if (nearestPipeConnector == null) 
                {
                    Logger.LogError("Không tìm thấy connector gần nhất trên pipe");
                    Logger.LogMethodExit(nameof(UpdatePipeEndpoint), false);
                    return false;
                }

                Logger.LogInfo($"Nearest pipe connector: {ConnectorHelper.GetConnectorInfo(nearestPipeConnector)}");

                // Phương án 1 (PRIORITY): Sử dụng Location curve để điều chỉnh endpoint
                bool method1Success = UpdateUsingLocationCurve(pipe, nearestPipeConnector, targetConnector);
                if (method1Success) 
                {
                    Logger.LogInfo("Phương án 1 (Location Curve) thành công - Endpoint đã được di chuyển");
                    Logger.LogMethodExit(nameof(UpdatePipeEndpoint), true);
                    return true;
                }

                // Phương án 2: Sử dụng ConnectTo để kết nối trực tiếp (chỉ khi gần nhau)
                bool method2Success = UpdateUsingConnectTo(nearestPipeConnector, targetConnector);
                if (method2Success) 
                {
                    Logger.LogInfo("Phương án 2 (ConnectTo) thành công");
                    Logger.LogMethodExit(nameof(UpdatePipeEndpoint), true);
                    return true;
                }

                // Phương án 3: Tạo pipe mới kết nối (chỉ khi cần thiết)
                bool method3Success = UpdateUsingPipeCreate(pipe, nearestPipeConnector, targetConnector);
                if (method3Success) 
                {
                    Logger.LogInfo("Phương án 3 (Pipe.Create) thành công - Tạo pipe kết nối");
                    Logger.LogMethodExit(nameof(UpdatePipeEndpoint), true);
                    return true;
                }

                Logger.LogError("Tất cả phương án đều thất bại");
                Logger.LogMethodExit(nameof(UpdatePipeEndpoint), false);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error in UpdatePipeEndpoint");
                Logger.LogMethodExit(nameof(UpdatePipeEndpoint), false);
                return false;
            }
        }

        /// <summary>
        /// Tìm connector gần nhất với một điểm cho trước
        /// </summary>
        /// <param name="connectors">ConnectorSet cần tìm</param>
        /// <param name="targetPoint">Điểm target</param>
        /// <returns>Connector gần nhất</returns>
        private static Connector FindNearestConnector(ConnectorSet connectors, XYZ targetPoint)
        {
            Connector nearestConnector = null;
            double minDistance = double.MaxValue;

            foreach (Connector connector in connectors)
            {
                if (connector != null)
                {
                    double distance = connector.Origin.DistanceTo(targetPoint);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestConnector = connector;
                    }
                }
            }

            return nearestConnector;
        }

        /// <summary>
        /// Phương án 1: Sử dụng ConnectTo để kết nối trực tiếp
        /// </summary>
        private static bool UpdateUsingConnectTo(Connector pipeConnector, Connector targetConnector)
        {
            Logger.LogMethodEntry(nameof(UpdateUsingConnectTo));
            
            try
            {
                Logger.LogInfo("Attempting direct connector connection using ConnectTo");
                
                // Kiểm tra connectors có tương thích không
                if (!ConnectorHelper.AreConnectorsCompatible(pipeConnector, targetConnector))
                {
                    Logger.LogWarning("Connectors are not compatible for connection");
                    return false;
                }

                // Kiểm tra khoảng cách giữa 2 connectors
                double distance = pipeConnector.Origin.DistanceTo(targetConnector.Origin);
                Logger.LogInfo($"Distance between connectors: {distance}");

                // Nếu khoảng cách quá xa, không thể kết nối trực tiếp
                if (distance > 0.1) // 0.1 feet ~ 3cm
                {
                    Logger.LogWarning($"Connectors too far apart for direct connection: {distance} feet");
                    Logger.LogMethodExit(nameof(UpdateUsingConnectTo), false);
                    return false;
                }

                // Kiểm tra xem đã kết nối chưa
                if (pipeConnector.IsConnected)
                {
                    Logger.LogInfo("Pipe connector is already connected, disconnecting first");
                    ConnectorHelper.DisconnectConnector(pipeConnector);
                }

                if (targetConnector.IsConnected)
                {
                    Logger.LogInfo("Target connector is already connected, disconnecting first");
                    ConnectorHelper.DisconnectConnector(targetConnector);
                }

                // Thực hiện kết nối
                pipeConnector.ConnectTo(targetConnector);
                
                Logger.LogInfo("Successfully connected connectors using ConnectTo");
                Logger.LogMethodExit(nameof(UpdateUsingConnectTo), true);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error in UpdateUsingConnectTo");
                Logger.LogMethodExit(nameof(UpdateUsingConnectTo), false);
                return false;
            }
        }

        /// <summary>
        /// Phương án 2: Sử dụng Pipe.Create để tạo pipe mới kết nối
        /// </summary>
        private static bool UpdateUsingPipeCreate(Pipe originalPipe, Connector pipeConnector, Connector targetConnector)
        {
            Logger.LogMethodEntry(nameof(UpdateUsingPipeCreate));
            
            try
            {
                Logger.LogInfo("Attempting to create new pipe connection");
                
                Document doc = originalPipe.Document;
                
                // Lấy thông tin về pipe type, level, diameter
                PipeType pipeType = doc.GetElement(originalPipe.GetTypeId()) as PipeType;
                Level level = doc.GetElement(originalPipe.LevelId) as Level;
                
                // Nếu không có level từ pipe, lấy level từ view hoặc default level
                if (level == null)
                {
                    level = doc.ActiveView.GenLevel;
                    if (level == null)
                    {
                        // Lấy level đầu tiên trong project
                        FilteredElementCollector levelCollector = new FilteredElementCollector(doc)
                            .OfClass(typeof(Level));
                        level = levelCollector.FirstElement() as Level;
                    }
                }

                Logger.LogInfo($"Creating pipe with Type: {pipeType?.Name}, Level: {level?.Name}");
                Logger.LogInfo($"Distance between connectors: {pipeConnector.Origin.DistanceTo(targetConnector.Origin)} feet");

                // Kiểm tra null trước khi tạo pipe
                if (pipeType == null)
                {
                    Logger.LogError("PipeType is null, cannot create pipe");
                    return false;
                }
                
                if (level == null)
                {
                    Logger.LogError("Level is null, cannot create pipe");
                    return false;
                }

                // Ngắt kết nối existing connections nếu có
                if (pipeConnector.IsConnected)
                {
                    Logger.LogInfo("Disconnecting pipe connector");
                    ConnectorHelper.DisconnectConnector(pipeConnector);
                }

                if (targetConnector.IsConnected)
                {
                    Logger.LogInfo("Disconnecting target connector");
                    ConnectorHelper.DisconnectConnector(targetConnector);
                }

                // Tạo pipe mới kết nối 2 connectors
                Logger.LogInfo($"Creating pipe with TypeId: {pipeType.Id}, LevelId: {level.Id}");
                Pipe newPipe = Pipe.Create(doc, pipeType.Id, level.Id, pipeConnector, targetConnector);
                
                if (newPipe != null)
                {
                    // Copy diameter từ original pipe
                    double originalDiameter = originalPipe.Diameter;
                    Parameter diameterParam = newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                    if (diameterParam != null && !diameterParam.IsReadOnly)
                    {
                        diameterParam.Set(originalDiameter);
                        Logger.LogInfo($"Set new pipe diameter to: {originalDiameter}");
                    }

                    Logger.LogInfo($"Successfully created new connecting pipe: ID={newPipe.Id}, Length={newPipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble()}");
                    
                    // Force regeneration
                    doc.Regenerate();
                    Logger.LogInfo("Document regenerated");
                    
                    Logger.LogMethodExit(nameof(UpdateUsingPipeCreate), true);
                    return true;
                }

                Logger.LogWarning("Failed to create new pipe - Pipe.Create returned null");
                Logger.LogMethodExit(nameof(UpdateUsingPipeCreate), false);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error in UpdateUsingPipeCreate");
                Logger.LogMethodExit(nameof(UpdateUsingPipeCreate), false);
                return false;
            }
        }

        /// <summary>
        /// Phương án 3: Sử dụng Location curve để điều chỉnh endpoint
        /// </summary>
        private static bool UpdateUsingLocationCurve(Pipe pipe, Connector pipeConnector, Connector targetConnector)
        {
            Logger.LogMethodEntry(nameof(UpdateUsingLocationCurve));
            
            try
            {
                Logger.LogInfo("Attempting to update using location curve modification");
                
                // Cách này phức tạp hơn và phụ thuộc vào loại pipe
                // Có thể cần sử dụng Location curve và các parameter khác
                
                LocationCurve locationCurve = pipe.Location as LocationCurve;
                if (locationCurve == null) 
                {
                    Logger.LogWarning("Pipe không có LocationCurve");
                    Logger.LogMethodExit(nameof(UpdateUsingLocationCurve), false);
                    return false;
                }

                Line currentLine = locationCurve.Curve as Line;
                if (currentLine == null) 
                {
                    Logger.LogWarning("LocationCurve không phải là Line");
                    Logger.LogMethodExit(nameof(UpdateUsingLocationCurve), false);
                    return false;
                }

                // Tạo line mới từ điểm bắt đầu hiện tại đến target point
                XYZ startPoint = currentLine.GetEndPoint(0);
                XYZ endPoint = targetConnector.Origin;
                
                // Xác định điểm nào gần target hơn để thay thế
                double distanceToStart = startPoint.DistanceTo(targetConnector.Origin);
                double distanceToEnd = currentLine.GetEndPoint(1).DistanceTo(targetConnector.Origin);
                
                Line newLine;
                if (distanceToStart < distanceToEnd)
                {
                    // Thay thế start point
                    newLine = Line.CreateBound(targetConnector.Origin, currentLine.GetEndPoint(1));
                    Logger.LogInfo("Thay thế start point của pipe");
                }
                else
                {
                    // Thay thế end point
                    newLine = Line.CreateBound(currentLine.GetEndPoint(0), targetConnector.Origin);
                    Logger.LogInfo("Thay thế end point của pipe");
                }

                // Cập nhật location curve
                locationCurve.Curve = newLine;
                
                Logger.LogInfo("Successfully updated location curve");
                Logger.LogMethodExit(nameof(UpdateUsingLocationCurve), true);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error in UpdateUsingLocationCurve");
                Logger.LogMethodExit(nameof(UpdateUsingLocationCurve), false);
                return false;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết về pipe
        /// </summary>
        /// <param name="pipe">Pipe cần lấy thông tin</param>
        /// <returns>Chuỗi mô tả pipe</returns>
        public static string GetPipeInfo(Pipe pipe)
        {
            if (pipe == null) return "Null pipe";

            try
            {
                string info = $"Pipe ID: {pipe.Id}\n";
                info += $"Name: {pipe.Name}\n";
                info += $"Diameter: {pipe.Diameter}\n";
                info += $"Length: {pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble()}\n";
                
                LocationCurve locationCurve = pipe.Location as LocationCurve;
                if (locationCurve?.Curve is Line line)
                {
                    info += $"Start Point: {line.GetEndPoint(0)}\n";
                    info += $"End Point: {line.GetEndPoint(1)}\n";
                }

                // Thông tin về connectors
                ConnectorSet connectors = pipe.ConnectorManager?.Connectors;
                if (connectors != null)
                {
                    info += $"Number of connectors: {connectors.Size}\n";
                    int i = 1;
                    foreach (Connector connector in connectors)
                    {
                        info += $"Connector {i}: {connector.Origin}\n";
                        i++;
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                return $"Error getting pipe info: {ex.Message}";
            }
        }

        /// <summary>
        /// Kiểm tra pipe có hợp lệ để cập nhật không
        /// </summary>
        /// <param name="pipe">Pipe cần kiểm tra</param>
        /// <returns>True nếu pipe hợp lệ</returns>
        public static bool IsPipeValidForUpdate(Pipe pipe)
        {
            if (pipe == null) return false;

            try
            {
                // Kiểm tra pipe có connector không
                ConnectorSet connectors = pipe.ConnectorManager?.Connectors;
                if (connectors == null || connectors.Size == 0) return false;

                // Kiểm tra pipe có location curve không
                LocationCurve locationCurve = pipe.Location as LocationCurve;
                if (locationCurve?.Curve == null) return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Lấy danh sách các element đã kết nối với pipe (fittings, other pipes)
        /// </summary>
        /// <param name="pipe">Pipe cần tìm connected elements</param>
        /// <returns>Danh sách các element đã connect</returns>
        public static List<Element> GetConnectedElements(Pipe pipe)
        {
            Logger.LogMethodEntry(nameof(GetConnectedElements), pipe?.Id);
            
            List<Element> connectedElements = new List<Element>();
            
            try
            {
                ConnectorSet connectors = pipe.ConnectorManager?.Connectors;
                if (connectors == null)
                {
                    Logger.LogWarning("Pipe has no connectors");
                    return connectedElements;
                }

                foreach (Connector connector in connectors)
                {
                    if (connector.IsConnected)
                    {
                        Logger.LogInfo($"Pipe connector IsConnected: true, checking AllRefs...");
                        ConnectorSet refs = connector.AllRefs;
                        foreach (Connector refConnector in refs)
                        {
                            if (refConnector.Owner != null)
                            {
                                Logger.LogInfo($"Found ref owner: {refConnector.Owner.Name} (ID: {refConnector.Owner.Id}), Type: {refConnector.Owner.GetType().Name}, Category: {refConnector.Owner.Category?.Name}");
                                
                                if (refConnector.Owner.Id != pipe.Id && 
                                    !connectedElements.Any(e => e.Id == refConnector.Owner.Id))
                                {
                                    connectedElements.Add(refConnector.Owner);
                                    Logger.LogInfo($"Added connected element: {refConnector.Owner.Name} (ID: {refConnector.Owner.Id}), Type: {refConnector.Owner.GetType().Name}");
                                }
                                else if (refConnector.Owner.Id == pipe.Id)
                                {
                                    Logger.LogInfo($"Skipped self reference: {refConnector.Owner.Name} (ID: {refConnector.Owner.Id})");
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.LogInfo("Pipe connector IsConnected: false");
                    }
                }
                
                Logger.LogInfo($"Total connected elements found: {connectedElements.Count}");
                Logger.LogMethodExit(nameof(GetConnectedElements), connectedElements.Count);
                return connectedElements;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error getting connected elements");
                Logger.LogMethodExit(nameof(GetConnectedElements), 0);
                return connectedElements;
            }
        }
        
        /// <summary>
        /// Lấy connector gần nhất từ pipe đến target connector
        /// </summary>
        /// <param name="pipe">Pipe cần tìm connector</param>
        /// <param name="targetConnector">Target connector để so sánh khoảng cách</param>
        /// <returns>Connector gần nhất hoặc null</returns>
        public static Connector GetNearestConnector(Pipe pipe, Connector targetConnector)
        {
            Logger.LogMethodEntry(nameof(GetNearestConnector), pipe?.Id, targetConnector?.Owner?.Id);
            
            try
            {
                ConnectorSet pipeConnectors = pipe.ConnectorManager?.Connectors;
                if (pipeConnectors == null) return null;

                return FindNearestConnector(pipeConnectors, targetConnector.Origin);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error getting nearest connector");
                return null;
            }
        }
    }
}
using Backend.Data;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Backend.Services
{
    // =======================================================================
    // DỊCH VỤ QUẢN LÝ KẾT NỐI WEBSOCKET
    // =======================================================================

    /// <summary>
    /// Quản lý các kết nối WebSocket đang hoạt động.
    /// </summary>
    public interface IWebSocketConnectionManager
    {
        void AddSocket(string chargePointId, WebSocket socket);
        Task RemoveSocket(string chargePointId);
        WebSocket GetSocketById(string chargePointId);
        IEnumerable<WebSocket> GetAll();
    }

    /// <summary>
    /// Triển khai quản lý kết nối WebSocket trong bộ nhớ.
    /// </summary>
    public class InMemoryWebSocketConnectionManager : IWebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public void AddSocket(string chargePointId, WebSocket socket)
        {
            _sockets.TryAdd(chargePointId, socket);
        }

        public async Task RemoveSocket(string chargePointId)
        {
            if (_sockets.TryRemove(chargePointId, out var socket))
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived || socket.State == WebSocketState.CloseSent)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by server", CancellationToken.None);
                }
            }
        }

        public WebSocket GetSocketById(string chargePointId)
        {
            _sockets.TryGetValue(chargePointId, out var socket);
            return socket;
        }

        public IEnumerable<WebSocket> GetAll()
        {
            return _sockets.Values;
        }
    }


    // =======================================================================
    // DỊCH VỤ XỬ LÝ LOGIC OCPP
    // =======================================================================

    /// <summary>
    /// Chịu trách nhiệm xử lý logic cho các tin nhắn OCPP nhận được.
    /// </summary>
    public interface IOcppMessageHandler
    {
        Task HandleMessage(WebSocket webSocket, string chargePointId, string messageString);
    }

    public class OcppMessageHandler : IOcppMessageHandler
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OcppMessageHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task HandleMessage(WebSocket webSocket, string chargePointId, string messageString)
        {
            Console.WriteLine($"\n📥  Nhận được tin nhắn từ '{chargePointId}':");
            Console.WriteLine(messageString);

            try
            {
                var jsonMessage = JsonNode.Parse(messageString);
                int messageTypeId = jsonMessage[0].GetValue<int>();
                string messageId = jsonMessage[1].GetValue<string>();

                // Chỉ xử lý tin nhắn dạng CALL (typeId = 2)
                if (messageTypeId == 2)
                {
                    string action = jsonMessage[2].GetValue<string>();
                    JsonNode payload = jsonMessage[3];

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        switch (action)
                        {
                            case "BootNotification":
                                await HandleBootNotificationAsync(webSocket, chargePointId, messageId, payload, dbContext);
                                break;
                            case "Heartbeat":
                                await HandleHeartbeatAsync(webSocket, messageId, dbContext);
                                break;
                            case "StatusNotification":
                                await HandleStatusNotificationAsync(webSocket, chargePointId, messageId, payload, dbContext);
                                break;
                            case "Authorize":
                                await HandleAuthorizeAsync(webSocket, messageId, payload, dbContext);
                                break;
                            case "StartTransaction":
                                await HandleStartTransactionAsync(webSocket, chargePointId, messageId, payload, dbContext);
                                break;
                            case "MeterValues":
                                await HandleMeterValuesAsync(webSocket, chargePointId, messageId, payload, dbContext);
                                break;
                            case "StopTransaction":
                                await HandleStopTransactionAsync(webSocket, messageId, payload, dbContext);
                                break;
                            default:
                                Console.WriteLine($"-> Hành động '{action}' chưa được hỗ trợ.");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi khi xử lý tin nhắn OCPP: {ex.Message}");
                Console.ResetColor();
            }
        }

        // =======================================================================
        // CÁC HÀM XỬ LÝ CHO TỪNG HÀNH ĐỘNG OCPP
        // =======================================================================

        private async Task HandleBootNotificationAsync(WebSocket webSocket, string chargePointId, string messageId, JsonNode payload, AppDbContext _dbContext)
        {
            Console.WriteLine($"-> Trạm '{chargePointId}' gửi BootNotification. Đang xử lý...");

            Console.WriteLine($"-> LƯU Ý: Đang sử dụng trạm sạc có ID = 2 (hardcoded).");
            var station = await _dbContext.ChargingStations.FindAsync(2);

            if (station != null)
            {
                station.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"-> Đã cập nhật thông tin cho trạm '{station.Name}' (ID: 2).");
            }
            else
            {
                Console.WriteLine($"-> CẢNH BÁO: Không tìm thấy trạm sạc có ID '2' trong DB.");
            }

            var responsePayload = new JsonObject
            {
                ["status"] = "Accepted",
                ["currentTime"] = DateTime.UtcNow.ToString("o"),
                ["interval"] = 300
            };
            await SendOcppResponse(webSocket, messageId, responsePayload);
        }

        private async Task HandleHeartbeatAsync(WebSocket webSocket, string messageId, AppDbContext _dbContext)
        {
            var responsePayload = new JsonObject
            {
                ["currentTime"] = DateTime.UtcNow.ToString("o")
            };
            await SendOcppResponse(webSocket, messageId, responsePayload);
        }

        private async Task HandleStatusNotificationAsync(WebSocket webSocket, string chargePointId, string messageId, JsonNode payload, AppDbContext _dbContext)
        {
            var connectorId = payload["connectorId"].GetValue<int>();
            var status = payload["status"].GetValue<string>();
            Console.WriteLine($"-> Trạm '{chargePointId}', Cổng sạc {connectorId} có trạng thái mới: {status}");

            Console.WriteLine($"-> LƯU Ý: Đang sử dụng trạm sạc có ID = 2 (hardcoded).");
            var station = await _dbContext.ChargingStations
                .Include(s => s.Connectors)
                .FirstOrDefaultAsync(s => s.Id == 2);

            if (station == null)
            {
                Console.WriteLine($"LỖI: Không tìm thấy trạm sạc có ID '2' để cập nhật trạng thái.");
                await SendOcppResponse(webSocket, messageId, new JsonObject());
                return;
            }

            var connector = station.Connectors.FirstOrDefault(c => c.Id == connectorId);

            if (connector != null)
            {
                var newStatusEnum = MapOcppStatusToEnum(status);
                connector.Status = newStatusEnum;
                Console.WriteLine($"-> Cập nhật trạng thái cho Connector ID {connectorId} thành '{newStatusEnum}'.");

                // THAY ĐỔI: Thêm logic ánh xạ sang các connector ảo
                if (connectorId >= 1 )
                {
                    var virtualConnectorId = 21;
                    // TỐI ƯU HÓA: Tìm trực tiếp connector ảo
                    var virtualConnector = await _dbContext.Connectors.FindAsync(virtualConnectorId);

                    if (virtualConnector != null)
                    {
                        virtualConnector.Status = newStatusEnum; // Cập nhật trạng thái giống hệt
                        Console.WriteLine($"--> Ánh xạ: Cũng cập nhật trạng thái cho Connector ID {virtualConnectorId} thành '{newStatusEnum}'.");
                    }
                    else
                    {
                        Console.WriteLine($"--> CẢNH BÁO: Không tìm thấy connector ảo có ID {virtualConnectorId} để ánh xạ.");
                    }
                }

                try
                {
                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine("-> Đã lưu các thay đổi trạng thái vào DB.");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"!!! LỖI DATABASE: Không thể cập nhật trạng thái cho Connector. Lỗi: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine($"LỖI: Không tìm thấy Connector ID {connectorId} tại trạm '{station.Name}' (ID: 2).");
            }

            await SendOcppResponse(webSocket, messageId, new JsonObject());
        }

        private async Task HandleAuthorizeAsync(WebSocket webSocket, string messageId, JsonNode payload, AppDbContext _dbContext)
        {
            var idTag = payload["idTag"].GetValue<string>();
            var user = await _dbContext.Users.AnyAsync(u => u.Code == idTag);

            var status = user ? "Accepted" : "Blocked";
            Console.WriteLine($"-> Xác thực cho idTag '{idTag}': {status}");

            var responsePayload = new JsonObject
            {
                ["idTagInfo"] = new JsonObject
                {
                    ["status"] = status
                }
            };
            await SendOcppResponse(webSocket, messageId, responsePayload);
        }

        private async Task HandleStartTransactionAsync(WebSocket webSocket, string chargePointId, string messageId, JsonNode payload, AppDbContext _dbContext)
        {
            var connectorId = payload["connectorId"].GetValue<int>();
            var idTag = payload["idTag"].GetValue<string>();
            Console.WriteLine($"-> Bắt đầu phiên sạc cho idTag '{idTag}' tại cổng {connectorId}");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Code == idTag);
            if (user == null)
            {
                Console.WriteLine($"LỖI: Không tìm thấy người dùng với idTag '{idTag}'. Bỏ qua StartTransaction.");
                return;
            }

            Console.WriteLine($"-> LƯU Ý: Đang sử dụng trạm sạc có ID = 2 (hardcoded).");
            var station = await _dbContext.ChargingStations
                .Include(s => s.Connectors)
                .FirstOrDefaultAsync(s => s.Id == 2);

            if (station == null)
            {
                Console.WriteLine($"LỖI: Không tìm thấy trạm sạc với ID '2' trong database.");
                return;
            }

            var connector = station.Connectors.FirstOrDefault(c => c.Id == connectorId);
            if (connector == null)
            {
                Console.WriteLine($"LỖI: Không tìm thấy connector ID '{connectorId}' tại trạm '{chargePointId}'.");
                return;
            }

            var newSession = new ChargingSession
            {
                StartTime = DateTime.UtcNow,
                EnergyConsumedKWh = 0,
                TotalCost = 0,
                UserId = user.Id,
                ConnectorId = connector.Id,
                PaymentMethod = "AppWallet",
                PaymentStatus = PaymentStatus.Pending,
                AuthorizationIdTag = idTag,
            };

            try
            {
                _dbContext.ChargingSessions.Add(newSession);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"-> Đã tạo ChargingSession mới với ID: {newSession.Id}");

                newSession.OcppTransactionId = newSession.Id;
                await _dbContext.SaveChangesAsync();

                var responsePayload = new JsonObject
                {
                    ["transactionId"] = newSession.OcppTransactionId,
                    ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" }
                };
                await SendOcppResponse(webSocket, messageId, responsePayload);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"!!! LỖI DATABASE: Không thể tạo ChargingSession. Lỗi: {ex.Message}");
                Console.ResetColor();
            }
        }

        private async Task HandleMeterValuesAsync(WebSocket webSocket, string chargePointId, string messageId, JsonNode payload, AppDbContext _dbContext)
        {
            var ocppTransactionId = payload["transactionId"].GetValue<int>();
            var meterValueNode = payload["meterValue"][0];
            var sampledValueNode = meterValueNode["sampledValue"][0];

            var valueString = sampledValueNode["value"].GetValue<string>();

            if (decimal.TryParse(valueString, out var value))
            {
                Console.WriteLine($"-> Nhận được MeterValue cho Transaction ID {ocppTransactionId}: {value} Wh");

                var session = await _dbContext.ChargingSessions.FirstOrDefaultAsync(cs => cs.OcppTransactionId == ocppTransactionId);

                if (session != null)
                {
                    session.EnergyConsumedKWh = value / 1000;
                    try
                    {
                        await _dbContext.SaveChangesAsync();
                        Console.WriteLine($"-> Đã cập nhật EnergyConsumedKWh cho Session ID {session.Id} thành {session.EnergyConsumedKWh} kWh.");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"!!! LỖI DATABASE: Không thể cập nhật MeterValue. Lỗi: {ex.Message}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.WriteLine($"LỖI: Không tìm thấy ChargingSession với OcppTransactionId '{ocppTransactionId}' để cập nhật MeterValue.");
                }
            }
            else
            {
                Console.WriteLine($"LỖI: Không thể parse giá trị MeterValue '{valueString}' thành số.");
            }

            await SendOcppResponse(webSocket, messageId, new JsonObject());
        }

        private async Task HandleStopTransactionAsync(WebSocket webSocket, string messageId, JsonNode payload, AppDbContext _dbContext)
        {
            var ocppTransactionId = payload["transactionId"].GetValue<int>();
            var meterStop = payload["meterStop"].GetValue<decimal>();
            var reason = payload["reason"]?.GetValue<string>();

            Console.WriteLine($"-> Kết thúc phiên sạc cho Transaction ID: {ocppTransactionId}");

            var session = await _dbContext.ChargingSessions
                .Include(cs => cs.Connector).ThenInclude(c => c.ChargingStation)
                .Include(cs => cs.User)
                .FirstOrDefaultAsync(cs => cs.OcppTransactionId == ocppTransactionId);

            if (session == null)
            {
                Console.WriteLine($"LỖI: Không tìm thấy ChargingSession với OcppTransactionId '{ocppTransactionId}'.");
                await SendOcppResponse(webSocket, messageId, new JsonObject { ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" } });
                return;
            }

            var energyKWh = meterStop / 1000;
            var totalCost = energyKWh * session.Connector.ChargingStation.DefaultPricePerKWh;

            session.EndTime = DateTime.UtcNow;
            session.EnergyConsumedKWh = energyKWh;
            session.StopReason = reason;
            session.TotalCost = totalCost;

            if (session.User.Balance >= totalCost)
            {
                session.User.Balance -= totalCost;
                session.PaymentStatus = PaymentStatus.Paid;
                session.PaymentGatewayTransactionId = $"WALLET_TXN_{Guid.NewGuid()}";
                Console.WriteLine($"-> Thanh toán thành công. Số dư còn lại của user '{session.User.Username}': {session.User.Balance}");
            }
            else
            {
                session.PaymentStatus = PaymentStatus.Failed;
                Console.WriteLine($"-> Thanh toán thất bại. Số dư không đủ.");
            }

            try
            {
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"-> Đã cập nhật và kết thúc ChargingSession ID: {session.Id}");

                var responsePayload = new JsonObject
                {
                    ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" }
                };
                await SendOcppResponse(webSocket, messageId, responsePayload);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"!!! LỖI DATABASE: Không thể kết thúc ChargingSession. Lỗi: {ex.Message}");
                Console.ResetColor();
            }
        }

        // =======================================================================
        // CÁC HÀM TIỆN ÍCH
        // =======================================================================

        private async Task SendOcppResponse(WebSocket webSocket, string messageId, JsonObject payload)
        {
            var ocppResponse = new JsonArray(3, messageId, payload);
            string responseString = ocppResponse.ToJsonString();

            Console.WriteLine($"<- Gửi phản hồi: {responseString}");

            var responseBytes = Encoding.UTF8.GetBytes(responseString);
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private ChargingStationStatus MapOcppStatusToEnum(string ocppStatus)
        {
            return ocppStatus switch
            {
                "Available" => ChargingStationStatus.Available,
                "Preparing" => ChargingStationStatus.Charging,
                "Charging" => ChargingStationStatus.Charging,
                "SuspendedEV" => ChargingStationStatus.Charging,
                "SuspendedEVSE" => ChargingStationStatus.Charging,
                "Finishing" => ChargingStationStatus.Available,
                "Reserved" => ChargingStationStatus.Reserved,
                "Unavailable" => ChargingStationStatus.Unavailable,
                "Faulted" => ChargingStationStatus.Unavailable,
                _ => ChargingStationStatus.Unknown,
            };
        }
    }
}


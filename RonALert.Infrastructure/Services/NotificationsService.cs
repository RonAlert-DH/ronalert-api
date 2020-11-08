using CorePush.Google;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RonALert.Infrastructure.Services
{
    public interface INotificationsService
    {
        Task SendNotificationAsync(string message,
            CancellationToken ct = default);
    }

    public class NotificationsService : INotificationsService
    {
        private readonly FcmSender _fcmSender;
        private readonly string _token;

        public NotificationsService(string senderId, string serverKey, string token)
        {
            _fcmSender = new FcmSender(new FcmSettings()
            {
                SenderId = senderId,
                ServerKey = serverKey,
            }, new System.Net.Http.HttpClient());
            _token = token;
        }

        public async Task SendNotificationAsync(string message,
            CancellationToken ct = default)
        {
            await _fcmSender.SendAsync(_token, message);
        }
    }
}

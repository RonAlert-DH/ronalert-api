using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RonALert.Infrastructure.Database;
using RonALert.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace RonALert.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureSqlRepository(this IServiceCollection services, 
            string connectionString)
        {
            services.AddDbContextPool<RepositoryContext>(options => 
            options.UseSqlServer(connectionString), poolSize: 20);
        }

        public static void ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<IRoomService, RoomService>();
            services.AddScoped<IPersonPositionService, PersonPositionService>();
            services.AddScoped<IAlarmService, AlarmService>();
        }

        public static void ConfigureNotifications(this IServiceCollection services,
            string senderId, string serverKey, string token)
        {
            services.AddScoped<INotificationsService>(x => 
                new NotificationsService(senderId, serverKey, token));
        }
    }
}

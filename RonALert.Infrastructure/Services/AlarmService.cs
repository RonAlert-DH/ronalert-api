using Microsoft.EntityFrameworkCore;
using RonALert.Core.Entities;
using RonALert.Core.Models;
using RonALert.Core.Shared.Enums;
using RonALert.Infrastructure.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RonALert.Infrastructure.Services
{
    public interface IAlarmService
    {
        Task AddAlarmAsync(Alarm alarm,
            CancellationToken ct = default);

        Task UpdateAlarmAsync(Alarm alarm,
            CancellationToken ct = default);

        Task<List<Alarm>> GetAlarmsForRoomAsync(Room room,
            CancellationToken ct = default);

        Task CheckAlarmsAsync(Room room, List<PersonDTO> people,
            CancellationToken ct = default);
    }

    public class AlarmService : IAlarmService
    {
        private readonly RepositoryContext _repository;
        private readonly INotificationsService _notificationsService;

        public AlarmService(RepositoryContext repository, INotificationsService notificationsService)
        {
            _repository = repository;
            _notificationsService = notificationsService;
        }

        public async Task AddAlarmAsync(Alarm alarm,
            CancellationToken ct = default)
        {
            await _repository.Alarms.AddAsync(alarm, ct);
            await _repository.SaveChangesAsync(ct);
        }

        public async Task UpdateAlarmAsync(Alarm alarm, CancellationToken ct = default)
        {
            _repository.Alarms.Update(alarm);
            await _repository.SaveChangesAsync(ct);
        }

        public async Task CheckAlarmsAsync(Room room, List<PersonDTO> people, 
            CancellationToken ct = default)
        {
            var activeAlarms = await GetAlarmsByStatusForRoomAsync(room, AlarmStatus.Open, ct);

            await HandleFaceMaskAlarms(room, people, activeAlarms, ct);
            await HandlePeopleTooCloseAlarms(room, people, activeAlarms, ct);

            return;
        }

        public async Task<List<Alarm>> GetAlarmsForRoomAsync(Room room,
            CancellationToken ct = default) =>
            await _repository.Alarms
                .Where(x => x.Room == room)
                .ToListAsync(ct);

        public async Task<List<Alarm>> GetAlarmsByStatusForRoomAsync(Room room,
            AlarmStatus status, CancellationToken ct = default) =>
            await _repository.Alarms
                .Where(x => x.Room == room && x.Status == status)
                .ToListAsync(ct);

        private async Task HandleFaceMaskAlarms(Room room, List<PersonDTO> people,
            List<Alarm> activeAlarms, CancellationToken ct = default)
        {
            if (people.Any(x => !x.FaceMask))
            {
                if (!activeAlarms.Any(x => x.Type == AlarmType.NoFaceMask))
                {
                    var faceMaskAlarm = new Alarm()
                    {
                        Room = room,
                        Type = AlarmType.NoFaceMask,
                        Status = AlarmStatus.Open
                    };

                    await AddAlarmAsync(faceMaskAlarm, ct);
                }
            }
            else
            {
                if (activeAlarms.Any(x => x.Type == AlarmType.NoFaceMask))
                {
                    var faceMaskAlarm = activeAlarms.FirstOrDefault(x => x.Type == AlarmType.NoFaceMask);
                    faceMaskAlarm.Status = AlarmStatus.Closed;

                    await UpdateAlarmAsync(faceMaskAlarm, ct);
                }
            }
        }

        private async Task HandlePeopleTooCloseAlarms(Room room, List<PersonDTO> people,
            List<Alarm> activeAlarms, CancellationToken ct = default)
        {
            if (people.Any(x => x.NearestDistance < 150))
            {
                if (!activeAlarms.Any(x => x.Type == AlarmType.PeopleTooClose))
                {
                    var faceMaskAlarm = new Alarm()
                    {
                        Room = room,
                        Type = AlarmType.PeopleTooClose,
                        Status = AlarmStatus.Open
                    };

                    await AddAlarmAsync(faceMaskAlarm, ct);
                    await _notificationsService.SendNotificationAsync($"lalaland alarm opened {faceMaskAlarm.Type}");
                }
            }
            else
            {
                if (activeAlarms.Any(x => x.Type == AlarmType.PeopleTooClose))
                {
                    var faceMaskAlarm = activeAlarms.FirstOrDefault(x => x.Type == AlarmType.PeopleTooClose);
                    faceMaskAlarm.Status = AlarmStatus.Closed;

                    await UpdateAlarmAsync(faceMaskAlarm, ct);
                }
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RonALert.Core.Entities;
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

        Task<List<Alarm>> GetAlarmsForRoomAsync(Room room,
            CancellationToken ct = default);
    }

    public class AlarmService : IAlarmService
    {
        private readonly RepositoryContext _repository;

        public AlarmService(RepositoryContext repository)
        {
            _repository = repository;
        }

        public async Task AddAlarmAsync(Alarm alarm,
            CancellationToken ct = default)
        {
            await _repository.Alarms.AddAsync(alarm, ct);
            await _repository.SaveChangesAsync(ct);
        }

        public async Task<List<Alarm>> GetAlarmsForRoomAsync(Room room,
            CancellationToken ct = default) =>
            await _repository.Alarms
                .Where(x => x.Room == room)
                .ToListAsync(ct);
    }
}

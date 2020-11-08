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
    public interface IRoomService
    {
        Task<List<Room>> GetRoomsAsync(CancellationToken ct = default);
        Task<Room> GetRoomByIdAsync(Guid id, CancellationToken ct = default);
    }

    public class RoomService : IRoomService
    {
        private readonly RepositoryContext _repository;

        public RoomService(RepositoryContext repository)
        {
            _repository = repository;
        }

        public async Task<List<Room>> GetRoomsAsync(CancellationToken ct = default) =>
            await _repository.Rooms.ToListAsync(ct);

        public async Task<Room> GetRoomByIdAsync(Guid id, CancellationToken ct = default) =>
            await _repository.Rooms.FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}

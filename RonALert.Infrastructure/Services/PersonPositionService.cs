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
    public interface IPersonPositionService
    {
        Task<List<PersonPosition>> GetPersonPositionsForRoomAsnyc(Room room,
            CancellationToken ct = default);

        Task AddPersonPositionAsync(PersonPosition personPosition,
            CancellationToken ct = default);
    }

    public class PersonPositionService : IPersonPositionService
    {
        private readonly RepositoryContext _repository;

        public PersonPositionService(RepositoryContext repository)
        {
            _repository = repository;
        }

        public async Task AddPersonPositionAsync(PersonPosition personPosition,
            CancellationToken ct = default)
        {
            await _repository.PersonPositions.AddAsync(personPosition, ct);
            await _repository.SaveChangesAsync(ct);
        }

        public async Task<List<PersonPosition>> GetPersonPositionsForRoomAsnyc(Room room,
            CancellationToken ct = default) =>
            await _repository.PersonPositions
                .Where(x => x.Room == room)
                .ToListAsync(ct);
    }
}

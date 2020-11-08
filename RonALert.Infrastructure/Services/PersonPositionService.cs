using Microsoft.EntityFrameworkCore;
using RonALert.Core.Entities;
using RonALert.Core.Models;
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

        Task AddPeoplePositionsAsync(Room room, List<PersonDTO> people,
            DateTime timestamp, CancellationToken ct = default);
    }

    public class PersonPositionService : IPersonPositionService
    {
        private readonly RepositoryContext _repository;

        public PersonPositionService(RepositoryContext repository)
        {
            _repository = repository;
        }

        public async Task AddPeoplePositionsAsync(Room room, List<PersonDTO> people, 
            DateTime timestamp, CancellationToken ct = default)
        {
            foreach(var person in people)
            {
                var personPosition = new PersonPosition()
                {
                    Room = room,
                    PositionX = person.PositionX,
                    PositionY = person.PositionY,
                    FaceMask = person.FaceMask,
                    NearestDistance = person.NearestDistance,
                    Timestamp = timestamp,
                };

                await AddPersonPositionAsync(personPosition, ct);
            }
        }

        public async Task AddPersonPositionAsync(PersonPosition personPosition,
            CancellationToken ct = default)
        {
            await _repository.PersonPositions.AddAsync(personPosition, ct);
            await _repository.SaveChangesAsync(ct);
        }

        public async Task<List<PersonPosition>> GetPersonPositionsForRoomAsnyc(Room room,
            CancellationToken ct = default)
        {
            var timestamp = (await _repository.PersonPositions
                .OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync(ct)).Timestamp;

            return await _repository.PersonPositions
                .Where(x => x.Room == room && x.Timestamp == timestamp)
                .ToListAsync(ct);
        }

    }
}

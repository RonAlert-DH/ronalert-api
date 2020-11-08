using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RonALert.Core.Entities;
using RonALert.Core.Models;
using RonALert.Infrastructure.Services;

namespace RonALert.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        public readonly IRoomService _roomService;
        public readonly IAlarmService _alarmService;
        public readonly IPersonPositionService _personPositionService;
        public readonly ILogger<RoomController> _logger;

        // room guid 9e9c13b6-3fc6-4875-bd37-b9511de0b082

        public RoomController(IRoomService roomService, IAlarmService alarmService,
            IPersonPositionService personPositionService, ILogger<RoomController> logger)
        {
            _roomService = roomService;
            _alarmService = alarmService;
            _personPositionService = personPositionService;
            _logger = logger;
        }

        /// <summary>
        /// Get list of all rooms
        /// </summary>
        /// <response code="200">Returns list of all rooms</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<Room>>> Index(CancellationToken ct = default)
        {
            try
            {
                var rooms = await _roomService.GetRoomsAsync(ct);

                return Ok(rooms);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception running {Controller} {Action}",
                   "Room", "Index");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Add people positions
        /// </summary>
        /// <response code="200">Returns list of all rooms</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [Route("{id}/people")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PersonPosition>>> AddPositions(Guid id, List<PersonDTO> people, CancellationToken ct = default)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                var room = await _roomService.GetRoomByIdAsync(id, ct);

                if (room.IsNull())
                    return NotFound();

                await _personPositionService.AddPeoplePositionsAsync(room, people, timestamp, ct);
                await _alarmService.CheckAlarmsAsync(room, people, ct);

                return Ok(timestamp);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception running {Controller} {Action}",
                   "Room", "Index");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get list of all alarms for a room
        /// </summary>
        /// <response code="200">Returns list of all alarms for a room</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [Route("{id}/alarms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<Room>>> RoomAlarms(Guid id, [FromQuery] bool openOnly = false,
            CancellationToken ct = default)
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(id, ct);

                if (room.IsNull())
                    return NotFound();

                var roomAlarms = openOnly ? 
                    await _alarmService.GetAlarmsByStatusForRoomAsync(room, Core.Shared.Enums.AlarmStatus.Open, ct) :
                    await _alarmService.GetAlarmsForRoomAsync(room, ct);

                return Ok(roomAlarms);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception running {Controller} {Action}",
                   "Room", "Index");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get list of all alarms for a room
        /// </summary>
        /// <response code="200">Returns list of all alarms for a room</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [Route("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<Room>>> RoomStatus(Guid id, CancellationToken ct = default)
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(id, ct);

                if (room.IsNull())
                    return NotFound();

                var peopleInRoom = await _personPositionService.GetPersonPositionsForRoomAsnyc(room, ct);

                return Ok(peopleInRoom);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception running {Controller} {Action}",
                   "Room", "Index");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}

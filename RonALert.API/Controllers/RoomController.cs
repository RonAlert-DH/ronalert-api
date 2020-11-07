using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RonALert.Core.Entities;
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
    }
}

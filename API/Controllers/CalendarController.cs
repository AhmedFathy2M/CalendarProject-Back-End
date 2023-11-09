using Core.Dtos;
using Core.Entities;
using Core.Helpers;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeZoneConverter;

namespace API.Controllers;

[Authorize(AuthenticationSchemes = "Bearer")]
public class CalendarController : BaseController
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    /// <summary>
    /// Creates an event in the calendar and handles the time validation.
    /// </summary>
    /// <param name="calendarPayload">The input parameter of the calendar which can include attachments</param>
    /// <returns>EventDto. A dto that contains the important information of the event</returns>
    [HttpPost]
    public async Task<ActionResult<EventDto>> CreateGoogleCalendarEvent([FromBody]GoogleCalendarPayload calendarPayload)
    {
        var googleToken = GetUserGoogleToken();
        if (string.IsNullOrEmpty(googleToken))
            return BadRequest(new { Message = "Please authenticate your google calendar account first" });

		string cairoTimeZoneId = "Africa/Cairo";

		// Get the time zone information for Cairo
		TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo(cairoTimeZoneId);

		DateTime startInTimeZone = TimeZoneInfo.ConvertTimeFromUtc(calendarPayload.Start, timeZone);
        DateTime nowInTimeZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        if (startInTimeZone < nowInTimeZone)
        {
            return BadRequest(new { Message = "Cannot create events in the past." });
        }

        if (startInTimeZone.DayOfWeek == DayOfWeek.Friday || startInTimeZone.DayOfWeek == DayOfWeek.Saturday)
        {
            return BadRequest(new { Message = "Events are not allowed on Fridays or Saturdays." });
        }

        var result = await _calendarService.CreateEvent(calendarPayload, googleToken);
        return Ok(result);
    }

    /// <summary>
    /// Deletes an event from a calendar using the eventId
    /// </summary>
    /// <param name="eventId">Used to find the event that the user wants to delete</param>
    /// <returns>A confirmation that it was deleted</returns>
    [HttpDelete]
    public async Task<ActionResult> DeleteGoogleCalendarEvent(string eventId)
    {
        var googleToken = GetUserGoogleToken();
        if (string.IsNullOrEmpty(googleToken))
            return BadRequest(new { Message = "Please authenticate your google calendar account first" });

        return Ok(await _calendarService.DeleteCalendarEvent(eventId, googleToken));
    }

    /// <summary>
    /// Gets all the calendars while implementing some sorting depending on input that the user sends.
    /// </summary>
    /// <param name="EventParameterFilterationDto">Used to determine the kind of sorting and events that the user wants</param>
    /// <param name="eventParams"></param>
    /// <returns>The events paginated, if there are any to be found</returns>
    [HttpGet]
    public async Task<ActionResult<Pagination<EventsDto>>> GetGoogleCalendarEvents(
        [FromQuery] EventParameterFiltrationDto eventParams)
    {
        var googleToken = GetUserGoogleToken();
        if (string.IsNullOrEmpty(googleToken))
            return BadRequest(new { Message = "Please authenticate your google calendar account first" });

        var result = await _calendarService.GetCalendarsEvents(eventParams, googleToken);
        return Ok(result);
    }

    /// <summary>
    /// deletes all events in a calendar
    /// </summary>
    /// <param name="calendarId">the id of the calendar to be deleted</param>
    /// <returns></returns>
    [HttpDelete("truncate")]
    public async Task<ActionResult> DeleteAllGoogleCalendarEvents(string calendarId)
    {
        var googleToken = GetUserGoogleToken();
        if (string.IsNullOrEmpty(googleToken))
            return BadRequest(new { Message = "Please authenticate your google calendar account first" });

        return Ok(await _calendarService.DeleteAllCalendarEvents(calendarId, googleToken));
    }
}
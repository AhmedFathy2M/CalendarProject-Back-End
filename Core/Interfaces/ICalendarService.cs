using Core.Helpers;
using Core.Payloads;
using Core.Dtos;
using Core.Entities;

namespace Core.Interfaces;

public interface ICalendarService
{
    Task<GoogleTokenResponse> GetTokens(string code);
    string GetAuthCode();
    Task<EventDto> CreateEvent(GoogleCalendarPayload payload, string googleToken);
    Task<UserDto> SaveGoogleRefreshToken(SaveGoogleRefreshTokenPayload payload, string userId);

    /// <summary>
    /// Deletes calendar event using the eventId
    /// </summary>
    /// <param name="eventId">the eventId for the event to be deleted</param>
    /// <returns>confirmation</returns>
    Task<string> DeleteCalendarEvent(string eventId, string refreshToken);

    /// <summary>
    /// applies sorting,ranging, and pagination according to the input sent by the user
    /// </summary>
    /// <param name="eventParams">includes the properties needed for sorting and paging</param>
    /// <param name="googleToken">User's google token</param>
    /// <returns>a full list of events if found</returns>
    Task<Pagination<EventDto>> GetCalendarsEvents(EventParameterFiltrationDto eventParams,
        string googleToken);

    /// <summary>
    /// deletes all events in a calendar through the calendarId
    /// </summary>
    /// <param name="calendarId">the id of the calendar to be deleted</param>
    /// <param name="googleToken"></param>
    /// <returns>confirmation</returns>
    Task<string> DeleteAllCalendarEvents(string calendarId, string googleToken);
}
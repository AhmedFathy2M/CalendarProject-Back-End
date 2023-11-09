using System.Text;
using Core.Dtos;
using Core.Helpers;
using Core.Payloads;
using Core.Entities;
using Core.Entities.IdentityEntities;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.TokenValidationInterface;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Drive.v3;
using System.Net.Mail;
using Google.Apis.Util.Store;
using System.Net;

namespace Infrastructure.Data.Repositories.Services;

public class CalendarService : ICalendarService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly UserManager<AppUser> _userManager;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ITokenService _tokenService;

	public CalendarService(IConfiguration configuration, UserManager<AppUser> userManager, ITokenService tokenService)
    {
        _configuration = configuration;
        _userManager = userManager;
        _tokenService = tokenService;
        _httpClient = new HttpClient();
        _clientId = _configuration["GoogleClientConfig:ClientId"];
        _clientSecret = _configuration["GoogleClientConfig:ClientSecret"];

    }

	public string GetAuthCode()
	{
		var redirectUri = $"https://my-calendar-be.azurewebsites.net/api/account/auth/google";
		var redirectUriEncode = UrlEncodeForGoogle(redirectUri);
		var prompt = "consent";
		var responseType = "code";
		var clientId = _clientId;
		var scope = "https://www.googleapis.com/auth/calendar " +
				"https://www.googleapis.com/auth/drive.appdata " +
				"https://www.googleapis.com/auth/drive.appfolder " +
				"https://www.googleapis.com/auth/drive " +
				"https://www.googleapis.com/auth/drive.readonly " +
				"https://www.googleapis.com/auth/calendar.readonly";
		var accessType = "offline";

		var scopeUrl =
			$"https://accounts.google.com/o/oauth2/auth?redirect_uri={redirectUriEncode}&prompt={prompt}&response_type={responseType}&client_id={clientId}&scope={scope}&access_type={accessType}";

		return scopeUrl;
	}

	public async Task<GoogleTokenResponse> GetTokens(string code)
    {
        // we get authorization token from google and exchange it for access token and refresh tokens
        var redirectUrl = @"https://my-calendar-be.azurewebsites.net/api/account/auth/google";
        var tokenEndpoint = _configuration["GoogleClientConfig:TokenEndpoint"];
        var content =
            new StringContent(
                $"code={code}&redirect_uri={Uri.EscapeDataString(redirectUrl)}&client_id={_clientId}&client_secret={_clientSecret}&grant_type=authorization_code",
                Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await _httpClient.PostAsync(tokenEndpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleTokenResponse>(responseContent);

            return tokenResponse;
        }

        throw new BadRequestException("Failed to authenticate for google");
    }
	public async Task<UserDto> SaveGoogleRefreshToken(SaveGoogleRefreshTokenPayload payload, string userId)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(i => i.Id == userId);

		user.GoogleRefreshToken = payload.RefreshToken;

		await _userManager.UpdateAsync(user);

		// we create new token for user with the google token

		var userDto = new UserDto
		{
			DisplayName = user.DisplayName,
			Email = user.Email,
			Token = _tokenService.CreateToken(user)
		};

		return userDto;
	}

	private Google.Apis.Calendar.v3.CalendarService CreateGoogleCalendarService(string refreshToken)
	{
		var credentials = new UserCredential(new GoogleAuthorizationCodeFlow(
			new GoogleAuthorizationCodeFlow.Initializer
			{
				ClientSecrets = new ClientSecrets
				{
					ClientId = _clientId,
					ClientSecret = _clientSecret
				}
			}), "user", new TokenResponse { RefreshToken = refreshToken });

		var calendarService = new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer()
		{
			HttpClientInitializer = credentials,
			ApplicationName = "My Calendar"
		});
		return calendarService;
	}
	private DriveService CreateGoogleDriveService(string refreshToken)
	{
		var initializer = new GoogleAuthorizationCodeFlow.Initializer
		{
			ClientSecrets = new ClientSecrets
			{
				ClientId = _clientId,
				ClientSecret = _clientSecret
			},
			Scopes = new[]
			{
			DriveService.Scope.Drive,
			DriveService.Scope.DriveFile
		}
		};

		var flow = new GoogleAuthorizationCodeFlow(initializer);
		var credential = new UserCredential(flow, "user", new TokenResponse { RefreshToken = refreshToken });

		return new DriveService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "My Calendar"
		});
	}
	public async Task<EventDto> CreateEvent(GoogleCalendarPayload payload, string googleToken)
	{
		var calendarService = CreateGoogleCalendarService(googleToken);
		var _driveService = CreateGoogleDriveService(googleToken);
		var eventCalendar = new Event
		{
			Summary = payload.Summary,
			Location = payload.Location,
			Start = new EventDateTime() { DateTime = payload.Start, TimeZone = "Africa/Cairo" },
			End = new EventDateTime() { DateTime = payload.End, TimeZone = "Africa/Cairo" },
			Description = payload.Description
		};

		if (payload.MimeType != null && payload.FileUrl != null && payload.fileId != null && payload.Title != null)
		{
			Google.Apis.Drive.v3.Data.File file = _driveService.Files.Get(payload.fileId).Execute();
			var attachment = new EventAttachment()
			{
				FileUrl = $"https://drive.google.com/file/d/{file.Id}/view",
				MimeType = file.MimeType,
				Title = file.Name
			};
			eventCalendar.Description = $"Attachment: Title--- {attachment.Title}, File Type--- {attachment.MimeType}, Link--- {attachment.FileUrl}";
			eventCalendar.Attachments = new List<EventAttachment>();
            eventCalendar.Attachments.Add(attachment);
		}

		var eventRequest = calendarService.Events.Insert(eventCalendar, "primary");
		var createRequest = await eventRequest.ExecuteAsync();

		return new EventDto
		{
			Id = createRequest.Id,
			Url = createRequest.HtmlLink,
			EventEnd = createRequest.End.DateTime,
			EventStart = createRequest.Start.DateTime,
			AttachmentTitle = eventCalendar.Attachments.FirstOrDefault().Title,
			AttachmentLink = eventCalendar.Attachments.FirstOrDefault().FileUrl
		};
	}

	/// <summary>
	/// Deletes calendar event using the eventId
	/// </summary>
	/// <param name="eventId">the eventId for the event to be deleted</param>
	/// <param name="refreshToken">Google's refresh token</param>
	/// <returns>confirmation</returns>
	public async Task<string> DeleteCalendarEvent(string eventId, string refreshToken)
    {
        var googleCalendarService = CreateGoogleCalendarService(refreshToken);
        var result = await googleCalendarService.Events.Delete("primary", eventId).ExecuteAsync();
        return result;
    }

    /// <summary>
    /// applies sorting,ranging, and pagination according to the input sent by the user
    /// </summary>
    /// <param name="eventParams">includes the properties needed for sorting and paging</param>
    /// <param name="googleToken">User's google token</param>
    /// <returns>a full list of events if found</returns>
    public async Task<Pagination<EventDto>> GetCalendarsEvents(EventParameterFiltrationDto eventParams,
        string googleToken)
    {
        var googleCalendarService = CreateGoogleCalendarService(googleToken);
        var events = googleCalendarService.Events.List("primary");
        var eventsList = await events.ExecuteAsync();
        var filteredEvents = ApplySearch(eventParams.Search, eventsList);
        filteredEvents = ApplySort(eventParams.Sort, filteredEvents);
        filteredEvents = ApplyRanging(eventParams.FromDate, eventParams.ToDate, filteredEvents);
		var eventsDto = ApplyPagination(eventParams.PageIndex ?? 1, (eventParams.PageSize ?? filteredEvents.Count) > 8 ? 9 : eventParams.PageSize ?? filteredEvents.Count, filteredEvents);
		return eventsDto;
    }

    /// <summary>
    /// deletes all events in a calendar through the calendarId
    /// </summary>
    /// <param name="calendarId">the id of the calendar to be deleted</param>
    /// <param name="googleToken"></param>
    /// <returns>confirmation</returns>
    public async Task<string> DeleteAllCalendarEvents(string calendarId, string googleToken)
    {
        try
        {
            var googleCalendarService = CreateGoogleCalendarService(googleToken);
            return await googleCalendarService.Calendars.Clear(calendarId).ExecuteAsync();
        }
        catch (GoogleApiException ex)
        {
            return "Calendar is not found";
        }
    }

    #region Filtration, Sorting, Searching, Ranging Methods

    protected static List<Event> ApplySearch(string search, Events eventsList)
    {
        if (!string.IsNullOrEmpty(search))
        {
            var searchKey = search?.Trim().ToLower() ?? "";
            var filteredEvents = eventsList.Items.Where(i =>
                    (i.Summary != null && i.Summary.Contains(searchKey, StringComparison.OrdinalIgnoreCase)) ||
                    (i.Id != null && i.Id.Contains(searchKey, StringComparison.OrdinalIgnoreCase)) ||
                    (i.Start != null && i.Start.DateTime.ToString()
                        .Contains(searchKey, StringComparison.OrdinalIgnoreCase)) ||
                    (i.Description != null &&
                     i.Description.Contains(searchKey, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            return filteredEvents;
        }
        else
        {
            return eventsList.Items.ToList();
        }
    }

    protected static List<Event> ApplySort(string sort, List<Event> eventsList)
    {
        var eventsOnPage = eventsList;
        if (!string.IsNullOrWhiteSpace(sort))
        {
            switch (sort)
            {
                case "EventStartAscending":
                    eventsOnPage = eventsOnPage.OrderBy(x => x.Start.DateTime).ToList();
                    break;
                case "EventStartDescending":
                    eventsOnPage = eventsOnPage.OrderByDescending(x => x.Start.DateTime).ToList();
                    break;
                case "EventEndAscending":
                    eventsOnPage = eventsOnPage.OrderBy(x => x.End.DateTime).ToList();
                    break;
                case "EventEndDescending":
                    eventsOnPage = eventsOnPage.OrderByDescending(x => x.End.DateTime).ToList();
                    break;
                default:
                    eventsOnPage = eventsOnPage.OrderBy(x => x.End.DateTime).ToList();
                    break;
            }

            return eventsOnPage;
        }
        else
        {
            return eventsOnPage;
        }
    }

    protected static List<Event> ApplyRanging(DateTime? fromDate, DateTime? toDate, List<Event> eventsList)
    {
        if (fromDate.HasValue && toDate.HasValue)
        {
            return eventsList.Where(eventItem =>
                eventItem.Start.DateTime >= fromDate.Value && eventItem.End.DateTime <= toDate.Value).ToList();
        }
        else if (fromDate.HasValue)
        {
            return eventsList.Where(eventItem => eventItem.Start.DateTime >= fromDate.Value).ToList();
        }
        else if (toDate.HasValue)
        {
            return eventsList.Where(eventItem => eventItem.End.DateTime <= toDate.Value).ToList();
        }
        else
        {
            return eventsList;
        }
    }

    protected static Pagination<EventDto> ApplyPagination(int? pageIndex, int? pageSize, List<Event> eventsList)
    {
        var eventsOnPage = eventsList
            .Skip((pageIndex.Value - 1) * pageSize.Value)
            .Take(pageSize.Value).Select(eventItem => new EventDto
            {
                Id = eventItem.Id,
                Url = eventItem.HtmlLink,
                EventStart = eventItem.Start.DateTime,
                EventEnd = eventItem.End.DateTime
            }).ToList();

        return new Pagination<EventDto>(pageIndex.Value, pageSize.Value, eventsOnPage.Count, eventsOnPage);
    }

	#endregion

	private protected string UrlEncodeForGoogle( string url)
	{
		string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.~";
		var result = new StringBuilder();
		foreach (char symbol in url)
		{
			if (unreservedChars.IndexOf(symbol) != -1)
			{
				result.Append(symbol);
			}
			else
			{
				result.Append("%" + ((int)symbol).ToString("X2"));
			}
		}
		return result.ToString();
	}
  
}
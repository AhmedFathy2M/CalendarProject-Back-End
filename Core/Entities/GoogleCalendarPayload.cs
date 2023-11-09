namespace Core.Entities
{
	public class GoogleCalendarPayload
	{
		public string Summary { get; set; }
		public string Description { get; set; }
		public string Location { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public string? fileId { get; set; }
		public string? FileUrl { get; set; }
		public string? MimeType { get; set; }
		public string? Title { get; set; }
	}
}

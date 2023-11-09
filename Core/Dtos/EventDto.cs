
namespace Core.Dtos;

public class EventDto
{
	public string Id { get; set; }	
	public string Url { get; set; }
	public DateTime? EventStart { get; set; }
	public DateTime? EventEnd { get; set; }
	public string AttachmentLink { get; set; }
	public string AttachmentTitle{ get; set; }
}
namespace Core.Dtos;
public class EventParameterFiltrationDto
{
	public string? Search {get; set;}
	public string? Sort {get; set;}
	public int? PageIndex {get; set;}
	public int? PageSize {get; set; }
	public DateTime? FromDate { get; set;}
	public DateTime? ToDate{ get; set;}
}
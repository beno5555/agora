using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class PageQuery
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, Constants.MaxPageSize)]
    public int PageSize { get; set; } = Constants.DefaultPageSize;
}

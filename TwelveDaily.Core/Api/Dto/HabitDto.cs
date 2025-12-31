using System.ComponentModel.DataAnnotations;

namespace TwelveDaily.Core.Api.Dto;

public record CreateHabitDto(
    [property: Required] string Name,
    //[property: Required] int UserId,
    string? Description, 
    string? Icon,
    TimeOnly? Monday,
    TimeOnly? Tuesday,
    TimeOnly? Wednesday,
    TimeOnly? Thursday,
    TimeOnly? Friday,
    TimeOnly? Saturday,
    TimeOnly? Sunday
);

public record UpdateHabitDto(
    [property: Required] int Id,
    //[property: Required] int UserId,
    string? Name,
    string? Description,
    string? Icon,
    bool? Enabled,
    TimeOnly? Monday,
    TimeOnly? Tuesday,
    TimeOnly? Wednesday,
    TimeOnly? Thursday,
    TimeOnly? Friday,
    TimeOnly? Saturday,
    TimeOnly? Sunday
);
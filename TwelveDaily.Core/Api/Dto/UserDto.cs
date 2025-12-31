using System.ComponentModel.DataAnnotations;

namespace TwelveDaily.Core.Api.Dto;

public record UserCreateDto(
    [property: Required, StringLength(100, MinimumLength = 2)] string Name,
    [property: Required, EmailAddress, StringLength(254)] string Email, 
    [property: Required, StringLength(100, MinimumLength = 8)] string PlainTextPassword
);

public record UserUpdateDto(
    [property: Required, StringLength(100, MinimumLength = 2)] string Name,
    [property: Required, EmailAddress, StringLength(254)] string Email
);

public record UserLoginDto(
    [property: Required, EmailAddress, StringLength(254)] string Email, 
    [property: Required, StringLength(100, MinimumLength = 8)] string PlainTextPassword
);
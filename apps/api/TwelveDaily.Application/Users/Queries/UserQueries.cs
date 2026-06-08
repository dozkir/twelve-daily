using MediatR;

namespace TwelveDaily.Application.Users.Queries;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileResult>;

public record UserProfileResult(Guid Id, string Email, string Timezone, DateTime CreatedAt);


using System.Security.Claims;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Repositories;
using Microsoft.AspNetCore.Authentication;

namespace Jurassic.movieserviceapi.Http;

internal static class UserBookingsHandlers
{
    public static async Task<IResult> GetMyBookingsAsync(
        ClaimsPrincipal principal,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken)
    {
        var userTxt = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userTxt) || !Guid.TryParse(userTxt, out var webUserId))
        {
            return Results.Unauthorized();
        }

        var list = await bookingRepository.ListConfirmedBookingsForUserAsync(webUserId, cancellationToken);
        return Results.Ok(list);
    }
}

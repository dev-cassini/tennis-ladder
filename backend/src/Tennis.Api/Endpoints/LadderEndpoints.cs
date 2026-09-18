using Tennis.Api.Auth;
using Tennis.Application.Ladders;

namespace Tennis.Api.Endpoints;

public static class LadderEndpoints
{
    public static IEndpointRouteBuilder MapLadderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var ladders = endpoints.MapGroup("/api/ladders").WithTags("Ladders");

        ladders.MapGet("/", async (
            HttpContext httpContext,
            CurrentUserResolver currentUserResolver,
            LadderService ladderService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserResolver.ResolveAsync(httpContext, cancellationToken);
            var result = await ladderService.GetForUserAsync(currentUser.Id, cancellationToken);
            return Results.Ok(result);
        });

        ladders.MapPost("/", async (
            CreateLadderRequest request,
            HttpContext httpContext,
            CurrentUserResolver currentUserResolver,
            LadderService ladderService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserResolver.ResolveAsync(httpContext, cancellationToken);
            var result = await ladderService.CreateAsync(currentUser, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToErrorResult(result.Error!);
            }

            return Results.Created($"/api/ladders/{result.Value!.Id}", result.Value);
        });

        ladders.MapGet("/{ladderId:guid}/setup", async (
            Guid ladderId,
            HttpContext httpContext,
            CurrentUserResolver currentUserResolver,
            LadderService ladderService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserResolver.ResolveAsync(httpContext, cancellationToken);
            var result = await ladderService.GetSetupAsync(
                ladderId,
                currentUser.Id,
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToErrorResult(result.Error!);
        });

        ladders.MapPut("/{ladderId:guid}/players", async (
            Guid ladderId,
            ReplacePlayersRequest request,
            HttpContext httpContext,
            CurrentUserResolver currentUserResolver,
            LadderService ladderService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserResolver.ResolveAsync(httpContext, cancellationToken);
            var result = await ladderService.ReplacePlayersAsync(
                ladderId,
                currentUser.Id,
                request,
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToErrorResult(result.Error!);
        });

        ladders.MapPut("/{ladderId:guid}/order", async (
            Guid ladderId,
            UpdatePlayerOrderRequest request,
            HttpContext httpContext,
            CurrentUserResolver currentUserResolver,
            LadderService ladderService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserResolver.ResolveAsync(httpContext, cancellationToken);
            var result = await ladderService.UpdatePlayerOrderAsync(
                ladderId,
                currentUser.Id,
                request,
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToErrorResult(result.Error!);
        });

        return endpoints;
    }

    private static IResult ToErrorResult(LadderError error) => error.Code switch
    {
        "validation_error" => Results.ValidationProblem(
            error.ValidationErrors ?? new Dictionary<string, string[]>(),
            title: error.Message),
        "forbidden" => Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: error.Message),
        "not_found" => Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: error.Message),
        _ => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: error.Message)
    };
}

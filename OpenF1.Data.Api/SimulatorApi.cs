using Microsoft.EntityFrameworkCore;

namespace OpenF1.Data.Api;

public static class SimulatorApi
{
    public static WebApplication MapSimulatorApis(this WebApplication app)
    {
        _ = app
            .MapGet("simulator", async (LiveTimingDbContext dbContext) =>
            {
                var sessionNames = await dbContext
                    .RawTimingDataPoints
                    .Select(x => x.SessionName)
                    .Distinct()
                    .ToListAsync();

                return TypedResults.Ok(sessionNames);
            })
            .WithOpenApi();

        _ = app
            .MapPost("simulator", (StartSimulatorRequest request, ILiveTimingProvider liveTimingProvider) =>
            {
                liveTimingProvider.StartSimulatedSession(request.SessionName, request.SimulationName);
            })
            .WithOpenApi();

        return app;
    }
}

public sealed record StartSimulatorRequest
{
    public string SessionName { get; set; } = null!;
    public string SimulationName { get; set; } = null!;
}

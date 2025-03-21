using Microsoft.AspNetCore.Mvc;
using OpenF1.Data;

namespace OpenF1.Console;

public static class TimingEndpoints
{
    public static WebApplication MapTimingEndpoints(this WebApplication app)
    {
        app.MapLatestDataEndpoint<DriverListProcessor, DriverListDataPoint>()
            .MapLatestDataEndpoint<ExtrapolatedClockProcessor, ExtrapolatedClockDataPoint>()
            .MapLatestDataEndpoint<HeartbeatProcessor, HeartbeatDataPoint>()
            .MapLatestDataEndpoint<LapCountProcessor, LapCountDataPoint>()
            .MapLatestDataEndpoint<RaceControlMessageProcessor, RaceControlMessageDataPoint>()
            .MapLatestDataEndpoint<SessionInfoProcessor, SessionInfoDataPoint>()
            .MapLatestDataEndpoint<TimingAppDataProcessor, TimingAppDataPoint>()
            .MapLatestDataEndpoint<TimingDataProcessor, TimingDataPoint>()
            .MapLatestDataEndpoint<TrackStatusProcessor, TrackStatusDataPoint>()
            .MapLatestDataEndpoint<WeatherProcessor, WeatherDataPoint>();

        app.MapGet(
            "/data/TimingData/laps/{lapNumber}",
            ([FromRoute] int lapNumber, TimingDataProcessor processor) =>
            {
                return processor.DriversByLap.TryGetValue(lapNumber, out var data)
                    ? TypedResults.Ok(data)
                    : Results.NotFound();
            }
        );

        app.MapGet(
            "/data/TimingData/laps/best",
            (TimingDataProcessor processor) =>
            {
                return TypedResults.Ok(processor.BestLaps);
            }
        );

        return app;
    }

    private static WebApplication MapLatestDataEndpoint<TProcessor, T>(this WebApplication app)
        where TProcessor : IProcessor<T>
        where T : ILiveTimingDataPoint, new()
    {
        var dataPoint = new T();
        app.MapGet(
            $"/data/{dataPoint.LiveTimingDataType}/latest",
            (TProcessor processor) =>
            {
                return TypedResults.Ok(processor.Latest);
            }
        );
        return app;
    }
}

using Microsoft.AspNetCore.Mvc;
using UndercutF1.Data;

namespace UndercutF1.Console.Api;

public static class TimingEndpoints
{
    public static WebApplication MapTimingEndpoints(this WebApplication app)
    {
        app.MapLatestDataEndpoint<CarDataProcessor, CarDataPoint>()
            .MapLatestDataEndpoint<
                ChampionshipPredictionProcessor,
                ChampionshipPredictionDataPoint
            >()
            .MapLatestDataEndpoint<DriverListProcessor, DriverListDataPoint>()
            .MapLatestDataEndpoint<ExtrapolatedClockProcessor, ExtrapolatedClockDataPoint>()
            .MapLatestDataEndpoint<HeartbeatProcessor, HeartbeatDataPoint>()
            .MapLatestDataEndpoint<LapCountProcessor, LapCountDataPoint>()
            .MapLatestDataEndpoint<PitLaneTimeCollectionProcessor, PitLaneTimeCollectionDataPoint>()
            .MapLatestDataEndpoint<PitStopSeriesProcessor, PitStopSeriesDataPoint>()
            .MapLatestDataEndpoint<PositionDataProcessor, PositionDataPoint>()
            .MapLatestDataEndpoint<RaceControlMessageProcessor, RaceControlMessageDataPoint>()
            .MapLatestDataEndpoint<SessionInfoProcessor, SessionInfoDataPoint>()
            .MapLatestDataEndpoint<TimingAppDataProcessor, TimingAppDataPoint>()
            .MapLatestDataEndpoint<TimingDataProcessor, TimingDataPoint>()
            .MapLatestDataEndpoint<TimingStatsProcessor, TimingStatsDataPoint>()
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
            )
            .WithTags("Timing Data");

        app.MapGet(
                "/data/TimingData/laps/best",
                (TimingDataProcessor processor) =>
                {
                    return TypedResults.Ok(processor.BestLaps);
                }
            )
            .WithTags("Timing Data");

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
            )
            .WithTags("Timing Data");
        return app;
    }
}

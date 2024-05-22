using OpenF1.Data;

namespace OpenF1.Console;

public static class TimingEndpoints
{
    public static WebApplication MapTimingEndpoints(this WebApplication app)
    {
        app
            .MapLatestDataEndpoint<DriverListProcessor, DriverListDataPoint>()
            .MapLatestDataEndpoint<ExtrapolatedClockProcessor, ExtrapolatedClockDataPoint>()
            .MapLatestDataEndpoint<HeartbeatProcessor, HeartbeatDataPoint>()
            .MapLatestDataEndpoint<LapCountProcessor, LapCountDataPoint>()
            .MapLatestDataEndpoint<RaceControlMessageProcessor, RaceControlMessageDataPoint>()
            .MapLatestDataEndpoint<SessionInfoProcessor, SessionInfoDataPoint>()
            .MapLatestDataEndpoint<TimingAppDataProcessor, TimingAppDataPoint>()
            .MapLatestDataEndpoint<TimingDataProcessor, TimingDataPoint>()
            .MapLatestDataEndpoint<TrackStatusProcessor, TrackStatusDataPoint>()
            .MapLatestDataEndpoint<WeatherProcessor, WeatherDataPoint>();

        return app;
    }

    private static WebApplication MapLatestDataEndpoint<TProcessor, T>(this WebApplication app) 
        where TProcessor : IProcessor<T> 
        where T : ILiveTimingDataPoint, new()
    {
        var dataPoint = new T();
        app.MapGet(
            $"/timingdata/{dataPoint.LiveTimingDataType}/latest", 
            (TProcessor processor, HttpContext context) => 
            {
                return TypedResults.Ok(processor.Latest);
            });
        return app;
    }
}

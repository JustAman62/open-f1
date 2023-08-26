# open-f1

open-f1 is a open source Live Timing client which integrates with the F1 Live Timing data stream. The client allows you to receive live data during F1 sessions, from where you can do your own processing.

open-f1 also allows you to record timing data from sessions using the `OpenF1.Data.Ingest` command line app. This app writes session data to a local sqlite database. The client `OpenF1.Data` can then use this database as a source to replay events from an earlier sessions, instead of the normal F1 Live Timing data source. This replay functionality allows you to develop tools and processing pipelines outside of live F1 sessions.

## Inspiration

This project is heavily inspired by the [FastF1 project by theOehrly](https://github.com/theOehrly/Fast-F1). They did a lot of the work understanding the SignalR stream coming from the F1 Live Timing service. Visit their project if you'd like to do any sort of data analysis on past F1 events, or gather live timing data using their module.

## Live Timing Data Source

F1 live timing is streamed using `SignalR` (the old ASP.NET version, not the newer ASP.NETCore protocol) at <https://livetiming.formula1.com/signalr>. The `OpenF1.Data` simply connects to this endpoint, subscribes to the "Streaming" `Hub`, and listens for messages. It subscribes to the following "topics":

* `Heartbeat`
* `CarData.z`
* `Position.z`
* `ExtrapolatedClock`
* `TopThree`
* `RcmSeries`
* `TimingStats`
* `TimingAppData`
* `WeatherData`
* `TrackStatus`
* `DriverList`
* `RaceControlMessages`
* `SessionInfo`
* `SessionData`
* `LapCount`
* `TimingData`

This list of topics is extracted from the Fast F1 python library.

## Notice

open-f1 is unofficial and are not associated in any way with the Formula 1 companies. F1, FORMULA ONE, FORMULA 1, FIA FORMULA ONE WORLD CHAMPIONSHIP, GRAND PRIX and related marks are trade marks of Formula One Licensing B.V.

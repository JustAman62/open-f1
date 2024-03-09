# open-f1

open-f1 is a open source Live Timing client which integrates with the F1 Live Timing data stream. The client allows you to receive live data during F1 sessions. This data can be streamed in to a file for later processing, or processed live to show in live timing clients.

The `OpenF1.Data` library is provided to facilitate connectivity with the F1 Live Timing data stream, and handle all the processing of the incoming data. It also allows for "simulated" streams, where previously recorded data streams can be played back to allow for easy development/testing.

`OpenF1.Console` is a TUI application which uses `OpenF1.Data` to show a basic Live Timing screen during sessions. 

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

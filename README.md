# open-f1

open-f1 is a open source Live Timing client which integrates with the F1 Live Timing data stream. The client allows you to receive live data during F1 sessions. This data can be streamed in to a file for later processing, or processed live to show in live timing clients.

The `OpenF1.Data` library is provided to facilitate connectivity with the F1 Live Timing data stream, and handle all the processing of the incoming data. It also allows for "simulated" streams, where previously recorded data streams can be played back to allow for easy development/testing.

`OpenF1.Console` is a TUI application which uses `OpenF1.Data` to show a basic Live Timing screen during sessions.

## Inspiration

This project is heavily inspired by the [FastF1 project by theOehrly](https://github.com/theOehrly/Fast-F1). They did a lot of the work understanding the SignalR stream coming from the F1 Live Timing service. Visit their project if you'd like to do any sort of data analysis on past F1 events, or gather live timing data using their module.

## Configuration

OpenF1 can be configured using a simple `config.json` file, or using environment variables. JSON configuration will be loaded from `~/open-f1/config.json`.

| Name           | JSON Path       | Environment Variable   | Description                                                     |
| -------------- | --------------- | ---------------------- | --------------------------------------------------------------- |
| Data Directory | `dataDirectory` | `OPENF1_DATADIRECTORY` | The directory in which JSON timing data is read or written from |

## Live Timing Data Source

F1 live timing is streamed using `SignalR` (the old ASP.NET version, not the newer ASP.NETCore protocol) at <https://livetiming.formula1.com/signalr>. The `OpenF1.Data` simply connects to this endpoint, subscribes to the "Streaming" `Hub`, and listens for messages. It subscribes to the following "topics":

* `Heartbeat`
* `ExtrapolatedClock`
* `TopThree`
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

The following topics are also available but currently not subscribed to:

* `CarData.z`
* `Position.z`

## Data Recording and Replay

All events received by the live timing client will be written to the configured `Data Directory`, see [see Configuration for details](#configuration). Files will be written to a subdirectory named using the current sessions name, e.g. `~/open-f1/data/Jeddah_Race/`. In this directory, two files will be written: 

* `subscribe.txt` contains the data received at subscription time (i.e. when the live timing client connected to the stream)
* `live.txt` contains an append-log of every message received in the stream

Both of these files are required for future simulations/replays. The `IJsonTimingClient` supports loading these files and processing them in the same way live data would be. Data points will be replayed in real time, using a calculated delay.

## Notice

open-f1 is unofficial and are not associated in any way with the Formula 1 companies. F1, FORMULA ONE, FORMULA 1, FIA FORMULA ONE WORLD CHAMPIONSHIP, GRAND PRIX and related marks are trade marks of Formula One Licensing B.V.

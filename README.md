<!-- omit in toc -->
# open-f1

open-f1 is a open source F1 Live Timing client.

`openf1-console` is a TUI application which uses `OpenF1.Data` to show a Live Timing screen during sessions, and records the data for future session replays.

The `OpenF1.Data` library is provided to facilitate connectivity with the F1 Live Timing data stream, and handle all the processing of the incoming data. It also allows for "simulated" streams, where previously recorded data streams can be played back to allow for easy development/testing.

Feature Highlights:

- [Timing Tower](#timing-tower-during-a-race) showing for each driver:
  - Live sector times, with colouring for personal/overall fastest
  - Last & Best Lap
  - Current tyre
  - Age of current tyre
  - Interval to driver in front
  - Gap to leader
  - Gap [between a selected driver](#using-a-cursor-to-display-relative-gap-for-a-specific-driver) and all other drivers (useful for monitoring pit windows)
- [Race Control](#race-control-screen) messages including investigations, penalties, lap deletions, and weather
- Lap-by-lap [Timing History](#using-a-cursor-to-view-timing-history-by-lap) to observe gaps over time

<!-- omit in toc -->
## Table of Contents

- [Inspiration](#inspiration)
- [Getting Started with `openf1-console`](#getting-started-with-openf1-console)
  - [Installation](#installation)
    - [Install and run as a dotnet tool](#install-and-run-as-a-dotnet-tool)
  - [Install and run the standalone executable](#install-and-run-the-standalone-executable)
    - [Run directly from Source](#run-directly-from-source)
  - [Start Timing for a Live Session](#start-timing-for-a-live-session)
  - [Start Timing for a Pre-recorded Session](#start-timing-for-a-pre-recorded-session)
  - [During the Session](#during-the-session)
    - [Managing Delay](#managing-delay)
    - [Using the Cursor](#using-the-cursor)
- [Configuration](#configuration)
- [Live Timing Data Source](#live-timing-data-source)
- [Data Recording and Replay](#data-recording-and-replay)
- [OpenF1 Console in Action](#openf1-console-in-action)
  - [Timing Tower during a Race](#timing-tower-during-a-race)
  - [Using a Cursor to Display Relative Gap for a Specific Driver](#using-a-cursor-to-display-relative-gap-for-a-specific-driver)
  - [Race Control Screen](#race-control-screen)
  - [Using a Cursor to View Timing History by Lap](#using-a-cursor-to-view-timing-history-by-lap)
- [Notice](#notice)


## Inspiration

This project is heavily inspired by the [FastF1 project by theOehrly](https://github.com/theOehrly/Fast-F1). They did a lot of the work understanding the SignalR stream coming from the F1 Live Timing service. Visit their project if you'd like to do any sort of data analysis on past F1 events, or gather live timing data using their module.

## Getting Started with `openf1-console`

### Installation

#### Install and run as a dotnet tool

`openf1-console` is available as a `dotnet` tool from NuGet, which means it can be installed system-wide simply by running:

```sh
# Install globally using the -g flag
dotnet tool install -g openf1-console

# Assuming the dotnet tools directory is on your path, simply execute openf1-console
openf1-console
```

This method is recommended as it is easy to keep the app updated using `dotnet tool update -g openf1-console`.

### Install and run the standalone executable

Standalone executables are attached to each GitHub release. Download the executable for your system OS/architecture and simply run it directly

```sh
# Download the latest executable (in this case for macos-arm64)
curl https://github.com/BrownKnight/open-f1/releases/latest/download/openf1-console-macos-arm64 -o ./openf1-console -L

# On MacOS, you'll likely need to sign the executable as otherwise it'll be blocked. 
# WARNING: If you don't understand why this needs to be done, you probably shouldn't do this and instead use the NuGet based installation method above.
#          Signing the executable yourself removes any integrity checks that signing usually provides.
codesign -s - ./openf1-console

# Execute openf1-console to start the TUI
./openf1-console
```

#### Run directly from Source

```sh
# Checkout the git repository
git clone git@github.com:BrownKnight/open-f1.git

# Run the console project with `dotnet run`
dotnet run --project OpenF1.Console/OpenF1.Console.csproj
```

### Start Timing for a Live Session

1. Start `openf1-console` as described above
2. Navigate to the <kbd>S</kbd> `Session` Screen
3. Start a Live Session with the <kbd>L</kbd> `Start Live Session` action.
4. Press <kbd>Esc</kbd> to return to the main screen
5. Switch to the Timing Tower screen with the <kbd>O</kbd> `Timing Tower` action

During the session, streamed timing data will be written to `~/open-f1/data/<session-name>`. This will allow for [future replays](#start-timing-for-a-pre-recorded-session) of this recorded data.

### Start Timing for a Pre-recorded Session

Data for pre-recorded sessions should be stored in the `~/open-f1/data/<session-name>` directory. Sample data can be found in this repos [Sample Data](/Sample%20Data/) folder. To use this sample data, copy one of the folders to `~/open-f1/data` and then it will be visible in step 4 below.

1. Start `openf1-console` as described above
2. Navigate to the <kbd>S</kbd> `Session` Screen
3. Start a Simulated Session with the <kbd>F</kbd> `Start Simulation` action.
4. Select the session to start using the Up/Down arrows, then pressing <kbd>Enter</kbd> `
5. Press <kbd>Esc</kbd> ` to return to the main screen
6. Switch to the Timing Tower screen with the <kbd>O</kbd> `Timing Tower` action
7. Optionally skip forward in time a bit by decreasing the delay with <kbd>←</kbd> (or <kbd>⇧ Shift</kbd> + <kbd>←</kbd> to decrease by 30 seconds).

### During the Session

#### Managing Delay

All session data, whether live or pre-recorded, is sent to a `Channel` that acts as a delayed-queue. After a short delay, data points are pulled from the queue and processed, leading to updates on the timing screens. The amount of this delay can be changed with the <kbd>►</kbd> `Delay+` and <kbd>◄</kbd> `Delay-` actions whilst on the timing screens. When using `openf1-console` during a live session, you may wish to increase this delay to around ~50 seconds (actual number may vary) to match with the broadcast delay and avoid being spoiled about upcoming action.

Simulated session start with a calculated delay equal to the amount of time between the start of the actual session and now. This means you can decrease the delay with the <kbd>◄</kbd> `Delay-` action to fast-forward through the session.

#### Using the Cursor

There is a global cursor that is controlled with the <kbd>▼</kbd> `Down (1)` and <kbd>▲</kbd> `Up` actions. What this cursor does depends on the screen, for example is can be used in the Timing Tower screen to scroll through Race Control Messages, or to select a driver on the Tower to see comparative intervals.

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

## OpenF1 Console in Action

### Timing Tower during a Race

```
LAP 12/56 Gap     Interval Best Lap Last Lap S1     S2     S3     Pits    Tyre Compare
 1  1 VER  LAP 12   LAP 12 1:40.103 1:41.401 27.121                       M 11
 2 11 PER  +8.755   +8.755 1:40.881 1:41.767 27.442                       M 11
 3  4 NOR +11.491   +2.761 1:41.267 1:41.828 27.266                       M 11
 4 16 LEC +18.104   +6.620 1:41.812 1:42.128 27.066 31.372 43.690         M 11
 5 81 PIA +18.982   +0.951 1:41.607 1:43.456 27.320 31.332 44.804         M 11
 6 55 SAI +22.176   +3.194 1:41.979 1:43.299 27.380 31.597 44.322         M 11
 7  3 RIC +33.950  +12.123 1:42.976 1:43.487 27.366 31.505 44.616         M 11
 8 20 MAG +37.469          1:43.179 1:43.549 27.633 31.297 44.619         H 10
 9 14 ALO +16.891          1:40.988 1:48.048 27.510 31.228 49.310 IN PITS M 11
10 63 RUS +20.875          1:41.696 1:48.016 27.595 31.267 49.154 IN PITS M 11
11 10 GAS +32.431          1:43.012 1:48.722 27.727 31.204 49.791 IN PITS M 10
12  2 SAR +39.150   +1.060 1:43.236 1:43.575 27.649 31.394 44.532         S 10
13 18 STR +45.140   +6.098 1:42.305 1:59.726 26.844 30.546           1    M  1
14 27 HUL +47.386   +2.328 1:41.429 1:41.429 26.861 30.684           1    H  2
15 77 BOT +48.706   +1.338 1:42.548 2:01.016 27.030 30.426           1    H  1
16 31 OCO +50.911   +2.205 1:42.807 2:00.652 26.867 30.508           1    H  1
17 22 TSU +52.707   +1.796 1:42.171 1:42.171 27.019 30.593           1    M  2
18 44 HAM +53.340   +0.959 1:42.906 1:58.909 26.848 30.842           1    M  1
19 23 ALB +53.984   +0.644 1:42.975 2:01.494 27.044 31.735           1    M  1
20 24 ZHO +57.777   +3.793 1:41.765 1:41.765 27.344 30.560           1    H  2

┌─Status──────────┐┌─Race Control Messages────────────────────────────────────────────────────────┐
│ 1 AllClear      ││ 07:19:12  CAR 2 (SAR) TIME 1:44.213 DELETED - TRACK LIMITS AT TURN 6 LAP     │
│ Delayed By      ││           8 15:16:26                                                         │
│ 8.06:30:04      ││ 07:19:07  FIA STEWARDS: TURN 14 INCIDENT INVOLVING CARS 23 (ALB) AND 10      │
└─────────────────┘└──────────────────────────────────────────────────────────────────────────────┘
[▼] Down (0)    [►] Delay+    [◄] Delay-   [R] Race Control     [H] Timing by Lap    [Esc] Return
```

### Using a Cursor to Display Relative Gap for a Specific Driver

Use the cursor controlled by the <kbd>▼</kbd> `Down (1)` and <kbd>▲</kbd> `Up` actions in the <kbd>O</kbd> `Timing Tower` screen to select a specific driver (in this case Norris) to see the relative interval between that driver and all other. This is useful for determining where a driver will fall to after a pit stop, or looking at pit windows during under cuts.

```
LAP  7/56 Gap     Interval Best Lap Last Lap S1     S2     S3     Pits Tyre Compare
 1  1 VER   LAP 7    LAP 7 1:40.103 1:41.147 27.164                    M  6
 2 11 PER  +5.999   +5.999 1:40.881 1:41.656 27.096                    M  6  -2.673
 3 14 ALO  +8.156   +2.157 1:40.988 1:42.363 27.371                    M  6  -0.516
 4  4 NOR  +8.672   +0.516 1:41.267 1:42.009 27.022                    M  6 -------
 5 81 PIA +11.238   +2.704 1:41.607 1:42.608 27.431                    M  6  +2.566
 6 63 RUS +12.118   +0.880 1:41.696 1:42.597 27.340                    M  6  +3.446
 7 16 LEC +12.595   +0.477 1:41.812 1:42.322 27.201                    M  6  +3.923
 8 55 SAI +13.657   +1.062 1:41.979 1:42.409 27.351                    M  6  +4.985
 9 18 STR +16.821   +3.154 1:42.305 1:43.277 27.390                    S  6  +8.149
10 27 HUL +18.437   +1.616 1:42.347 1:43.384 27.553                    M  6  +9.765
11 77 BOT +19.035   +0.598 1:42.548 1:43.151 27.557                    M  6 +10.363
12 31 OCO +19.907   +0.872 1:42.807 1:43.212 27.503                    M  6 +11.235
13 23 ALB +20.999   +1.383 1:42.975 1:43.353 27.681                    M  6 +12.327
14 10 GAS +21.585   +0.586 1:43.012 1:43.494 27.528                    M  6 +12.913
15  3 RIC +22.582   +0.997 1:42.976 1:43.613 27.670                    M  6 +13.910
16 22 TSU +23.594   +1.012 1:43.095 1:43.669 27.667                    S  6 +14.922
17 44 HAM +24.186   +0.592 1:42.906 1:43.467 27.593                    S  6 +15.514
18 20 MAG +25.745   +1.559 1:43.179 1:43.899 27.684                    H  6 +17.073
19 24 ZHO +26.337   +0.592 1:43.484 1:44.212 27.488                    M  6 +17.665
20  2 SAR +27.095   +0.752 1:43.516 1:43.726 27.432 31.588 44.706      S  6 +18.423
┌─Status──────────┐┌─Race Control Messages────────────────────────────────────────────────────────┐
│ 1 AllClear      ││ 07:05:29          DRS ENABLED                                                │
│ Delayed By      ││ 07:03:37          GREEN LIGHT - PIT EXIT OPEN                                │
│ 8.06:33:14      ││ 06:57:10          DRS DISABLED                                               │
└─────────────────┘└──────────────────────────────────────────────────────────────────────────────┘
[▼] Down (4)   [▲] Up  [►] Delay+   [◄] Delay-  [R] Race Control   [H] Timing by Lap   [Esc] Return
```

### Race Control Screen

The <kbd>R</kbd> `Race Control` screen shows all Race Control Messages for the session, along with other session data such as the Weather.

```
┌─Race Control Messages────────────────────────────────────────────────────┐┌─Status──────────────┐
│ 07:12:30  CAR 10 (GAS) TIME 1:42.847 DELETED - TRACK LIMITS AT TURN      ││ LAP  8/56           │
│           14 LAP 4 15:10:25                                              ││ 1 AllClear          │
│ 07:11:59  TURN 14 INCIDENT INVOLVING CARS 23 (ALB) AND 10 (GAS) NOTED    │└─────────────────────┘
│           - FORCING ANOTHER DRIVER OFF THE TRACK                         │┌─Clock───────────────┐
│ 07:08:12  CAR 27 (HUL) TIME 1:43.687 DELETED - TRACK LIMITS AT TURN 6    ││ Simulation Time     │
│           LAP 2 15:05:58                                                 ││ 2024-04-21T07:15:34 │
│ 07:08:10  TURN 6 INCIDENT INVOLVING CARS 18 (STR) AND 27 (HUL) NOTED     ││ Delayed By          │
│           - FORCING ANOTHER DRIVER OFF THE TRACK                         ││ 8.06:33:14          │
│ 07:05:29  DRS ENABLED                                                    │└─────────────────────┘
│ 07:03:37  GREEN LIGHT - PIT EXIT OPEN                                    │┌─Weather─────────────┐
│ 06:57:10  DRS DISABLED                                                   ││ 🌡 Air   19.0C      │
│ 06:45:09  RISK OF RAIN FOR F1 RACE IS 10%                                ││ 🌡 Track 30.7C      │
│ 06:30:01  PIT EXIT CLOSED                                                ││ 💨 1.0kph           │
│ 06:20:01  GREEN LIGHT - PIT EXIT OPEN                                    ││ 🌧  0mm             │
│ 06:11:44  DRS ENABLED IN ZONE 1                                          ││                     │
└──────────────────────────────────────────────────────────────────────────┘└─────────────────────┘
[▼] Down (0)    [►] Delay+    [◄] Delay-   [O] Timing Tower     [H] Timing by Lap    [Esc] Return
```

### Using a Cursor to View Timing History by Lap

In the <kbd>H</kbd> `Timing by Lap` screen, you can use the cursor controlled by the <kbd>▼</kbd> `Down (1)` and <kbd>▲</kbd> `Up` actions to view historical snapshots of the timing tower at the end of every lap.

```
LAP  6/56 Gap     Interval Last Lap S1     S2     S3
 1  1 VER LAP 7   LAP 7    1:41.147 27.114 30.431 43.602
 2 11 PER +5.945  +5.945   1:41.656 27.190 30.742 43.724
 3 14 ALO +7.527  +1.582   1:42.363 27.069 31.125 44.169
 4  4 NOR +8.187  +0.660   1:42.009 27.086 31.028 43.895
 5 81 PIA +10.800 +2.613   1:42.608 27.420 31.171 44.017
 6 63 RUS +11.644 +0.844   1:42.597 27.312 31.169 44.116
 7 16 LEC +12.263 +0.619   1:42.322 27.267 31.156 43.899
 8 55 SAI +13.241 +0.978   1:42.409 27.317 31.132 43.960
 9 18 STR +16.414 +3.173   1:43.277 27.427 31.431 44.419
10 27 HUL +17.850 +1.436   1:43.384 27.468 31.525 44.391
11 77 BOT +18.349 +0.499   1:43.151 27.357 31.453 44.341
12 31 OCO +19.281 +0.932   1:43.212 27.330 31.387 44.495
13 23 ALB +20.491 +1.210   1:43.353 27.518 31.276 44.559
14 10 GAS +21.227 +0.736   1:43.494 27.579 31.338 44.577
15  3 RIC +22.072 +0.845   1:43.613 27.519 31.527 44.567
16 22 TSU +23.092 +1.020   1:43.669 27.431 31.581 44.657
17 44 HAM +23.758 +0.666   1:43.467 27.510 31.423 44.534
18 20 MAG +25.220 +1.462   1:43.899 27.510 31.597 44.792
19 24 ZHO +26.010 +0.790   1:44.212 27.719 31.729 44.764
20  2 SAR +26.566 +0.556   1:43.726 27.432 31.588 44.706
[▼] Down (6) [▲] Up [O] Timing Tower [R] Race Control [Esc] Retur
```

## Notice

open-f1 is unofficial and are not associated in any way with the Formula 1 companies. F1, FORMULA ONE, FORMULA 1, FIA FORMULA ONE WORLD CHAMPIONSHIP, GRAND PRIX and related marks are trade marks of Formula One Licensing B.V.

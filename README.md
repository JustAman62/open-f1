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
  - [Timing Tower during Practice/Qualifying](#timing-tower-during-practicequalifying)
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
* `CarData.z`
* `Position.z`
* `ChampionshipPrediction`
* `TeamRadio`

## Data Recording and Replay

All events received by the live timing client will be written to the configured `Data Directory`, see [see Configuration for details](#configuration). Files will be written to a subdirectory named using the current sessions name, e.g. `~/open-f1/data/Jeddah_Race/`. In this directory, two files will be written: 

* `subscribe.txt` contains the data received at subscription time (i.e. when the live timing client connected to the stream)
* `live.txt` contains an append-log of every message received in the stream

Both of these files are required for future simulations/replays. The `IJsonTimingClient` supports loading these files and processing them in the same way live data would be. Data points will be replayed in real time, using a calculated delay.

## OpenF1 Console in Action

### Timing Tower during a Race

Monitor sector times and gaps, see recent race control messages, capture position changes, observe pit strategies, and more with the standard Timing Tower view.

```
 LAP 23/63   Gap       Interval   Best Lap   Last Lap   S1      S2      S3      Pit  Tyre Compare Driver
───────────────────────────────────────────────────────────────────────────────────────────────────────────
  1  1 VER    LAP 23     LAP 23   1:20.543   1:21.763   25.778  28.626               M 22          1 VER
  2 16 LEC    +8.426     +8.426   1:20.926   1:22.018   26.057  28.558               M 22         16 LEC ▲
  3 55 SAI   +12.709     +4.335   1:20.966   1:21.926   25.812                       M 22         55 SAI ▲
  4 81 PIA   +13.404     +0.695   1:20.808   1:21.869   25.643                       M 22         81 PIA ▲
  5 44 HAM   +18.143     +4.714   1:21.222   1:21.846   25.730                       M 22         44 HAM ▲
  6 11 PER   +33.463    +15.320   1:21.682   1:21.871   26.001                       H 22         11 PER ▲
  7  4 NOR   +34.275     +0.641   1:20.673   1:26.107   49.204                   1   H  1          4 NOR ▼
  8 18 STR   +38.043     +3.768   1:22.233   1:22.697   26.150                        23          18 STR ▲
  9 63 RUS   +44.151     +6.279   1:21.135   1:44.938   48.506  28.710  27.722   1   H  1         63 RUS ▼
 10 31 OCO   +44.838     +0.702   1:22.229   1:23.376   26.298  29.076  28.002        22          31 OCO
 11 20 MAG   +46.649     +1.811   1:22.237   1:23.013   26.263  28.965  27.785       M 22         20 MAG
 12 24 ZHO   +50.385     +3.915   1:22.565   1:23.261   26.299  29.001  27.961        22          24 ZHO
 13  2 SAR   +53.538     +3.153   1:22.702   1:23.287   26.402  29.057  27.828       H 22          2 SAR
 14 22 TSU   +53.907     +0.369   1:20.936   1:23.322   26.253  29.201  27.868   1   H 10         22 TSU
 15 27 HUL   +55.158     +1.328   1:21.700   1:22.412   26.061  28.612  27.739   1   H  9         27 HUL
 16  3 RIC   +57.675     +2.454   1:21.569   1:22.400   25.974  28.779  27.647   1   H 11          3 RIC
 17 10 GAS   +60.375     +2.671   1:21.371   1:22.296   26.034  28.692  27.570   1   H 14         10 GAS
 18 77 BOT   +62.533     +2.158   1:21.455   1:22.436   26.097  28.724  27.615   1   H 13         77 BOT
 19 14 ALO   +68.766     +6.204   1:21.492   1:24.339   26.141  29.043           1   H 15         14 ALO
 20 23 ALB       1 L    +80.160   1:21.491   1:21.725   25.866  28.768           2   M 11         23 ALB
╭─Status──────╮╭─Race Control Messages────────────────────────────────────────────────────────────────────╮
│ 1 AllClear  ││ ► FIA STEWARDS: 10 SECOND STOP/GO PENALTY FOR CAR 23 (ALB) - UNSAFE CONDITION            │
│ 13:34:08    ││ ► CAR 1 (VER) TIME 1:21.550 DELETED - TRACK LIMITS AT TURN 6 LAP 20 15:29:22             │
│ 01:29:07    ││ ► WAVED BLUE FLAG FOR CAR 23 (ALB) TIMED AT 15:31:32                                     │
╰─────────────╯╰──────────────────────────────────────────────────────────────────────────────────────────╯
[▼] Down (0)     [►] Delay+     [◄] Delay-     [R] Race Control      [H] Timing by Lap      [Esc] Return
```

### Using a Cursor to Display Relative Gap for a Specific Driver

Use the cursor controlled by the <kbd>▼</kbd> `Down (1)` and <kbd>▲</kbd> `Up` actions in the <kbd>O</kbd> `Timing Tower` screen to select a specific driver (in this case Norris) to see the relative interval between that driver and all other. This is useful for determining where a driver will fall to after a pit stop, or looking at pit windows during under cuts.

Additionally, the gap between the selected drivers and those around them over the last four laps will be displayed at the bottom of the screen. This allows you to easily see evolving gaps over time and evaluate how soon a driver may catch up or pull away.

```
 LAP 52/63   Gap       Interval   Best Lap   Last Lap   S1      S2      S3      Pit  Tyre Compare Driver
───────────────────────────────────────────────────────────────────────────────────────────────────────────
  1  1 VER    LAP 52     LAP 52   1:20.366   1:20.736   25.541  28.085  27.110   1   H 27  -4.507  1 VER
  2  4 NOR    +4.507     +4.507   1:20.039   1:20.367   25.252  28.191  26.924   1   H 30 -------  4 NOR
  3 16 LEC    +7.664     +3.121   1:19.935   1:20.364   25.492  27.991  26.881   1   H 26  +3.157 16 LEC
  4 81 PIA   +14.889     +7.292   1:19.907   1:21.203   25.443  28.610  27.150   1   H 29 +10.382 81 PIA
  5 55 SAI   +19.124     +4.255   1:20.466   1:21.748   25.751  28.736  27.261   1   H 24 +14.617 55 SAI
  6 63 RUS   +26.919     +7.749   1:20.669   1:21.491   25.445  28.401           1   H 29 +22.412 63 RUS
  7 44 HAM   +31.073     +4.154   1:20.331   1:20.698   25.481  28.319           1   H 23 +26.566 44 HAM
  8 11 PER   +57.304    +26.303   1:19.919   1:19.985   25.631                   1   M 13 +52.797 11 PER
  9 18 STR   +72.614    +15.489   1:20.588   1:20.731   25.652                   1   H 14 +68.107 18 STR ▲
 10 22 TSU   +75.126     +2.512   1:20.936   1:23.356   26.007                   1   H 38 +70.619 22 TSU ▼
 11 27 HUL   +79.504     +4.470   1:21.700   1:21.921   25.974  28.539  27.408   1   H 37 +74.997 27 HUL
 12  3 RIC       1 L     +3.599   1:21.569   1:24.081   27.498  28.984  27.599   1   H 39          3 RIC
 13 77 BOT       1 L    +13.893   1:21.455   1:23.889   26.193  29.398  28.298   1   H 42         77 BOT
 14 20 MAG       1 L     +0.267   1:21.068   1:21.496   25.600  28.548  27.348   1   H 13         20 MAG ▲
 15 31 OCO       1 L     +1.433   1:21.304   1:23.073   26.423  28.977  27.673   1   H 25         31 OCO ▼
 16 24 ZHO       1 L     +0.506   1:21.016   1:22.488   25.868  29.088  27.532   1   M 17         24 ZHO
 17 10 GAS       1 L     +5.995   1:21.371   1:21.981   26.011  28.417           2   M 19         10 GAS
 18  2 SAR       1 L     +3.835   1:21.335   1:23.806   25.900  28.836           1   M 18          2 SAR
╭─Status──────╮                     VER vs NOR                                    NOR vs LEC
│ 1 AllClear  │LAP 51  █████████████████████ 4.539           LAP 51  █████████████████████████████ 3.012
│ 14:12:54    │LAP 50  ████████████████████████ 4.908        LAP 50  ██████████████████████████████ 3.015
│ 00:50:21    │LAP 49  ████████████████████████████ 5.686    LAP 49  ██████████████████████████ 2.64
╰─────────────╯LAP 48  ███████████████████████████████ 6.024 LAP 48  █████████████████████ 2.327
[▼] Down (2)    [▲] Up   [►] Delay+   [◄] Delay-   [R] Race Control     [H] Timing by Lap    [Esc] Return
```

### Timing Tower during Practice/Qualifying

Monitor live/best sector times, gaps, tyres, and lap deletions easily with the specialized timing tower for non-race sessions.

```
 Qualifyin  Gap      Best Lap  BS1            BS2            BS3             S1     S2     S3     Pit Tyre
───────────────────────────────────────────────────────────────────────────────────────────────────────────
  1 81 PIA    0.000  1:15.940  23.824(+0.008) 26.360(+0.023) 25.756(+0.092)  34.118                   S  2
  2  1 VER   +0.073  1:16.013  23.949(+0.133) 26.337(+0.000) 25.727(+0.063)  36.105 37.328 36.319 IN  S  2
  3 63 RUS   +0.167  1:16.107  23.816(+0.000) 26.401(+0.064) 25.890(+0.226)  23.816 26.401 25.890  1  S  1
  4  4 NOR   +0.254  1:16.194  24.042(+0.226) 26.394(+0.057) 25.758(+0.094)  58.904 33.455         1  S  3
  5 11 PER   +0.464  1:16.404  24.150(+0.334) 26.452(+0.115) 25.802(+0.138)  36.740 36.187 42.823 IN  S  3
  6 18 STR   +0.518  1:16.458  24.104(+0.288) 26.366(+0.029) 25.988(+0.324)  24.104 26.366 25.988  1  S  2
  7 16 LEC   +0.523  1:16.463  24.096(+0.280) 26.608(+0.271) 25.759(+0.095)  50.807 36.915 39.436     M  4
  8 31 OCO   +0.705  1:16.645  24.239(+0.423) 26.601(+0.264) 25.805(+0.141)  24.161                1  S  1
  9 44 HAM   +0.779  1:16.719  24.101(+0.285) 26.900(+0.563) 25.718(+0.054)         35.185 31.978  1  S  4
 10 20 MAG   +0.914  1:16.854  24.177(+0.361) 26.651(+0.314) 26.026(+0.362)  24.177 26.651 26.026  1  S  5
 11 14 ALO   +0.977  1:16.917  24.191(+0.375) 26.563(+0.226) 26.163(+0.499)  26.346 34.989 34.513 IN  S  3
 12 55 SAI   +1.035  1:16.975  24.699(+0.883) 26.612(+0.275) 25.664(+0.000)  24.014                   M  4
 13 27 HUL   +1.052  1:16.992  24.230(+0.414) 26.784(+0.447) 25.978(+0.314)  28.867                1  S  5
 14 10 GAS   +1.344  1:17.284  24.402(+0.586) 26.772(+0.435) 26.110(+0.446)         35.776 37.456  1  S  1
 15 24 ZHO   +1.375  1:17.315  24.337(+0.521) 26.877(+0.540) 26.101(+0.437)  26.776                2  S  5
 16 23 ALB                                                                   32.717 30.457 36.415 IN  S  3
 17  3 RIC                                                                   24.359 26.653            S  1
 18 22 TSU                                                                   24.026 26.444            S  1
 19 77 BOT                                                                   24.504 26.471            S  1
 20  2 SAR                                                                          33.158 39.990  1  S  1
╭─Status──────╮╭─Race Control Messages────────────────────────────────────────────────────────────────────╮
│ 1 AllClear  ││ ► CAR 23 (ALB) TIME 1:17.414 DELETED - TRACK LIMITS AT TURN 18 LAP 3 16:05:53            │
│ 14:09:45    ││ ► CAR 2 (SAR) TIME 1:17.188 DELETED - TRACK LIMITS AT TURN 18 LAP 3 16:03:19             │
│ 00:08:14    ││                                                                                          │
╰─────────────╯╰──────────────────────────────────────────────────────────────────────────────────────────╯
[▼] Down (0)     [►] Delay+     [◄] Delay-     [R] Race Control      [H] Timing by Lap      [Esc] Return
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

In the <kbd>H</kbd> `Timing by Lap` screen, you can use the cursor controlled by the <kbd>▼</kbd> `Down (1)` and <kbd>▲</kbd> `Up` actions to view historical snapshots of the timing tower at the end of every lap. This view will show position changes during that lap, and relative changes in Gap and Interval. Scrolling through laps allows you to build a picture of how the race is unfolding.

```
 LAP 52/63 Gap              Interval         Last Lap S1     S2     S3
─────────────────────────────────────────────────────────────────────────────
  1  1 VER LAP 53 (+0)      LAP 53 (+0)      1:20.556 25.536 28.206 26.814
  2  4 NOR +4.565 (+0.026)  +4.565 (+0.026)  1:20.582 25.297 28.229 27.056
  3 16 LEC +7.353 (-0.198)  +2.788 (-0.224)  1:20.358 25.427 28.048 26.883
  4 81 PIA +15.090 (+0.221) +7.737 (+0.419)  1:20.777 25.364 28.354 27.059
  5 55 SAI +20.169 (+1.045) +5.079 (+0.824)  1:21.601 25.625 28.669 27.307
  6 44 HAM +31.523 (+0.620) +11.354 (+7.253) 1:21.176 25.580 28.336 27.260 ▲
  7 63 RUS +27.974 (+1.172)                  1:26.242 25.615 28.670 31.957 ▼
  8 11 PER +56.452 (-0.870) +23.964 (-2.455) 1:19.686 25.282 27.676 26.728
  9 18 STR +72.962 (+0.126) +16.510 (+0.996) 1:20.682 25.568 28.136 26.978
 10 22 TSU +77.666 (+1.558) +4.704 (+1.432)  1:22.114 26.023 28.590 27.501
 11 27 HUL 1 L              +8.239 (+1.903)  1:24.017 25.952 28.767 29.298
 12  3 RIC 1 L              +3.450 (+1.592)  1:25.609 27.362 28.938 29.309
 13 20 MAG 1 L              +9.365 (-3.999)  1:21.610 25.964 28.403 27.243
```

## Notice

open-f1 is unofficial and are not associated in any way with the Formula 1 companies. F1, FORMULA ONE, FORMULA 1, FIA FORMULA ONE WORLD CHAMPIONSHIP, GRAND PRIX and related marks are trade marks of Formula One Licensing B.V.

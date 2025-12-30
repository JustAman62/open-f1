<!-- omit in toc -->
# undercut-f1

undercut-f1 is an open source F1 Live Timing client.

`undercutf1` is a TUI application which uses `UndercutF1.Data` to show a Live Timing screen during sessions, and records the data for future session replays.
F1 live broadcasts are usually delayed by some indeterminable amount (usually 30-60 seconds), so the TUI allows you to delay the data being displayed so that you can match up what you see on your screen to what you see on your TV.

The `UndercutF1.Data` library is provided to facilitate connectivity with the F1 Live Timing data stream, and handle all the processing of the incoming data. It also allows for "simulated" streams, where previously recorded data streams can be played back to allow for easy development/testing.

Feature Highlights:

![Timing Tower during a Race](docs/screenshots/race-timing-screen.png)

- [Timing Tower](#timing-tower-during-a-race) showing for each driver:
  - Live sector times, with colouring for personal/overall fastest
  - Last & Best Lap
  - Current tyre
  - Age of current tyre
  - Interval to driver in front
  - Gap to leader
  - Gap [between a selected driver](#using-a-cursor-to-display-relative-gap-for-a-specific-driver) and all other drivers (useful for monitoring pit windows)
- [Pit Stop Strategy](#tyre-stint--strategy) gives you at-a-glance information about all the drivers strategies
- [Race Control](#race-control-page) messages including investigations, penalties, lap deletions, and weather
- [Driver Tracker](#driver-tracker) shows the position of selected drivers on a live track map
- Lap-by-lap [Timing History](#using-a-cursor-to-view-timing-history-by-lap) to observe gaps over time
- [Pause the session clock](#external-session-clock-sync) when you pause external video players (such as Kodi), so that your timing screen remains in sync with your feed.

> [!WARNING]
> Since the 2025 Dutch GP, the F1 Live Timing feed no longer freely publishes some of the data we need to provide all existing functionality.
>
> The following functionality is now only available to those who have an active F1 TV subscription:
>
> - Driver Tracker (live position of each car is unavailable)
> - DRS Indicator in Timing Screens
> - Team/Drivers championship tables
> - Pit Stop times (in box and in pitlane)
>
> Importing a session after it finishes still fetches all the data, so all functionality is available if you use undercut-f1 after a race finishes.
>
> Linking your F1 TV account with undercut-f1 is as simple as running `undercutf1 login` before starting your session, and following the instructions displayed. See [F1 TV Account Login](#f1-tv-account-login) for details.

<!-- omit in toc -->
## Table of Contents

- [Inspiration](#inspiration)
- [UndercutF1 in Action](#undercutf1-in-action)
  - [Timing Tower during a Race](#timing-tower-during-a-race)
  - [Using a Cursor to Display Relative Gap for a Specific Driver](#using-a-cursor-to-display-relative-gap-for-a-specific-driver)
  - [Track Gaps between Two Drivers Over Time](#track-gaps-between-two-drivers-over-time)
  - [Timing Tower during Practice/Qualifying](#timing-tower-during-practicequalifying)
  - [Race Control Page](#race-control-page)
  - [Driver Tracker](#driver-tracker)
  - [Tyre Stint / Strategy](#tyre-stint--strategy)
  - [Using a Cursor to View Timing History by Lap](#using-a-cursor-to-view-timing-history-by-lap)
  - [Listen to and Transcribe Team Radio](#listen-to-and-transcribe-team-radio)
- [Getting Started with `undercutf1`](#getting-started-with-undercutf1)
  - [Installation](#installation)
    - [Prerequisites](#prerequisites)
    - [Install and run as a dotnet tool](#install-and-run-as-a-dotnet-tool)
    - [Install from Homebrew](#install-from-homebrew)
    - [Install and run the standalone executable](#install-and-run-the-standalone-executable)
    - [Run using the docker image](#run-using-the-docker-image)
      - [Known Issues with Docker](#known-issues-with-docker)
    - [Run directly from Source](#run-directly-from-source)
  - [Start Timing for a Live Session](#start-timing-for-a-live-session)
  - [Start Timing for a Pre-recorded Session](#start-timing-for-a-pre-recorded-session)
  - [Download a previous session data for replay](#download-a-previous-session-data-for-replay)
  - [During the Session](#during-the-session)
    - [Managing Delay](#managing-delay)
    - [Using the Cursor](#using-the-cursor)
- [Configuration](#configuration)
  - [Default Directories](#default-directories)
  - [F1 TV Account Login](#f1-tv-account-login)
- [Logging](#logging)
- [Alternate Key Binds](#alternate-key-binds)
- [Data Recording and Replay](#data-recording-and-replay)
- [API](#api)
  - [Control API](#control-api)
  - [Data API](#data-api)
- [External Session Clock Sync](#external-session-clock-sync)
- [Notice](#notice)

## Inspiration

This project is heavily inspired by the [FastF1 project by theOehrly](https://github.com/theOehrly/Fast-F1). They did a lot of the work understanding the SignalR stream coming from the F1 Live Timing service. Visit their project if you'd like to do any sort of data analysis on past F1 events, or gather live timing data using their module.

## UndercutF1 in Action

### Timing Tower during a Race

Monitor sector times and gaps, see recent race control messages, capture position changes, observe pit strategies, and more with the standard Timing Tower view.

![Timing Tower during a Race](docs/screenshots/race-timing-screen.png)

### Using a Cursor to Display Relative Gap for a Specific Driver

Use the cursor controlled by the <kbd>▼</kbd>/<kbd>▲</kbd> `Cursor` actions in the <kbd>O</kbd> `Timing Tower` screen to select a specific driver (in this case Norris) to see the relative interval between that driver and all other. This is useful for determining where a driver will fall to after a pit stop, or looking at pit windows during under cuts.

Additionally, the gap between the selected drivers and those around them over the last four laps will be displayed at the bottom of the screen. This allows you to easily see evolving gaps over time and evaluate how soon a driver may catch up or pull away.

![Relative gaps for a specific driver](docs/screenshots/relative-gap-race.png)

### Track Gaps between Two Drivers Over Time

Similar to the above comparison feature, you can also select two drivers to enter a persistent comparison mode, by using the <kbd>C</kbd> `Select for Compare` action when your cursor is over a particular driver. Selecting two drivers in this way will replace the normal comparison panel with a persistent comparison between the two selected drivers.

This allows you to easily monitor the gaps between two drivers who are not next to eachother on track, for example if they are on contra tyre strategies or to monitor a driver pit window against another driver.

Once you select the two driver to compare, move your cursor back to the top (position 0) to have the Compare column show relative gaps to the first selected driver.

![Two driver selected for compare](docs/screenshots/compare-drivers.png)

### Timing Tower during Practice/Qualifying

Monitor live/best sector times, gaps, tyres, and lap deletions easily with the specialized timing tower for non-race sessions.

![Timing Tower during Practice/Qualifying](docs/screenshots/quali-timing-screen.png)

### Race Control Page

The `Race Control` page shows all Race Control Messages for the session, along with other session data such as the Weather.

![Race Control Page](docs/screenshots/race-control-screen.png)

### Driver Tracker

The `Driver Tracker` page shows a track map overlaid with selected drivers. Use the <kbd>▼</kbd>/<kbd>▲</kbd> `Cursor` actions to choose drivers, then use the <kbd>⏎</kbd> `Toggle Select` action to toggle the inclusion of the driver on the track map. The driver under the current cursor position will also be highlighted on the map, and timing gaps will switch to interval between that driver and all other drivers.

![Driver Tracker Page](docs/screenshots/driver-tracker.png)

> [!NOTE]
> The Driver Tracker and Timing History charts work using terminal graphics protocols. UndercutF1 supports three protocols:
>
> - [Kitty Graphics Protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/)
> - [iTerm2 Inline Image Protocol](https://iterm2.com/documentation-images.html)
> - [Sixel](https://www.vt100.net/docs/vt3xx-gp/chapter14.html)
>
> Between these three protocols, UndercutF1 should support a wide variety of terminals.
> I personally try to test on iTerm2, Ghostty, WezTerm, and Kitty.
> Windows Terminal should be supported via Sixel, but is rarely tested.
>
> Run `undercutf1 info` to see if UndercutF1 detects graphics protocol support in your terminal.
>
> If the terminal you use doesn't show the graphics, please raise an issue and I'll try to implement support!

### Tyre Stint / Strategy

The `Tyre Stint` page shows the tyre strategy for all the drivers. At a glance, see what tyres the drivers have used, how old they are, and if they are on an offset strategy to any other drivers. Each stint contains the pit stop time and pit lane time for the pit stop at the start of the stint.

Use the <kbd>▼</kbd>/<kbd>▲</kbd> `Cursor` actions to view more information for a particular drivers strategy.

![Tyre Stint](docs/screenshots//tyre-stint-screen.png)

### Using a Cursor to View Timing History by Lap

In the `Timing by Lap` page, you can use the cursor controlled by the <kbd>▼</kbd>/<kbd>▲</kbd> `Cursor` actions to view historical snapshots of the timing tower at the end of every lap. This view will show position changes during that lap, and relative changes in Gap and Interval. Scrolling through laps allows you to build a picture of how the race is unfolding.

Charts on the right display how Gap to Leader and Lap Time for all selected drivers over the last 15 laps, letting you see trends and catch strategies unfolding.

You can show/hide drivers here by using the <kbd>D</kbd> `Select Drivers` action and then hitting enter on the drivers you have to show/hide. The drivers hidden here will also be hidden on the Driver Tracker page.

![Using a Cursor to View Timing History by Lap](docs/screenshots/timing-history-screen.png)

### Listen to and Transcribe Team Radio

Listen to team radio clips from anytime in the session, and use a local ML model (Whisper) to transcribe the audio on demand. Transcription accuracy is fairly low, depending on the that days audio quality and driver. Suggestions welcome for improving this!

See [Prerequisites](#prerequisites) to ensure you are able to playback audio.

![Listen to and Transcribe Team Radio](docs/screenshots/team-radio.png)

## Getting Started with `undercutf1`

### Installation

#### Prerequisites

UndercutF1 tries to statically link as many dependencies as possible to make installation and usage easy.
There are however some utilities that may need to be installed for some functionality:

- Team Radio audio playback uses platform-specific command-line executables to play audio files.
  - On Linux, you need `mpg123` available on the `PATH`. For apt-based systems, you can install with `apt install mpg123`
  - On Mac, you need `afplay` available on the `PATH`. This should be installed by default.
  - On Windows, we only support audio playback via FFmpeg (`ffplay`) - see below for installation instructions.
  - On Linux/Mac, you can use the [`preferFfmpegPlayback` configuration](#configuration) to use `ffplay` instead of `mpg123`/`afplay`
- Team Radio transcription relies on FFmpeg and Whisper. Whisper models are downloaded on demand (after user confirmation) in the app. See the [FFmpeg download page](See <https://www.ffmpeg.org/download.html>) for details on how to install.
  - On Linux apt-based systems, you can install with `apt install ffmpeg`
  - On Mac with brew, you can install with `brew install ffmpeg`
  - On Windows with WinGet, you can install with `winget install ffmpeg`
- Terminal graphics rely on [SkiaSharp](https://github.com/mono/SkiaSharp). I've statically linked all the skia libs, so you shouldn't need to download skia. However, skia does rely on `libfontconfig` which may not be installed on your system by default.
  - On Linux apt-based systems, you can install with `apt install libfontconfig`
  - On Mac with brew, you can install with `brew install fontconfig`
- Guided F1 TV sign in (see [F1 TV Account Login](#f1-tv-account-login)) uses the <https://github.com/webview/webview> library to launch a WebView and guide you through sign in. This isn't required functionality, F1 TV sign-in can be done manually or completely ignored.
  - On Windows/Mac, this should work with the natively installed WebView2 and WebKit respectively.
  - On Linux, this uses WebKitGTK which may or may not already be installed. See [webview's documentation for more](https://github.com/webview/webview#linux-and-bsd).

#### Install and run as a dotnet tool

`undercutf1` is available as a `dotnet` tool from NuGet, which means it can be installed system-wide simply by running:

```sh
# Install globally using the -g flag
dotnet tool install -g undercutf1

# Assuming the dotnet tools directory is on your path, simply execute undercutf1
undercutf1
```

This method is recommended as it is easy to keep the app updated using `dotnet tool update -g undercutf1`. You'll need the .NET 9 SDK installed to use this installation method. If you'd rather not install the SDK, try the [standalone installation option below](#install-and-run-the-standalone-executable).

#### Install from Homebrew

`undercutf1` is available as a formula from `brew` which means it can be installed system-wide (on Mac and Linux) simply by running:

```sh
brew install undercutf1

# Execute undercutf1 to start the TUI
undercutf1
```

This method is recommended as it is easy to keep the app updated using `brew upgrade`. Note that installing using `brew` will also install the `dotnet` formula. If you develop on your machine using the dotnet-sdk, and have the sdk installed through a non-brew method (e.g. directly from MS or via VSCode), I would recommend avoiding this installation method as the brew `dotnet` installation can conflict with your own installation due to differences in how `dotnet` is signed via brew. Alternatively, you can install the `dotnet` cask (which is signed correctly) `brew install --cash dotnet-sdk`, and then install `undercutf1`. This way, your system will still have a signed version of the SDK on its `$PATH`.

The brew installation method will also install all the mentioned [prerequisites](#prerequisites) for you.

#### Install and run the standalone executable

Standalone executables are attached to each GitHub release. Download the executable for your system OS/architecture and simply run it directly. The list of artifacts are available on the [release page for the latest release](https://github.com/JustAman62/undercut-f1/releases/latest). These executables are static builds so don't require the `dotnet` runtime to be present.

```sh
# Download the latest executable (in this case for osx-arm64)
curl https://github.com/JustAman62/undercut-f1/releases/latest/download/undercutf1-osx-arm64 -o ./undercutf1 -L

# Execute undercutf1 to start the TUI
./undercutf1
```

#### Run using the docker image

Docker images are pushed to Dockerhub containing the executable.
The image expects a volume to be mounted at `/data` to store/read session recordings.
If this is not provided, the application will only work for live sessions and you'll lose recorded data.

If provided, the directory you are mapping must already exist, as the docker image will not have the required permissions to create it for you.

If you are using Wezterm or iTerm as your terminal, you'll need to pass through the TERM_PROGRAM environment variable
to make sure that your terminal graphics work correctly (e.g. driver tracker).

```sh
docker run -it -e TERM_PROGRAM -v $HOME/undercut-f1/data:/data justaman62/undercutf1

# Arguments can still be passed to the executable as normal
# for example:
docker run -it -v $HOME/undercut-f1/data:/data justaman62/undercutf1 import 2025
```

##### Known Issues with Docker

- Audio playback of Team Radio may not work when using Docker. This is due to difficulties in using audio devices in a cross-platform way, which I haven't managed to figure out yet.

#### Run directly from Source

```sh
# Checkout the git repository
git clone git@github.com:JustAman62/undercut-f1.git
cd undercut-f1

# Run the console project with `dotnet run`
dotnet run --project UndercutF1.Console/UndercutF1.Console.csproj

# Arguments can be provided after the -- argument, for example
dotnet run --project UndercutF1.Console/UndercutF1.Console.csproj -- import 2025
```

### Start Timing for a Live Session

1. Start `undercutf1` as described above
2. Navigate to the <kbd>S</kbd> `Session` Screen
3. Start a Live Session with the <kbd>L</kbd> `Start Live Session` action

During the session, streamed timing data will be written to [the configured data directory](#default-directories). This will allow for [future replays](#start-timing-for-a-pre-recorded-session) of this recorded data.

### Start Timing for a Pre-recorded Session

Data for pre-recorded sessions should be stored in the `<data-directory>/<session-name>` directory. Sample data can be found in this repos [Sample Data](/Sample%20Data/) folder. To use this sample data, copy one of the folders to [the configured data directory](#default-directories) and then it will be visible in step 4 below.

1. OPTIONAL: Download sample data to [the configured data directory](#default-directories). If you already have data, or have checked out the repository, skip to the next step.

    ```sh
    # Import data from the 2025 race in Suzuka
    undercutf1 import 2025 --meeting-key 1256 --session-key 10006
    ```

2. Start `undercutf1` as described [above](#installation)
3. Navigate to the <kbd>S</kbd> `Session` Screen
4. Start a Simulated Session with the <kbd>F</kbd> `Start Simulation` action.
5. Select the session to start using the Up/Down arrows, then pressing <kbd>Enter</kbd>
6. Skip forward in time a bit by decreasing the delay with <kbd>N</kbd> (or <kbd>⇧ Shift</kbd> + <kbd>N</kbd> to decrease by 30 seconds).

### Download a previous session data for replay

F1 provides static timing data files for already completed sessions. This data can be downloaded and converted into the same format `undercutf1` uses to save live recorded data. You can then replay the old session using the steps above.

1. List the meetings that have data available to import with `undercutf1 import <year>`
2. Review the list of meetings returned from the command, and list the available sessions inside the chosen meeting with `undercutf1 import <year> --meeting-key <meeting-key>`
3. Review the list of sessions, and select one to import: `undercutf1 import <year> --meeting-key <meeting-key> --session-key <session-key>`
4. Data that is imported will be saved to the configured data directory. See [Configuration](#configuration) for information on how to change this.

### During the Session

#### Managing Delay

All session data, whether live or pre-recorded, is sent to a `Channel` that acts as a delayed-queue. After your currently configured delay, data points are pulled from the queue and processed, leading to updates on the timing screens. The amount of this delay can be changed with the <kbd>M</kbd>/<kbd>N</kbd> `Delay` actions whilst on the timing screens. Hold <kbd>⇧ Shift</kbd> to change the delay by 30 seconds instead of 5. Use the <kbd>,</kbd>/<kbd>.</kbd> keys to change by 1 second. When using `undercutf1` during a live session, you may wish to increase this delay to around ~50 seconds (actual number may vary) to match with the broadcast delay and avoid being spoiled about upcoming action.

Simulated sessions start with a calculated delay equal to the amount of time between the start of the actual session and now. This means you can decrease the delay with the <kbd>N</kbd> `Delay` action to fast-forward through the session.

Data processing, and therefore the "session clock" can be paused using the <kbd>P</kbd> `Pause Clock` action. This allows you to easily synchronize pre-recorded sessions by pausing the session in UndercutF1, then resuming at the perfect time when, for example, the formation lap starts.

#### Using the Cursor

There is a global cursor that is controlled with the <kbd>▼</kbd>/<kbd>▲</kbd> `Cursor` actions. What this cursor does depends on the screen, for example is can be used in the Timing Tower screen to scroll through Race Control Messages, or to select a driver on the Tower to see comparative intervals. Hold the <kbd>⇧ Shift</kbd> key to move the cursor by five positions instead of one.

## Configuration

UndercutF1 can be configured using either a simple `config.json` file, through the command line at startup, or using environment variables. JSON configuration will be loaded from [the appropriate config file path](#default-directories), if it exists.

A JSON Schema file is available in the root of this repository which can help when creating the `config.json` file. Add the JSON `{"$schema": "https://raw.githubusercontent.com/JustAman62/undercut-f1/refs/heads/master/config.schema.json"}"` to your `config.json` to get automatic code complete and validation.

To view what configuration is currently being used, open the <kbd>I</kbd> `Info` screen when the app starts up.

| JSON Path                                     | Command Line                | Environment Variable                                      | Description                                                                                                                                                                                                                             |
| --------------------------------------------- | --------------------------- | --------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dataDirectory`                               | `--data-directory`          | `UNDERCUTF1_DATADIRECTORY`                                | The directory to which JSON timing data is read or written from. This directory is also where Whisper models will be stored (if downloaded) for team radio transcription.                                                               |
| `logDirectory`                                | `--log-directory`           | `UNDERCUTF1_LOGDIRECTORY`                                 | The directory to which logs are written to.                                                                                                                                                                                             |
| `verbose`                                     | `-v\|--verbose`             | `UNDERCUTF1_VERBOSE`                                      | Whether verbose logging should be enabled. Default: `false`. Values: `true` or `false`.                                                                                                                                                 |
| `apiEnabled`                                  | `--with-api`                | `UNDERCUTF1_APIENABLED`                                   | Whether the app should expose an API at <http://localhost:61937>. Default: `false`. Values: `true` or `false`.                                                                                                                          |
| `notify`                                      | `--notify`                  | `UNDERCUTF1_NOTIFY`                                       | Whether the app should sent audible BELs to your terminal when new race control messages are received. Default: `true`. Values: `true` or `false`.                                                                                      |
| `preferFfmpegPlayback`                        | `--prefer-ffmpeg`           | `UNDERCUTF1_PREFERFFMPEGPLAYBACK`                         | Prefer the usage of `ffplay` for playing Team Radio on Mac/Linux, instead of afplay/mpg123 respectively. `ffplay` is always used on Windows. Default: `false`. Values: `true` or `false`.                                               |
| `forceGraphicsProtocol`                       | `--force-graphics-protocol` | `UNDERCUTF1_FORCEGRAPHICSPROTOCOL`                        | Forces the usage of a particular graphics protocol instead of using heuristics to find a supported one. Values: `Kitty`, `Sixel`, or `iTerm`.                                                                                           |
| `preventDisplaySleep`                         | `--prevent-sleep`           | `UNDERCUTF1_PREVENTDISPLAYSLEEP`                          | If enabled, will attempt to prevent the device/display from sleeping whilst the app is running. Intended to help prevent accidental stream disconnection due to inactivity on your device. Default: `false`. Values: `true` or `false`. |
| `formula1AccessToken`                         | N/A                         | `UNDERCUTF1_FORMULA1ACCESSTOKEN`                          | The access token to use when connecting to the F1 Live Timing feed. Only required to see additional data feeds.                                                                                                                         |
| `externalPlayerSync.enabled`                  | N/A                         | `UNDERCUTF1_EXTERNALPLAYERSYNC__ENABLED`                  | Whether synchronisation of the session clock is enabled. Default: `false`. Values: `true` or `false`.                                                                                                                                   |
| `externalPlayerSync.url`                      | N/A                         | `UNDERCUTF1_EXTERNALPLAYERSYNC__URL`                      | The URL to use when connecting to the external services websocket. For Kodi, this will be something like `ws://localhost:9090/jsonrpc`.                                                                                                 |
| `externalPlayerSync.serviceType`              | N/A                         | `UNDERCUTF1_EXTERNALPLAYERSYNC__SERVICETYPE`              | What type of service is behind the provided URL to synchronise with. Values: `Kodi`.                                                                                                                                                    |
| `externalPlayerSync.webSocketConnectInterval` | N/A                         | `UNDERCUTF1_EXTERNALPLAYERSYNC__WEBSOCKETCONNECTINTERVAL` | If the websocket connection fails or ends, how long to wait before trying again (in ms). Default: `30000`.                                                                                                                              |

### Default Directories

UndercutF1 tries to adhere the Windows and XDG standards as much as possible. By default, timing data and logs are written/read at the following directories:

| Type        | Windows                                | Linux/Mac                                  | Linux/Mac Fallback                  |
| ----------- | -------------------------------------- | ------------------------------------------ | ----------------------------------- |
| Config File | `$env:APPDATA/undercut-f1/config.json` | `$XDG_CONFIG_HOME/undercut-f1/config.json` | `~/.config/undercut-f1/config.json` |
| Data        | `$env:LOCALAPPDATA/undercut-f1/data`   | `$XDG_DATA_HOME/undercut-f1/data`          | `~/.local/share/undercut-f1/data`   |
| Logs        | `$env:LOCALAPPDATA/undercut-f1/logs`   | `$XDG_STATE_HOME/undercut-f1/logs`         | `~/.local/state/undercut-f1/logs`   |

Data and Logs paths can be configured as [described above](#configuration).
The config file location cannot be modified, and will always be read from the above location.

### F1 TV Account Login

Since the 2025 Dutch GP, the F1 Live Timing feed no longer freely publishes some of the data we need to provide all functionality in undercut-f1.

The following functionality is now only available to those who have an active F1 TV subscription:

- Driver Tracker (live position of each car is unavailable)
- DRS Indicator in Timing Screens
- Team/Drivers championship tables
- Pit Stop times (in box and in pitlane)

Importing a session after it finishes still fetches all the data, so all functionality is available if you use undercut-f1 after a race finishes.

Linking your F1 TV account with undercut-f1 is as simple as running `undercutf1 login` before starting your session, and following the instructions displayed. undercutf1 will open a WebView on to the F1 website, allowing you to sign in. It will then read the generated access token cookie for you from the WebView, and save it in the undercutf1 config file. This token is usually valid for a week, so you will likely have to perform this action often.

If you've already started a session, you can launch the login window using the <kbd>A</kbd> `Account` screen, then pressing <kbd>L</kbd> `Login`.

The current validity of your access token can be shown by running `undercutf1 info` (or in the Info screen of the TUI). A note will also be displayed on the entrypoint display of the TUI, reminding you to re-login if the current token has expired.

If the `undercutf1 login` command does not work for you, there is a manual process:

1. In a web browser, navigate to [the Formula 1 Account Login](https://account.formula1.com/#/en/login)
2. Log in with your account credentials
3. Open the browsers web inspector/inspect element/dev console
4. Navigate to the cookie storage section (usually in `Application` or `Storage`)
5. Copy the value of the `login-session` cookie stored under the `static.formula1.com` or `formula1.com` domain
6. Paste this string (without modification) in to the undercutf1 config file with the key `"formula1AccessToken"`

Example:

```json
{
  "$schema": "https://raw.githubusercontent.com/JustAman62/undercut-f1/refs/heads/master/config.schema.json",
  "formula1AccessToken": "%7B%22data%22%3A%7B%22subscriptionToken%22%3A%22eyJ....."
}
```

## Logging

`UndercutF1.Data` writes logs using the standard `ILogger` implementation. SignalR client logs are also passed to the standard `ILoggerProvider`.

When running `undercutf1` logs are available in two places:

- Logs are stored in memory and viewable the <kbd>L</kbd> `Logs` screen. Logs can be scrolled on this screen, and the minimum level of logs shown can be changed with the <kbd>M</kbd> `Minimum Log Level` action.
- Log files are written to the [configured log directory](#default-directories).

Default log level is set to `Information`. More verbose logging can be enabled with the [`verbose` config option](#configuration).

## Alternate Key Binds

`undercutf1` displays the keys associated with each action that is applicable for the current screen.
However, the shown keys aren't the only keys which can trigger the action, there are alternative keys which may be more intuitive for users of other TUIs.

- <kbd>H</kbd>,<kbd>J</kbd>,<kbd>K</kbd>,<kbd>L</kbd> can be used in place of arrow keys.
- <kbd>Backspace</kbd> can be used in place of the <kbd>Escape</kbd> key.
- <kbd>^ Control</kbd>+<kbd>C</kbd> can always be used to go back a screen, or exit the app. You may need to press multiple times to completely exit the app.
- <kbd>⇧ Shift</kbd> can be held for certain actions to modify the action. For example, hold shift to change delay by 30 seconds instead of 5, or to move the cursor by 5 positions instead of 1.

## Data Recording and Replay

All events received by the live timing client will be written to the configured `Data Directory`, see [see Configuration for details](#configuration). Files will be written to a subdirectory named using the current sessions name, e.g. `<data-directory>/2025_Jeddah_Race/`. In this directory, two files will be written:

- `subscribe.json` contains the data received at subscription time (i.e. when the live timing client connected to the stream)
- `live.jsonl` contains an append-log of every message received in the stream

Both of these files are required for future simulations/replays. The `IJsonTimingClient` supports loading these files and processing them in the same way live data would be. Data points will be replayed in real time, using an adjustable delay.

## API

undercut-f1 ships with a simple HTTP API which allows for local integrations with your timing screen/data. [see Configuration for details on how to enable the API](#configuration). Once enabled, visit [the Swagger UI page at http://localhost:61937/swagger/index.html](http://localhost:61937/swagger/index.html) to see what APIs are available. This is an easy place to test the API and see what kind of data you get, and also the schema.

### Control API

The Control API `POST http://localhost:61937/control` allows you to issue control commands to the currently running session to, for example, pause the session clock.

```sh
# Pause the session clock
curl -H "content-type:application/json" -X POST http://localhost:61937/control -d '{"operation": "PauseClock"}'
# Resume the session clock
curl -H "content-type:application/json" -X POST http://localhost:61937/control -d '{"operation": "ResumeClock"}'
# Toggle (Play/Pause) the session clock
curl -H "content-type:application/json" -X POST http://localhost:61937/control -d '{"operation": "ToggleClock"}'
```

### Data API

The Data API `POST http://localhost:61938/data/<data-type>/latest` allow you to fetch the current raw timing data state. Take a look at the Swagger schema, and the [source model files](./UndercutF1.Data/Models/TimingDataPoints/) to understand more about what data is available for each timing `data-type`.

## External Session Clock Sync

If you watch F1 using an app like Kodi, you can choose to connect undercut-f1 to the Kodi API so that the undercut-f1's session clock automatically pauses when you pause the Kodi feed.
This allows you to keep your timing screen synchronised with your video feed.

See [Configuration](#configuration) for how to configure the `externalPlayerSync` options to enable this functionality.
Once configured, you can use the `Info` view in undercut-f1 to confirm its correct, and see if the WebSocket successfully connected.
[Logs](#logging) are also output with information or any errors related to this functionality.

> [!NOTE]
> Currently this functionality is only supported for Kodi over its [WebSocket JSON RPC API](https://kodi.wiki/view/JSON-RPC_API#WebSocket).
> If you use a different video player, please raise a GitHub issue and we may be able to implement support!
> You can also make your own integrations with our [Control API](#control-api).

## Notice

undercut-f1 is unofficial and are not associated in any way with the Formula 1 companies. F1, FORMULA ONE, FORMULA 1, FIA FORMULA ONE WORLD CHAMPIONSHIP, GRAND PRIX and related marks are trade marks of Formula One Licensing B.V.

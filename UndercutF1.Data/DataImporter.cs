using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UndercutF1.Data;

public sealed class DataImporter(
    IHttpClientFactory httpClientFactory,
    IOptions<LiveTimingOptions> options,
    ILogger<DataImporter> logger
) : IDataImporter
{
    private static readonly string[] _raceTopics =
    [
        "Heartbeat",
        "CarData.z",
        "Position.z",
        "ExtrapolatedClock",
        "TopThree",
        "TimingStats",
        "TimingAppData",
        "WeatherData",
        "TrackStatus",
        "DriverList",
        "RaceControlMessages",
        "SessionData",
        "LapCount",
        "TimingData",
        "ChampionshipPrediction",
        "TeamRadio",
        "PitLaneTimeCollection",
        "PitStopSeries",
        "PitStop",
    ];

    private static readonly string[] _nonRaceTopics =
    [
        "Heartbeat",
        "CarData.z",
        "Position.z",
        "ExtrapolatedClock",
        "TopThree",
        "TimingStats",
        "TimingAppData",
        "WeatherData",
        "TrackStatus",
        "DriverList",
        "RaceControlMessages",
        "SessionData",
        "TimingData",
        "TeamRadio",
        "PitLaneTimeCollection",
        "PitStopSeries",
        "PitStop",
    ];

    /// <inheritdoc />
    public async Task<ListMeetingsApiResponse> GetMeetingsAsync(int year)
    {
        var httpClient = httpClientFactory.CreateClient("Default");
        var url = $"https://livetiming.formula1.com/static/{year}/Index.json";
        return await httpClient
                .GetFromJsonAsync(url, TimingDataSerializerContext.Raw.ListMeetingsApiResponse)
                .ConfigureAwait(false)
            ?? throw new InvalidOperationException("An error occurred parsing the API response");
    }

    /// <inheritdoc />
    public async Task ImportSessionAsync(int year, int meetingKey, int sessionKey)
    {
        var res = await GetMeetingsAsync(year);
        var meeting =
            res.Meetings.SingleOrDefault(x => x.Key == meetingKey)
            ?? throw new KeyNotFoundException($"Meeting with key {meetingKey} not found");
        await ImportSessionAsync(year, meeting, sessionKey);
    }

    /// <inheritdoc />
    public async Task ImportSessionAsync(
        int year,
        ListMeetingsApiResponse.Meeting meeting,
        int sessionKey
    )
    {
        var session =
            meeting.Sessions.SingleOrDefault(x => x.Key == sessionKey)
            ?? throw new KeyNotFoundException($"Meeting with key {sessionKey} not found");

        if (string.IsNullOrWhiteSpace(session.Path))
        {
            throw new InvalidOperationException(
                $"This session cannot be imported as it has no Path property defined. This is usually because the session has not completed yet."
            );
        }

        var location = meeting.Location;
        var sessionName = session.Name;

        logger.LogInformation(
            "Downloading data for session {Year} {Location} {SessionName}",
            year,
            location,
            sessionName
        );

        var directory = Path.Join(
            options.Value.DataDirectory,
            $"{year}_{location}_{sessionName}".Replace(' ', '_')
        );

        var liveFilePath = Path.Join(directory, "live.jsonl");
        var subscribeFilePath = Path.Join(directory, "subscribe.json");

        if (File.Exists(liveFilePath))
        {
            logger.LogError(
                "Live data file at '{Path}' already exists. Delete this file before importing data.",
                liveFilePath
            );
            return;
        }

        if (File.Exists(subscribeFilePath))
        {
            logger.LogError(
                "Subscribe data file at '{Path}' already exists. Delete this file before importing data.",
                subscribeFilePath
            );
            return;
        }

        var prefix = $"https://livetiming.formula1.com/static/{session.Path}";

        var sessionDataPoint = await GetDataAsync(prefix, "SessionInfo", DateTimeOffset.UnixEpoch)
            .ConfigureAwait(false);
        var heartbeatDataPoint = await GetDataAsync(prefix, "Heartbeat", DateTimeOffset.UnixEpoch)
            .ConfigureAwait(false);

        if (heartbeatDataPoint.First().Json["Utc"] is null)
        {
            throw new InvalidOperationException(
                "Unable to find the first heartbeat data point for the session"
            );
        }

        // Records returned by the API don't have a timestamp, instead they have an offset from the very first datapoint sent
        // We need to calculate when that first data point was
        // The heartbeat data point includes the Utc timestamp from when the message was sent,
        // So use that timestamp, and take away the offset from when that message was received
        // To get the real start of the sessions data stream
        var startDate = ((DateTimeOffset)heartbeatDataPoint.First().Json["Utc"]!).AddMilliseconds(
            -heartbeatDataPoint.First().DateTime.ToUnixTimeMilliseconds()
        );

        logger.LogInformation(
            "Found start date {StartDate} with other {Other}",
            startDate,
            heartbeatDataPoint.First().DateTime
        );

        var topics = session.Type == "Race" ? _raceTopics : _nonRaceTopics;
        var tasks = topics.Select(topic => GetDataAsync(prefix, topic, startDate));

        var dataPointsCollection = await Task.WhenAll(tasks).ConfigureAwait(false);
        var lines = dataPointsCollection
            .SelectMany(x => x)
            .OrderBy(x => x.DateTime)
            .Select(x =>
                JsonSerializer.Serialize(x, TimingDataSerializerContext.Raw.RawTimingDataPoint)
            )
            .ToList();

        logger.LogInformation("Saving session data to {FilePath}", liveFilePath);

        Directory.CreateDirectory(directory);

        await File.WriteAllLinesAsync(liveFilePath, lines, Encoding.UTF8).ConfigureAwait(false);

        logger.LogInformation("Saving initial session data to {FilePath}", subscribeFilePath);
        var subscribeJson = new JsonObject
        {
            ["SessionInfo"] = sessionDataPoint.First().Json,
            ["Heartbeat"] = heartbeatDataPoint.First().Json,
        };
        await File.WriteAllTextAsync(subscribeFilePath, subscribeJson.ToString(), Encoding.UTF8)
            .ConfigureAwait(false);

        logger.LogInformation("Written {LineCount} lines of session data", lines.Count);
    }

    private async Task<List<RawTimingDataPoint>> GetDataAsync(
        string urlPrefix,
        string type,
        DateTimeOffset startDateTime
    )
    {
        var url = $"{urlPrefix}{type}.jsonStream";
        logger.LogDebug("Downloading {Type} data from {Url}", type, url);

        try
        {
            var httpClient = httpClientFactory.CreateClient("Default");
            var rawData = await httpClient.GetStringAsync(url).ConfigureAwait(false);
            var lines = rawData.Split('\n');
            return lines
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(line => new RawTimingDataPoint(
                    type,
                    JsonNode.Parse(line[12..])!,
                    startDateTime + TimeSpan.Parse(line[..12])
                ))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download {Type} data from {Url}", type, url);
            return [];
        }
    }
}

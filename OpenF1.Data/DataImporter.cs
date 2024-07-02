using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenF1.Data;

public sealed class DataImporter(IOptions<LiveTimingOptions> options, ILogger<DataImporter> logger)
    : IDataImporter
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
        "TeamRadio"
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
        "TeamRadio"
    ];

    /// <inheritdoc />
    public async Task<ListMeetingsApiResponse> GetMeetingsAsync(int year)
    {
        // TODO: Get this HttpClient from DI IHttpClientFactory
        var httpClient = new HttpClient();
        var url = $"https://livetiming.formula1.com/static/{year}/Index.json";
        return await httpClient.GetFromJsonAsync<ListMeetingsApiResponse>(url).ConfigureAwait(false)
            ?? throw new InvalidOperationException("An error occurred parsing the API response");
    }

    /// <inheritdoc />
    public async Task ImportSessionAsync(int year, int meetingKey, int sessionKey)
    {
        var res = await GetMeetingsAsync(year);
        var meeting =
            res.Meetings.SingleOrDefault(x => x.Key == meetingKey)
            ?? throw new KeyNotFoundException($"Meeting with key {meetingKey} not found");
        await ImportSessionAsync(meeting, sessionKey);
    }

    /// <inheritdoc />
    public async Task ImportSessionAsync(ListMeetingsApiResponse.Meeting meeting, int sessionKey)
    {
        var session =
            meeting.Sessions.SingleOrDefault(x => x.Key == sessionKey)
            ?? throw new KeyNotFoundException($"Meeting with key {sessionKey} not found");
        var location = meeting.Location;
        var sessionName = session.Name;

        logger.LogInformation(
            "Downloading data for session {Location} {SessionName}",
            location,
            sessionName
        );

        var directory = Path.Join(
            options.Value.DataDirectory,
            $"{location}_{sessionName}".Replace(' ', '_')
        );

        var liveFilePath = Path.Join(directory, "live.txt");
        var subscribeFilePath = Path.Join(directory, "subscribe.txt");

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

        var startDate = new DateTimeOffset(session.StartDate, TimeSpan.Zero) - session.GmtOffset;
        var sessionDataPoint = await GetDataAsync(prefix, "SessionInfo", startDate)
            .ConfigureAwait(false);

        var topics = session.Type == "Race" ? _raceTopics : _nonRaceTopics;
        var tasks = topics.Select(topic => GetDataAsync(prefix, topic, startDate));

        var dataPointsCollection = await Task.WhenAll(tasks).ConfigureAwait(false);
        var lines = dataPointsCollection
            .SelectMany(x => x)
            .OrderBy(x => x.DateTime)
            .Select(x => JsonSerializer.Serialize(x))
            .ToList();

        logger.LogInformation("Saving session data to {FilePath}", liveFilePath);

        Directory.CreateDirectory(directory);

        await File.WriteAllLinesAsync(liveFilePath, lines, Encoding.UTF8).ConfigureAwait(false);

        logger.LogInformation("Saving session data to {FilePath}", subscribeFilePath);
        var subscribeJson = new JsonObject
        {
            ["SessionInfo"] = sessionDataPoint.First().Json,
            ["Heartbeat"] = new JsonObject
            {
                ["Utc"] = startDate.ToString(@"yyyy-MM-dd\THH:mm:ss.ffffff\Z")
            }
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
        logger.LogInformation("Downloading {Type} data from {Url}", type, url);

        try
        {
            // TODO: Get this HttpClient from DI IHttpClientFactory
            var httpClient = new HttpClient();
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
            logger.LogError(ex, "Failed to downloaed {Type} data from {Url}", type, url);
            return [];
        }
    }
}

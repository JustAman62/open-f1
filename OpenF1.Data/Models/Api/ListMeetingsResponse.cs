namespace OpenF1.Data;

/// <summary>
/// The response model for the F1 Live Timing meetings and sessions index.
/// </summary>
public record ListMeetingsApiResponse
{
    public required int Year { get; set; }
    public required List<Meeting> Meetings { get; set; }

    public record Meeting
    {
        public required int Key { get; set; }
        public required string Name { get; set; }
        public required string Location { get; set; }
        public required List<Session> Sessions { get; set; }

        public record Session
        {
            public required int Key { get; set; }
            public required string Name { get; set; }
            public required string Path { get; set; }
            public required string Type { get; set; }
        }
    }
}

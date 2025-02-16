namespace OpenF1.Data;

public interface IDataImporter
{
    /// <summary>
    /// Gets all the meetings for the provided <paramref name="year"/>.
    /// </summary>
    /// <param name="year">The year to find meetings for.</param>
    /// <returns>The <see cref="ListMeetingsApiResponse"/> returned by the F1 API.</returns>
    Task<ListMeetingsApiResponse> GetMeetingsAsync(int year);

    /// <summary>
    /// Imports all the live timing data for the session in the given <paramref name="year"/>
    /// with the provided <paramref name="meetingKey"/> and <paramref name="sessionKey"/>.
    /// </summary>
    /// <param name="year">The year (format YYYY) in which session took place.</param>
    /// <param name="meetingKey">The <see cref="ListMeetingsApiResponse.Meeting.Key"/> key for the meeting.</param>
    /// <param name="sessionKey">The <see cref="ListMeetingsApiResponse.Meeting.Session.Key"/> key for the session.</param>
    /// <returns>A <see cref="Task"/> representing the status of the import.</returns>
    Task ImportSessionAsync(int year, int meetingKey, int sessionKey);

    /// <summary>
    /// Imports all the live timing data for the provided <paramref name="session"/>,
    /// in to the <see cref="LiveTimingOptions.DataDirectory"/>.
    /// </summary>
    /// <param name="meeting">The <see cref="ListMeetingsApiResponse.Meeting"/> which contains the session to import data for.</param>
    /// <param name="session">The key of the session inside the meeting to import data for.</param>
    /// <returns>A <see cref="Task"/> representing the status of the import.</returns>
    Task ImportSessionAsync(int year, ListMeetingsApiResponse.Meeting meeting, int sessionKey);
}

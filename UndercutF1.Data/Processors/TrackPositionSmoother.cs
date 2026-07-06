using System.Collections.Concurrent;
using EntryStatus = UndercutF1.Data.PositionDataPoint.PositionData.Entry.DriverStatus;

namespace UndercutF1.Data;

/// <summary>
/// Smooths driver-tracker dots: each car is rendered a short delay behind the playback clock,
/// its position fitted from recent samples so it glides between the feed's bursty updates rather
/// than teleporting. Consumes the same Position feed as <see cref="PositionDataProcessor"/>.
/// </summary>
public sealed class TrackPositionSmoother : IProcessor<PositionDataPoint>
{
    // Render behind the clock so the fit sits between real samples; must exceed the feed's
    // largest update gap (~1.25s).
    private static readonly TimeSpan RenderDelay = TimeSpan.FromSeconds(1.5);

    private const int SmoothingPoints = 7;
    private const int HistoryCapacity = 32;

    private sealed class DriverState
    {
        // Locked on itself: the ingestion thread appends while the render thread snapshots it.
        public readonly List<(DateTimeOffset Time, int X, int Y)> History = [];
        public bool OffTrack;
    }

    private readonly ConcurrentDictionary<string, DriverState> _drivers = new();

    public PositionDataPoint Latest { get; private set; } = new();

    public void Process(PositionDataPoint data)
    {
        Latest = data;
        foreach (var batch in data.Position)
        {
            foreach (var (driverNumber, entry) in batch.Entries)
            {
                if (!entry.X.HasValue || !entry.Y.HasValue)
                    continue;

                var state = _drivers.GetOrAdd(driverNumber, _ => new DriverState());
                state.OffTrack = entry.Status == EntryStatus.OffTrack;
                lock (state.History)
                {
                    // Off-track samples (pit lane, gravel) aren't drawn; drop the history so a
                    // return to track doesn't smooth a streak across the excursion.
                    if (state.OffTrack)
                    {
                        state.History.Clear();
                        continue;
                    }
                    state.History.Add((batch.Timestamp, entry.X.Value, entry.Y.Value));
                    if (state.History.Count > HistoryCapacity)
                        state.History.RemoveAt(0);
                }
            }
        }
    }

    public bool TryGetSmoothed(
        string driverNumber,
        DateTimeOffset now,
        out (double x, double y) point
    )
    {
        point = default;
        if (!_drivers.TryGetValue(driverNumber, out var state) || state.OffTrack)
            return false;

        (DateTimeOffset Time, int X, int Y)[] hist;
        lock (state.History)
            hist = [.. state.History];
        if (hist.Length == 0)
            return false;

        point = FitPoint(hist, now - RenderDelay);
        return true;
    }

    // Fits a line (in x and y) to the SmoothingPoints samples nearest target. Time is measured
    // from target, so each line's intercept is the value at target.
    private static (double x, double y) FitPoint(
        (DateTimeOffset Time, int X, int Y)[] hist,
        DateTimeOffset target
    )
    {
        // Outside the buffered range (e.g. right after a reset clears history) hold the near end.
        if (target <= hist[0].Time)
            return (hist[0].X, hist[0].Y);
        if (target >= hist[^1].Time)
            return (hist[^1].X, hist[^1].Y);

        var hi = 0;
        while (hi < hist.Length && hist[hi].Time <= target)
            hi++;
        var lo = hi - 1;

        double n = 0,
            st = 0,
            st2 = 0,
            sx = 0,
            stx = 0,
            sy = 0,
            sty = 0;
        for (var taken = 0; taken < SmoothingPoints && (lo >= 0 || hi < hist.Length); taken++)
        {
            var takeLo =
                hi >= hist.Length || (lo >= 0 && target - hist[lo].Time <= hist[hi].Time - target);
            var (time, x, y) = takeLo ? hist[lo--] : hist[hi++];
            var t = (time - target).TotalSeconds;
            n++;
            st += t;
            st2 += t * t;
            sx += x;
            stx += t * x;
            sy += y;
            sty += t * y;
        }

        // denom is zero only if every sampled timestamp is identical; then hold the last sample.
        var denom = (n * st2) - (st * st);
        if (Math.Abs(denom) < 1e-9)
            return (hist[^1].X, hist[^1].Y);
        return (
            (sx - (((n * stx) - (st * sx)) / denom * st)) / n,
            (sy - (((n * sty) - (st * sy)) / denom * st)) / n
        );
    }
}

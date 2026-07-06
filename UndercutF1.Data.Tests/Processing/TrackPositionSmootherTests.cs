using EntryStatus = UndercutF1.Data.PositionDataPoint.PositionData.Entry.DriverStatus;

namespace UndercutF1.Data.Tests;

public class TrackPositionSmootherTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    private static PositionDataPoint Batch(
        DateTimeOffset ts,
        int x,
        int y,
        EntryStatus status = EntryStatus.OnTrack
    ) =>
        new()
        {
            Position =
            [
                new()
                {
                    Timestamp = ts,
                    Entries = new()
                    {
                        ["44"] = new()
                        {
                            X = x,
                            Y = y,
                            Status = status,
                        },
                    },
                },
            ],
        };

    [Fact]
    public void InterpolatesBetweenTheSamplesBracketingTheRenderTime()
    {
        // Arrange: samples 1s apart moving +1000 units along x.
        var smoother = new TrackPositionSmoother();
        for (var i = 0; i <= 3; i++)
            smoother.Process(Batch(T0.AddSeconds(i), i * 1000, 0));

        // Act: RenderDelay is 1.5s, so querying at T0+3 renders at T0+1.5 -- midway between the
        // samples at T0+1 (1000) and T0+2 (2000).
        var found = smoother.TryGetSmoothed("44", T0.AddSeconds(3), out var p);

        // Assert
        Assert.True(found);
        Assert.Equal(1500, p.x, 3);
        Assert.Equal(0, p.y, 3);
    }

    [Fact]
    public void HoldsAtTheBufferEndsOutsideItsTimeRange()
    {
        // Arrange
        var smoother = new TrackPositionSmoother();
        for (var i = 0; i <= 2; i++)
            smoother.Process(Batch(T0.AddSeconds(i), i * 1000, 0));

        // Act + Assert: before the oldest sample, hold the oldest...
        Assert.True(smoother.TryGetSmoothed("44", T0, out var oldest));
        Assert.Equal(0, oldest.x, 3);

        // ...and past the newest, hold the newest.
        Assert.True(smoother.TryGetSmoothed("44", T0.AddSeconds(10), out var newest));
        Assert.Equal(2000, newest.x, 3);
    }

    [Fact]
    public void OffTrackReturnsFalse()
    {
        // Arrange
        var smoother = new TrackPositionSmoother();
        smoother.Process(Batch(T0, 5000, 0, EntryStatus.OffTrack));

        // Act + Assert: off-track cars fall back to their raw position in the display.
        Assert.False(smoother.TryGetSmoothed("44", T0, out _));
    }

    [Fact]
    public void SmoothsSpeedAcrossJitteryTimestamps()
    {
        // Arrange: constant motion (+1000 units/sample) but samples arrive at irregular
        // timestamps. Interpolating straight between two samples would pulse -- segment speed
        // (1000/gap) swings from ~700 to ~2000 units/s -- whereas the line fit averages it out.
        var smoother = new TrackPositionSmoother();
        double[] times = [0, 1.3, 1.8, 3.2, 3.9, 5.1];
        for (var i = 0; i < times.Length; i++)
            smoother.Process(Batch(T0.AddSeconds(times[i]), i * 1000, 0));

        // Act: sample the rendered position at evenly-spaced times spanning the middle.
        var xs = new List<double>();
        for (var target = 1.5; target <= 3.5; target += 0.5)
        {
            Assert.True(smoother.TryGetSmoothed("44", T0.AddSeconds(target + 1.5), out var p));
            xs.Add(p.x);
        }

        // Assert: equal query spacing gives near-equal position steps -- a smooth, steady speed.
        var firstStep = xs[1] - xs[0];
        for (var i = 2; i < xs.Count; i++)
            Assert.InRange(xs[i] - xs[i - 1], firstStep * 0.9, firstStep * 1.1);
    }
}

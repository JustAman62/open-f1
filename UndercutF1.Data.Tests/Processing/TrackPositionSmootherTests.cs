using EntryStatus = UndercutF1.Data.PositionDataPoint.PositionData.Entry.DriverStatus;

namespace UndercutF1.Data.Tests;

public class TrackPositionSmootherTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset Utc { get; set; }
        public TimeSpan Delay { get; set; }
        public bool IsPaused => false;

        public void TogglePause() { }
    }

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
    public void RendersRenderDelayBehindTheNewestSample()
    {
        // Arrange: samples 1s apart moving +1000 units along x.
        var clock = new FakeClock();
        var smoother = new TrackPositionSmoother(clock);
        for (var i = 0; i <= 3; i++)
            smoother.Process(Batch(T0.AddSeconds(i), i * 1000, 0));

        // Act: the wall clock is far from feed time (as it is live), yet the first frame renders
        // RenderDelay (1.5s) behind the newest sample -- T0+1.5, midway between T0+1 (1000) and
        // T0+2 (2000) -- not clamped to the newest.
        clock.Utc = T0.AddSeconds(500);
        var found = smoother.TryGetSmoothed("44", out var p);

        // Assert
        Assert.True(found);
        Assert.Equal(1500, p.x, 3);
        Assert.Equal(0, p.y, 3);
    }

    [Fact]
    public void AdvancesWithWallTimeThenHoldsAtTheNewestSample()
    {
        // Arrange
        var clock = new FakeClock();
        var smoother = new TrackPositionSmoother(clock);
        for (var i = 0; i <= 3; i++)
            smoother.Process(Batch(T0.AddSeconds(i), i * 1000, 0));

        // Act: first frame anchors the cursor RenderDelay behind the newest, then each later frame
        // advances it by the real time elapsed.
        var xs = new List<double>();
        foreach (var wall in new[] { 500, 500.5, 501, 501.5, 502 })
        {
            clock.Utc = T0.AddSeconds(wall);
            Assert.True(smoother.TryGetSmoothed("44", out var p));
            xs.Add(p.x);
        }

        // Assert: it sweeps forward (1500, 2000, 2500) then holds at the newest sample (3000) once
        // it catches up, never overrunning it.
        Assert.Equal([1500, 2000, 2500, 3000, 3000], xs.Select(x => Math.Round(x)));
    }

    [Fact]
    public void OffTrackReturnsFalse()
    {
        // Arrange
        var clock = new FakeClock { Utc = T0 };
        var smoother = new TrackPositionSmoother(clock);
        smoother.Process(Batch(T0, 5000, 0, EntryStatus.OffTrack));

        // Act + Assert: off-track cars fall back to their raw position in the display.
        Assert.False(smoother.TryGetSmoothed("44", out _));
    }

    [Fact]
    public void SmoothsSpeedAcrossJitteryTimestamps()
    {
        // Arrange: constant motion (+1000 units/sample) but samples arrive at irregular
        // timestamps. Interpolating straight between two samples would pulse -- segment speed
        // (1000/gap) swings from ~700 to ~2000 units/s -- whereas the line fit averages it out.
        var clock = new FakeClock();
        var smoother = new TrackPositionSmoother(clock);
        double[] times = [0, 1.3, 1.8, 3.2, 3.9, 5.1];
        for (var i = 0; i < times.Length; i++)
            smoother.Process(Batch(T0.AddSeconds(times[i]), i * 1000, 0));

        // Act: the cursor starts 1.5s behind the newest (T0+3.6) and advances with the wall clock;
        // sample it over evenly-spaced frames while it stays inside the buffer.
        var xs = new List<double>();
        for (var frame = 0; frame < 4; frame++)
        {
            clock.Utc = T0.AddSeconds(500 + (frame * 0.3));
            Assert.True(smoother.TryGetSmoothed("44", out var p));
            xs.Add(p.x);
        }

        // Assert: equal frame spacing gives near-equal position steps -- a smooth, steady speed.
        var firstStep = xs[1] - xs[0];
        for (var i = 2; i < xs.Count; i++)
            Assert.InRange(xs[i] - xs[i - 1], firstStep * 0.9, firstStep * 1.1);
    }
}

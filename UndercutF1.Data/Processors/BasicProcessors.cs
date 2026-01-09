namespace UndercutF1.Data;

public class HeartbeatProcessor(IMerger merger) : ProcessorBase<HeartbeatDataPoint>(merger);

public class LapCountProcessor(IMerger merger) : ProcessorBase<LapCountDataPoint>(merger);

public class TimingAppDataProcessor(IMerger merger) : ProcessorBase<TimingAppDataPoint>(merger);

public class TrackStatusProcessor(IMerger merger) : ProcessorBase<TrackStatusDataPoint>(merger);

public class WeatherProcessor(IMerger merger) : ProcessorBase<WeatherDataPoint>(merger);

public class ChampionshipPredictionProcessor(IMerger merger)
    : ProcessorBase<ChampionshipPredictionDataPoint>(merger);

public class TimingStatsProcessor(IMerger merger) : ProcessorBase<TimingStatsDataPoint>(merger);

public class PitStopSeriesProcessor(IMerger merger) : ProcessorBase<PitStopSeriesDataPoint>(merger);

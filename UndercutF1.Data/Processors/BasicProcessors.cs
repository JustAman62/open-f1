namespace UndercutF1.Data;

public class HeartbeatProcessor() : ProcessorBase<HeartbeatDataPoint>();

public class LapCountProcessor() : ProcessorBase<LapCountDataPoint>();

public class TimingAppDataProcessor() : ProcessorBase<TimingAppDataPoint>();

public class TrackStatusProcessor() : ProcessorBase<TrackStatusDataPoint>();

public class WeatherProcessor() : ProcessorBase<WeatherDataPoint>();

public class ChampionshipPredictionProcessor() : ProcessorBase<ChampionshipPredictionDataPoint>();

public class TimingStatsProcessor() : ProcessorBase<TimingStatsDataPoint>();

public class PitStopSeriesProcessor() : ProcessorBase<PitStopSeriesDataPoint>();

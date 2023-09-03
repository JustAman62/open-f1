
SELECT * FROM RAW_DATA_POINTS
--where EventType = 'TimingData'
where EventType not in ('Position.z', 'CarData.z')
--where EventType in ('WeatherData', 'RaceControlMessages')
order by LOGD_TS desc

import Foundation

@Observable
class TimingData : Codable {
    var lines: Dictionary<DriverNumber, DriverData> = .init()
    
    @Observable
    class DriverData : Codable {
        var gapToLeader: String?
        var interval: Interval?
        var line: Int?
        var position: String?
        var inPit: Bool?
        var pitOut: Bool?
        var numberOfPitStops: Int?
        var numberOfLaps: Int?
        var lastLapTime: LapSectorTime?
        var sectors: Dictionary<String, LapSectorTime> = .init()
        var bestLapTime: BestLap?
        
        @Observable
        class Interval : Codable {
            var value: String?
            var catching: Bool?
        }
        
        @Observable
        class LapSectorTime : Codable {
            var value: String?
            var overallFastest: Bool?
            var personalFastest: Bool?
        }
        
        @Observable
        class BestLap : Codable {
            var value: String?
            var lap: Int?
        }
    }
}

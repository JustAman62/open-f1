import Foundation

@Observable
class TimingData : Codable {
    var lines: Dictionary<DriverNumber, DriverData> = .init()
    
    enum CodingKeys: String, CodingKey {
        case _lines = "lines"
    }
    
    @Observable
    class DriverData : Identifiable, Codable {
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
        
        enum CodingKeys: String, CodingKey {
            case _gapToLeader = "gapToLeader"
            case _interval = "interval"
            case _line = "line"
            case _position = "position"
            case _inPit = "inPit"
            case _pitOut = "pitOut"
            case _numberOfPitStops = "numberOfPitStops"
            case _numberOfLaps = "numberOfLaps"
            case _lastLapTime = "lastLapTime"
            case _sectors = "sectors"
            case _bestLapTime = "bestLapTime"
        }
        
        @Observable
        class Interval : Codable {
            var value: String?
            var catching: Bool?
            
            enum CodingKeys: String, CodingKey {
                case _value = "value"
                case _catching = "catching"
            }
        }
        
        @Observable
        class LapSectorTime : Codable {
            var value: String?
            var overallFastest: Bool?
            var personalFastest: Bool?
            
            enum CodingKeys: String, CodingKey {
                case _value = "value"
                case _overallFastest = "overallFastest"
                case _personalFastest = "personalFastest"
            }
        }
        
        @Observable
        class BestLap : Codable {
            var value: String?
            var lap: Int?
            
            enum CodingKeys: String, CodingKey {
                case _value = "value"
                case _lap = "lap"
            }
        }
    }
}

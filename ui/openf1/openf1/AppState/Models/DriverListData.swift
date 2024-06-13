import Foundation

@Observable
class DriverData : Codable {
    var racingNumber: String?
    var broadcastName: String?
    var fullName: String?
    var tla: String?
    var line: Int?
    var teamName: String?
    var teamColour: String?
}

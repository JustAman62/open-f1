import Foundation
import SwiftUI

@Observable
class DriverListData : Identifiable, Codable {
    var racingNumber: String?
    var broadcastName: String?
    var fullName: String?
    var tla: String?
    var line: Int?
    var teamName: String?
    var teamColour: String?
    
    enum CodingKeys: String, CodingKey {
        case _racingNumber = "racingNumber"
        case _broadcastName = "broadcastName"
        case _fullName = "fullName"
        case _tla = "tla"
        case _line = "line"
        case _teamName = "teamName"
        case _teamColour = "teamColour"
    }
}

extension DriverListData {
    var color: Color {
        Color.init(hex: self.teamColour ?? "FFFFFF")
    }
}

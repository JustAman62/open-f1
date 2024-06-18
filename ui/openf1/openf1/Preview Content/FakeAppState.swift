import Foundation
import SwiftUI

class FakeAppState: AppStateProtocol {
    lazy var timingData: TimingData = load("TimingData")
    lazy var timingDataHistory: Dictionary<LapNumber, Dictionary<DriverNumber, TimingData.DriverData>> = [
        1: load("TimingDataHistory_1"),
        2: load("TimingDataHistory_2")
    ]
    
    lazy var driverList: Dictionary<DriverNumber, DriverListData> = load("DriverList")
    
    private func load<T>(_ name: String) -> T where T: Decodable {
        guard let path = Bundle.main.path(forResource: name, ofType: "json")
        else { fatalError("Cannot load \(name) data as file could not be found")}
        
        do {
            let data = try Data(contentsOf: URL(filePath: path))
            return try JSONDecoder.shared.decode(T.self, from: data)
        } catch {
            fatalError("Failed to load JSON data \(error)")
        }
    }
}

extension View {
    func previewEnvironment() -> some View {
        return self
            .environment(\.appState, FakeAppState())
    }
}

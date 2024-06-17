import Foundation
import SwiftUI

class FakeAppState: AppStateProtocol {
    lazy var timingData: TimingData = { return load("TimingData") }()
    lazy var driverList: Dictionary<DriverNumber, DriverListData> = { return load("DriverList") }()
    
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

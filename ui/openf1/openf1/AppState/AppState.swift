import Foundation
import SwiftUI

protocol AppStateProtocol {
    var timingData: TimingData { get }
    var driverList: Dictionary<DriverNumber, DriverListData> { get }
}

@Observable
class AppState : AppStateProtocol {
    var timingData: TimingData { .init() }
    var driverList: Dictionary<DriverNumber, DriverListData> { .init() }
}

private struct AppStateEnvironmentKey : EnvironmentKey {
    static let defaultValue: AppStateProtocol = AppState()
}

extension EnvironmentValues {
    var appState: AppStateProtocol {
        get { self[AppStateEnvironmentKey.self] }
        set { self[AppStateEnvironmentKey.self] = newValue }
    }
}

//

import SwiftUI
import Charts

struct GapToLeaderChart: View {
    @Environment(\.appState) private var appState
    @Environment(SelectedDrivers.self) private var selectedDrivers
    
    private var laps: [(LapNumber, Dictionary<DriverNumber, TimingData.DriverData>)] {
        appState.timingDataHistory.sorted(using: KeyPathComparator(\.key))
    }
    
    var body: some View {
        Chart {
            ForEach(laps, id: \.0) { lap, timingData in
                ForEach(timingData.sorted, id: \.0) { driverNumber, data in
                    if selectedDrivers.isSelected(driverNumber) {
                        LineMark(
                            x: .value("Lap", lap),
                            y: .value("Gap", -data.gapToLeaderSeconds)
                        )
                        .foregroundStyle(by: .value("Driver", "\(driverNumber) \(appState.driverList[driverNumber]?.tla ?? "UNK")"))
                        .foregroundStyle(appState.driverList[driverNumber]?.color ?? .red)
                    }
                }
            }
        }
        .chartForegroundStyleScale(mapping: mapDrivers)
    }
    
    private func mapDrivers(x: String) -> Color {
        let driverNumber = x.split(separator: " ").first!
        return appState.driverList[String(driverNumber)]!.color
    }
}

#Preview {
    GapToLeaderChart()
        .previewEnvironment()
        .environment(SelectedDrivers.initAllSelected())
}

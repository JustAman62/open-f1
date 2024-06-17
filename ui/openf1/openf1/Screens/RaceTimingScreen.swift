import SwiftUI

struct RaceTimingScreen: View {
    @Environment(\.appState) private var appState
    
    var sortedTiming: [(DriverNumber, TimingData.DriverData)] {
        appState.timingData.lines.sorted(using: KeyPathComparator(\.value.line))
    }
    
    var body: some View {
        Table(of: RaceTimingScreenData.self) {
            TableColumn("Driver", content: { data in
                Text(data.driver.tla ?? "UNK")
                    .background(Color.init(hex: data.driver.teamColour ?? "FFFFFF"))
            })
            .alignment(.trailing)
            
            TableColumn("Gap To Leader", content: { data in
                Text(data.timing.gapToLeader ?? "UNK")
            })
            .alignment(.trailing)
        } rows: {
            ForEach(sortedTiming, id: \.0) { driverNumber, timing in
                if let driver = appState.driverList[driverNumber] {
                    let rowData = RaceTimingScreenData(timing: timing, driver: driver)
                    TableRow(rowData)
                }
            }
        }
        .font(.body.monospaced())
    }
}

private struct RaceTimingScreenData: Identifiable {
    var id: Int { timing.line ?? 0 }
    
    var timing: TimingData.DriverData
    var driver: DriverListData
}

#if DEBUG
#Preview {
    RaceTimingScreen()
        .previewEnvironment()
}
#endif

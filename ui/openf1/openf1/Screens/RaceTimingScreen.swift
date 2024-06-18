import SwiftUI

struct RaceTimingScreen: View {
    @Environment(\.appState) private var appState
    
    var body: some View {
        ScrollView([.horizontal, .vertical]) {
            Grid {
                GridRow {
                    Text("")
                    Text("Gap")
                    Text("Interval")
                    Text("")
                    Text("Best")
                    Text("Last")
                    Text("")
                    Text("Sector 1")
                    Text("Sector 2")
                    Text("Sector 3")
                }
                .font(.headline)
                
                ForEach(appState.timingData.sorted, id: \.0) { driverNumber, timing in
                    GridRow {
                        gridRow(driverNumber: driverNumber, timing: timing)
                    }
                    .font(.body.monospaced())
                }
            }
        }
    }
    
    @ViewBuilder private func gridRow(driverNumber: String, timing: TimingData.DriverData) -> some View {
        if let driver = appState.driverList[driverNumber] {
            DriverTag(driver: driver)

            Text(timing.gapToLeader ?? "UNK")
            Text(timing.intervalToPositionAhead?.value ?? "UNK")
                .foregroundStyle(timing.intervalToPositionAhead?.isInsideDrs ?? false ? .green : .primary)
            
            Divider()
                .gridCellUnsizedAxes(.horizontal)
            
            Text(timing.bestLapTime?.value ?? "UNK")
            sectorTime(timing.lastLapTime ?? .init())
            
            Divider()
                .gridCellUnsizedAxes(.horizontal)
            
            sectorTime(timing.sectors["0"] ?? .init())
            sectorTime(timing.sectors["1"] ?? .init())
            sectorTime(timing.sectors["2"] ?? .init())
                
        } else {
            Text("Could not find all expected data")
        }
    }
    
    @ViewBuilder private func sectorTime(_ sector: TimingData.DriverData.LapSectorTime) -> some View {
        let color: any ShapeStyle = 
        if sector.overallFastest ?? false {
            .purple
        } else if sector.personalFastest ?? false {
            .green
        } else {
            .primary
        }
        
        Text(sector.value.orUnknown())
            .foregroundStyle(color)
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

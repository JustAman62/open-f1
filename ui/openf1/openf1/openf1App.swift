//

import SwiftUI

@main
struct openf1App: App {
    @State var appState = AppState()
    
    var body: some Scene {
        Group {
            WindowGroup {
                ContentView()
            }
            
#if os(macOS)
            Window("Timing Screen", id: "timing-screen") {
                RaceTimingScreen()
            }
            
            Window("Charts", id: "charts-screen") {
                ChartScreen()
            }
            
            Window("Raw Data", id: "raw-data") {
                RawDataScreen()
            }
#endif
        }
        .environment(\.appState, appState)
    }
}

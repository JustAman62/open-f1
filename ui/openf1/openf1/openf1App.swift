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
            
            Window("Timing Screen", id: "timing-screen") {
                RaceTimingScreen()
            }
            
            Window("Raw Data", id: "raw-data") {
                RawDataScreen()
            }
        }
        .environment(\.appState, appState)
    }
}

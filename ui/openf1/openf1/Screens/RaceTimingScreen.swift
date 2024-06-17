import SwiftUI

struct RaceTimingScreen: View {
    @Environment(\.appState) private var appState
    
    var body: some View {
        HStack {
            Text("Driver")
            Divider()
            Text("Gap")
        }
        .frame(height: 50)
    }
}

#if DEBUG
#Preview {
    TimingScreen()
        .previewEnvironment()
}
#endif

import SwiftUI

struct ChartScreen: View {
    @State private var selectedDrivers: SelectedDrivers = .init()

    var body: some View {
        HStack {
            DriverSelection(selectedDrivers: selectedDrivers)
            
            VStack {
                Text("Gap To Leader")
                    .font(.title)
                GapToLeaderChart()
            }
            .padding()
        }
        .environment(selectedDrivers)
    }
}

@Observable
class SelectedDrivers {
    var selected: Set<DriverNumber> = []
    
    func select(_ driverNumber: DriverNumber) { selected.insert(driverNumber) }
    func deselect(_ driverNumber: DriverNumber) { selected.remove(driverNumber) }
    func isSelected(_ driverNumber: DriverNumber) -> Bool { selected.contains(driverNumber) }
    
    func overwrite(_ new: Set<DriverNumber>) {
        selected.removeAll()
        selected.formUnion(new)
    }
}

#Preview {
    ChartScreen()
        .previewEnvironment()
}

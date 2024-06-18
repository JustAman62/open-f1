import SwiftUI

struct DriverSelection: View {
    var selectedDrivers: SelectedDrivers
    
    @Environment(\.appState) private var appState
    
    private var sortedDrivers: [(DriverNumber, DriverListData)] {
        appState.driverList.sorted(using: KeyPathComparator(\.key))
    }
    
    var body: some View {
        ScrollView {
            Button("Select All") {
                selectedDrivers.overwrite(Set(appState.driverList.map({ $0.key })))
            }
            
            Button("Clear") {
                selectedDrivers.overwrite([])
            }
            
            ForEach(sortedDrivers, id: \.0) { number, driver in
                Toggle(isOn: .init(
                    get: { selectedDrivers.isSelected(number) },
                    set: {
                        if $0 {
                            selectedDrivers.select(number)
                        } else {
                            selectedDrivers.deselect(number)
                        }
                    }
                )) {
                    
                    DriverTag(driver: driver)
                }
            }
#if !os(macOS)
            .environment(\.editMode, .constant(.active))
#endif
        }
    }
}

#Preview {
    @State var selectedDrivers: SelectedDrivers = .init()
    return DriverSelection(selectedDrivers: selectedDrivers)
        .previewEnvironment()
}

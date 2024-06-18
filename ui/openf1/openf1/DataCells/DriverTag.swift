import SwiftUI

struct DriverTag: View {
    var driver: DriverListData
    
    var body: some View {
        Text("\(driver.racingNumber.orUnknown().padding(toLength: 2, withPad: " ", startingAt: 0)) \(driver.tla.orUnknown())")
            .background(driver.color)
            .font(.body.monospaced())
    }
}

#Preview {
    DriverTag(driver: FakeAppState().driverList.first!.value)
}

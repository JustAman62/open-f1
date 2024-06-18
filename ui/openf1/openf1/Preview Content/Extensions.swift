import Foundation

extension SelectedDrivers {
    static func initAllSelected() -> SelectedDrivers {
        let drivers = SelectedDrivers()
        drivers.overwrite(Set(FakeAppState().driverList.map { $0.key } ))
        return drivers
    }
}

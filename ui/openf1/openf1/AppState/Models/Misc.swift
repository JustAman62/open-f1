import Foundation

typealias DriverNumber = String
typealias LapNumber = Int

extension String? {
    func orUnknown() -> String { self ?? "UNK" }
}

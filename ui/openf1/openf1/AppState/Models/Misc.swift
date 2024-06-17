import Foundation

typealias DriverNumber = String

extension String? {
    func orUnknown() -> String { self ?? "UNK" }
}

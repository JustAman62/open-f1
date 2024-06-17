import Foundation
import SwiftUI

extension TimingData.DriverData.Interval {
    var isInsideDrs: Bool { (Decimal(string: self.value.orUnknown()) ?? 1) < 1 }
}

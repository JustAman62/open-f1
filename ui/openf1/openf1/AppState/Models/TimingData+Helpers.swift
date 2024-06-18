import Foundation
import SwiftUI

extension TimingData.DriverData.Interval {
    var isInsideDrs: Bool { (Decimal(string: self.value.orUnknown()) ?? 1) < 1 }
}

extension TimingData.DriverData {
    var gapToLeaderSeconds: Decimal { Decimal(string: self.gapToLeader.orUnknown()) ?? 0 }
}

extension TimingData {
    var sorted: [(DriverNumber, TimingData.DriverData)] {
        self.lines.sorted(using: KeyPathComparator(\.value.line))
    }
}

extension Dictionary where Key : Comparable {
    var sorted: [(Key, Value)] { self.sorted(using: KeyPathComparator(\.key)) }
}

import SwiftUI

private enum DataType: String {
    case timingData, driverList
}

struct RawDataScreen: View {
    @Environment(\.appState) private var appState
    
    @State private var selectedData: DataType = .timingData
    
    private var json: String {
        let data = switch selectedData {
        case .timingData: \AppStateProtocol.timingData
        case .driverList: \AppStateProtocol.driverList
        }
        
        if let data = appState[keyPath: data] as? any Encodable {
            guard let encoded = try? JSONEncoder.pretty.encode(data) else {
                return "Unable to encode JSON"
            }
            return String(data: encoded, encoding: .utf8)
                ?? "Unable to understand encoded JSON"
        }
        return "Data not found"
    }
    
    var body: some View {
        VStack {
            Picker("Data Type", selection: $selectedData) {
                Text("Timing Data")
                    .tag(DataType.timingData)
                
                Text("Driver Data")
                    .tag(DataType.driverList)
            }
            .pickerStyle(.automatic)
            
            Divider()
            
            Text(selectedData.rawValue)
            
            TextEditor(text: .constant(json))
                .font(.body.monospaced())
        }
    }
}

#Preview {
    RawDataScreen()
        .previewEnvironment()
}

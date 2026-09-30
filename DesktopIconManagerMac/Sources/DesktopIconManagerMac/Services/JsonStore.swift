import Foundation

enum JsonStore {
    static let encoder: JSONEncoder = {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        encoder.dateEncodingStrategy = .iso8601
        return encoder
    }()

    static let decoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }()

    static func load<T: Decodable>(_ type: T.Type, from url: URL, default defaultValue: @autoclosure () -> T) -> T {
        do {
            let data = try Data(contentsOf: url)
            return try decoder.decode(type, from: data)
        } catch {
            return defaultValue()
        }
    }

    static func save<T: Encodable>(_ value: T, to url: URL) throws {
        let data = try encoder.encode(value)
        try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
        try data.write(to: url, options: [.atomic])
    }
}

# Packet System Usage Guide

## Overview

This packet system provides a robust, versioned, and type-safe framework for network communication in the MMO server. It supports both fixed and variable-length packets with automatic serialization/deserialization.

## Key Features

- ✅ **Type-Safe**: Strongly typed packet definitions with compile-time safety
- ✅ **Versioning**: Multiple packet versions can coexist with configuration-based selection
- ✅ **Immutable**: Packet properties use internal setters to prevent modification after creation
- ✅ **Little-Endian**: Consistent byte order for protocol compatibility
- ✅ **Automatic Discovery**: Reflection-based packet registration at startup
- ✅ **Validation**: Built-in size and data validation
- ✅ **Fixed & Variable Length**: Supports both packet types efficiently

## Quick Start

### 1. Initialize the Packet System

```csharp
using Core.Server.Packets;
using Microsoft.Extensions.Configuration;

// Create and initialize the packet system
var packetSystem = new PacketSystem();

// Option A: Initialize without configuration (uses defaults)
packetSystem.Initialize();

// Option B: Initialize with configuration (loads packet versions)
IConfiguration configuration = /* your configuration */;
packetSystem.Initialize(configuration);
```

### 2. Define a New Packet

#### Example: Fixed-Length Incoming Packet

```csharp
[PacketVersion(1)]
public class CZ_MY_PACKET : IncomingPacket
{
    public int PlayerId { get; internal set; }
    public byte Action { get; internal set; }
    
    public CZ_MY_PACKET() : base(PacketHeader.CZ_MY_PACKET, isFixedLength: true)
    {
    }
    
    public override void Read(BinaryReader reader)
    {
        PlayerId = reader.ReadInt32();
        Action = reader.ReadByte();
    }
    
    public override int GetSize() => 2 + 4 + 1; // header + int + byte = 7 bytes
}
```

#### Example: Variable-Length Outgoing Packet

```csharp
[PacketVersion(1)]
public class ZC_MY_PACKET : OutgoingPacket
{
    public string Message { get; init; } = string.Empty; // 256 bytes max
    public int[] PlayerIds { get; init; } = Array.Empty<int>();
    
    private const int MessageLength = 256;
    
    public ZC_MY_PACKET() : base(PacketHeader.ZC_MY_PACKET, isFixedLength: false)
    {
    }
    
    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);              // 2 bytes
        writer.Write((short)GetSize());           // 2 bytes
        writer.WriteFixedString(Message, MessageLength); // 256 bytes
        writer.Write((short)PlayerIds.Length);    // 2 bytes
        foreach (var id in PlayerIds)
        {
            writer.Write(id);                     // 4 bytes each
        }
    }
    
    public override int GetSize()
    {
        return 2 + 2 + MessageLength + 2 + (PlayerIds.Length * 4);
    }
}
```

### 3. Send and Receive Packets

#### Sending Packets (Server → Client)

```csharp
// Create packet
var packet = new AC_ACCEPT_LOGIN
{
    SessionToken = 12345678,
    CharacterSlots = 9
};

// Serialize to byte array
byte[] data = packetSystem.SerializePacket(packet);

// Or write directly to a stream
using var stream = /* your network stream */;
packetSystem.WritePacket(stream, packet);
```

#### Receiving Packets (Client → Server)

```csharp
using var stream = /* your network stream */;

// Read packet from stream (automatically determines type and deserializes)
IncomingPacket packet = packetSystem.ReadPacket(stream);

// Cast to specific type
if (packet is CA_LOGIN loginPacket)
{
    Console.WriteLine($"Login attempt: {loginPacket.Username}");
}
```

## Adding a New Packet: Step-by-Step Checklist

1. **Add enum entry** to `PacketHeader.cs`:
   ```csharp
   public enum PacketHeader : short
   {
       // ... existing entries
       CZ_MY_NEW_PACKET = 0x1234,
   }
   ```

2. **Create packet class** in appropriate folder:
   - `ClientPackets/` for incoming (CZ, CA, CH)
   - `ServerPackets/` for outgoing (ZC, AC, HC, SC)

3. **Add `[PacketVersion(1)]` attribute** to the class

4. **Define properties** with `internal set` for incoming, `init` for outgoing

5. **Implement `Read()` or `Write()` method**

6. **Implement `GetSize()` method**

7. **Add static `Create()` factory method** (for incoming packets):
   ```csharp
   public static CZ_MY_NEW_PACKET Create(BinaryReader reader)
   {
       var packet = new CZ_MY_NEW_PACKET();
       packet.Read(reader);
       return packet;
   }
   ```

8. **(Optional) Add to configuration** file if you need version control

9. **Restart the server** (packets are auto-registered at startup)

10. **Write unit tests** for the new packet

## Working with Strings

The packet system uses **fixed-length ASCII strings** (single-byte encoding):

```csharp
// Writing strings
writer.WriteFixedString("MyUsername", 24); // Pads with nulls if shorter, truncates if longer

// Reading strings
string username = reader.ReadFixedString(24); // Reads 24 bytes, stops at first null
```

### String Handling Rules

- **Encoding**: ASCII (single-byte per character)
- **Fixed length**: Always specify exact byte count
- **Null-terminated**: Strings are null-terminated or null-padded
- **Truncation**: Strings longer than max length are automatically truncated
- **Common lengths**:
  - Username: 24 bytes
  - Password: 24 bytes
  - Character name: 24 bytes
  - Chat message: 256 bytes
  - Item name: 50 bytes

## Working with Arrays

### Writing Arrays

```csharp
// Byte count prefix (for small arrays, max 255 elements)
writer.Write((byte)array.Length);
foreach (var item in array)
{
    // Write item
}

// Short count prefix (for medium arrays, max 32,767 elements)
writer.Write((short)array.Length);
foreach (var item in array)
{
    // Write item
}

// Using helper methods
writer.WriteByteArray(items, (w, item) => w.Write(item.Value));
writer.WriteShortArray(items, (w, item) => w.Write(item.Value));
```

### Reading Arrays

```csharp
// Read with byte count
byte count = reader.ReadByte();
var array = new MyStruct[count];
for (int i = 0; i < count; i++)
{
    array[i] = MyStruct.Read(reader);
}

// Using helper methods
var items = reader.ReadByteArray(r => r.ReadInt32());
var items = reader.ReadShortArray(r => MyStruct.Read(r));
```

## Packet Versioning

### Creating a New Version

When you need to modify an existing packet:

1. **Create new class** with incremented version:
   ```csharp
   [PacketVersion(2)]
   public class CA_LOGIN_V2 : IncomingPacket
   {
       // New fields or modified structure
   }
   ```

2. **Update configuration** to use new version:
   ```json
   {
     "PacketVersions": {
       "CA_LOGIN": 2
     }
   }
   ```

3. **Keep old version** for backward compatibility if needed

### Version Selection

- **Automatic**: Without configuration, uses highest registered version
- **Configured**: Loads active versions from `appsettings.json`
- **Per-packet**: Each packet header can have different active version

## Validation

### Size Validation

```csharp
// Automatic validation in Write()
PacketValidator.ValidateSize(packet.GetSize());

// Limits
PacketValidator.MinPacketSize; // 2 bytes (header only)
PacketValidator.MaxPacketSize; // 32,768 bytes (32KB)
```

### String Validation

```csharp
PacketValidator.ValidateStringLength(username, 24, nameof(username));
```

### Array Validation

```csharp
PacketValidator.ValidateArrayCount(entities.Length, EntityInfo.Size, nameof(entities));
```

## Configuration

Add to your `appsettings.json`:

```json
{
  "PacketVersions": {
    "CA_LOGIN": 1,
    "CZ_HEARTBEAT": 1,
    "HC_CHARACTER_LIST": 1,
    "ZC_ENTITY_LIST": 1
  }
}
```

## Best Practices

### DO ✅

- Use `internal set` for incoming packet properties
- Use `init` for outgoing packet properties
- Define field size constants (e.g., `UsernameLength = 24`)
- Document byte layout in comments
- Validate sizes in `GetSize()` and `Write()`
- Use structs for repeated nested data
- Write comprehensive unit tests
- Keep packet classes immutable after creation

### DON'T ❌

- Don't use `public set` (breaks immutability)
- Don't hardcode magic numbers (use constants)
- Don't skip size validation
- Don't assume string encoding (always use ASCII)
- Don't create packets larger than 32KB
- Don't modify packet data after creation
- Don't skip unit tests for new packets

## Naming Conventions

### Packet Direction Prefixes

| Prefix | Direction | Description |
|--------|-----------|-------------|
| **CA** | Client → Auth | Client to Auth server |
| **AC** | Auth → Client | Auth server to Client |
| **CH** | Client → Char | Client to Char server |
| **HC** | Char → Client | Char server to Client |
| **CZ** | Client → Zone | Client to Zone/Map server |
| **ZC** | Zone → Client | Zone/Map server to Client |
| **SC** | Server → Client | Generic server to Client |

### Packet Naming

- Use ALL_CAPS with underscores
- Start with direction prefix
- Use descriptive action names
- Examples:
  - `CA_LOGIN` - Client requests login
  - `AC_ACCEPT_LOGIN` - Auth server accepts login
  - `ZC_ENTITY_LIST` - Zone server sends entity list

## Debugging Tips

### Hex Dump Packets

```csharp
byte[] data = packetSystem.SerializePacket(packet);
Console.WriteLine($"Packet {packet.Header}:");
Console.WriteLine(BitConverter.ToString(data).Replace("-", " "));
```

### Verify Endianness

```csharp
int value = 0x12345678;
byte[] bytes = BitConverter.GetBytes(value);
// Should be: 78 56 34 12 (little-endian)
```

### Check Size Calculations

```csharp
var packet = /* create packet */;
byte[] serialized = packetSystem.SerializePacket(packet);
Assert.Equal(packet.GetSize(), serialized.Length);
```

## Performance Considerations

- **No Pooling**: Packets are not pooled; create new instances as needed
- **No Chaining**: TCP stream reassembly handles fragmentation
- **Zero-Copy**: Use streams directly when possible
- **Reflection Once**: Packet discovery happens once at startup
- **Thread-Safe**: Factory and registry are thread-safe

## Testing

Example unit test structure:

```csharp
[Fact]
public void MyPacket_Serialization_RoundTrip()
{
    // Arrange
    var original = new ZC_MY_PACKET
    {
        Field1 = 123,
        Field2 = "test"
    };
    
    // Act - Serialize
    byte[] data;
    using (var ms = new MemoryStream())
    using (var writer = new BinaryWriter(ms))
    {
        original.Write(writer);
        data = ms.ToArray();
    }
    
    // Assert - Size
    Assert.Equal(original.GetSize(), data.Length);
    
    // Act - Deserialize
    using (var ms = new MemoryStream(data))
    using (var reader = new BinaryReader(ms))
    {
        short header = reader.ReadInt16();
        short size = reader.ReadInt16();
        // Read fields...
    }
    
    // Assert - Values match
}
```

## Troubleshooting

### Problem: "Packet not registered"

**Solution**: Ensure packet class has `[PacketVersion]` attribute and parameterless constructor.

### Problem: "Property cannot be assigned"

**Solution**: Use `internal set` for incoming packets and add `[assembly: InternalsVisibleTo("YourTestProject")]`.

### Problem: "Size mismatch"

**Solution**: Verify `GetSize()` matches actual bytes written in `Write()`.

### Problem: "String truncation"

**Solution**: Check string length doesn't exceed defined maximum (use validation).

### Problem: "Wrong endianness"

**Solution**: Use `BinaryReader`/`BinaryWriter` which handle little-endian automatically.

## Reference

### Available Extension Methods

#### BinaryReader
- `ReadFixedString(int length)` - Read fixed-length ASCII string
- `ReadByteArray<T>(Func<BinaryReader, T>)` - Read array with byte count
- `ReadShortArray<T>(Func<BinaryReader, T>)` - Read array with short count
- `ReadArray<T>(Func<BinaryReader, T>)` - Read array with int count

#### BinaryWriter
- `WriteFixedString(string, int length)` - Write fixed-length ASCII string
- `WriteByteArray<T>(T[], Action<BinaryWriter, T>)` - Write array with byte count
- `WriteShortArray<T>(T[], Action<BinaryWriter, T>)` - Write array with short count
- `WriteArray<T>(T[], Action<BinaryWriter, T>)` - Write array with int count

### Packet Lifecycle

1. **Registration** (at startup)
   - Scan assemblies
   - Find classes with `[PacketVersion]`
   - Register in factory and size registry

2. **Receiving** (incoming)
   - Read header (2 bytes)
   - Check fixed/variable length
   - Read size if variable (2 bytes)
   - Create packet instance via factory
   - Call `Read(reader)` to deserialize body

3. **Sending** (outgoing)
   - Create packet with object initializer
   - Call `Write(writer)` to serialize
   - Packet writes header, size (if variable), and body

## Example: Complete Packet Implementation

```csharp
// 1. Add to PacketHeader enum
public enum PacketHeader : short
{
    ZC_PLAYER_STATUS = 0x1234,
}

// 2. Create packet class
[PacketVersion(1)]
public class ZC_PLAYER_STATUS : OutgoingPacket
{
    // 3. Define properties
    public int PlayerId { get; init; }
    public short HP { get; init; }
    public short MaxHP { get; init; }
    public short SP { get; init; }
    public short MaxSP { get; init; }
    public string Name { get; init; } = string.Empty;
    
    private const int NameLength = 24;
    
    // 4. Constructor
    public ZC_PLAYER_STATUS() 
        : base(PacketHeader.ZC_PLAYER_STATUS, isFixedLength: false)
    {
    }
    
    // 5. Write method
    public override void Write(BinaryWriter writer)
    {
        int size = GetSize();
        PacketValidator.ValidateSize(size);
        PacketValidator.ValidateStringLength(Name, NameLength, nameof(Name));
        
        writer.Write((short)Header);        // 2 bytes
        writer.Write((short)size);          // 2 bytes
        writer.Write(PlayerId);             // 4 bytes
        writer.Write(HP);                   // 2 bytes
        writer.Write(MaxHP);                // 2 bytes
        writer.Write(SP);                   // 2 bytes
        writer.Write(MaxSP);                // 2 bytes
        writer.WriteFixedString(Name, NameLength); // 24 bytes
    }
    
    // 6. GetSize method
    public override int GetSize()
    {
        return 2 + 2 + 4 + 2 + 2 + 2 + 2 + NameLength; // 40 bytes
    }
}

// 7. Usage
var packet = new ZC_PLAYER_STATUS
{
    PlayerId = 1001,
    HP = 1500,
    MaxHP = 2000,
    SP = 300,
    MaxSP = 500,
    Name = "HeroPlayer"
};

byte[] data = packetSystem.SerializePacket(packet);
// Send data over network
```

## Support

For questions or issues:
- Check unit tests for examples
- Review existing packet implementations
- Ensure all attributes and methods are properly implemented
- Verify packet size calculations match serialized data


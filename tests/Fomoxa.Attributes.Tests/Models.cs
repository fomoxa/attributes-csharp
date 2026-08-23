using Fomoxa;

namespace Fomoxa.Attributes.Tests;

// The models the metadata tests read. They carry annotations and nothing else:
// no base class, no interface, no method. What they are is entirely what the
// attributes say.

/// <summary>The model from the specification's own example.</summary>
[Network]
[Codec("edge", "unity")]
public class DeviceState
{
    /// <summary>In both codecs.</summary>
    [Network("u32")]
    [Codec("edge", "unity")]
    public uint Id { get; set; }

    /// <summary>Edge only.</summary>
    [Network("f32")]
    [Codec("edge")]
    public float Temperature { get; set; }

    /// <summary>Unity only.</summary>
    [Network("string")]
    [Codec("unity")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>A network field in no codec.</summary>
    [Network("u32")]
    public uint Unrouted { get; set; }

    /// <summary>Not a network field at all.</summary>
    public string Cache { get; set; } = string.Empty;
}

/// <summary>
/// The case §16 asks for by name: the C# type is <c>ulong</c>, the wire type is
/// <c>u32</c>, and the wire type is the one that counts.
/// </summary>
[Network]
[Codec("edge")]
public class NativeTypeIsIrrelevant
{
    /// <summary>Declared <c>ulong</c>, encoded as <c>u32</c>.</summary>
    [Network("u32")]
    [Codec("edge")]
    public ulong Value { get; set; }

    /// <summary>Declared <c>uint</c>, encoded as <c>u32</c> — the same wire type.</summary>
    [Network("u32")]
    [Codec("edge")]
    public uint SameWireType { get; set; }

    /// <summary>Declared <c>double</c>, encoded as <c>f32</c>.</summary>
    [Network("f32")]
    [Codec("edge")]
    public double NarrowerOnTheWire { get; set; }

    /// <summary>Declared <c>int</c>, encoded as <c>u8</c>.</summary>
    [Network("u8")]
    [Codec("edge")]
    public int MuchNarrowerOnTheWire { get; set; }
}

/// <summary>Codec names the package has never heard of.</summary>
[Network]
[Codec("orange_pi", "database", "custom_protocol", "godot")]
public class UserNamedCodecs
{
    /// <summary>In every one of them.</summary>
    [Network("u64")]
    [Codec("orange_pi", "database", "custom_protocol", "godot")]
    public ulong Sequence { get; set; }
}

/// <summary>
/// The repeated form. <c>[Codec("edge")] [Codec("unity")]</c> says what
/// <c>[Codec("edge", "unity")]</c> says.
/// </summary>
[Network]
[Codec("edge")]
[Codec("unity")]
public class RepeatedCodecAttributes
{
    /// <summary>Also declared the repeated way.</summary>
    [Network("u32")]
    [Codec("edge")]
    [Codec("unity")]
    public uint Id { get; set; }
}

/// <summary>Fields work exactly like properties.</summary>
[Network]
[Codec("edge")]
public class WithFields
{
    /// <summary>A network field, declared as a field.</summary>
    [Network("u32")]
    [Codec("edge")]
    public uint Id;

    /// <summary>Another one.</summary>
    [Network("string")]
    [Codec("edge")]
    public string Name = string.Empty;
}

/// <summary>A model may be a struct.</summary>
[Network]
[Codec("edge")]
public struct Point
{
    /// <summary>The x coordinate.</summary>
    [Network("i32")]
    [Codec("edge")]
    public int X { get; set; }

    /// <summary>The y coordinate.</summary>
    [Network("i32")]
    [Codec("edge")]
    public int Y { get; set; }
}

/// <summary>A model that declares no codec.</summary>
[Network]
public class NoCodecs
{
    /// <summary>A network field.</summary>
    [Network("u32")]
    public uint Id { get; set; }
}

/// <summary>Nothing marks this, so it is not a model.</summary>
public class NotAModel
{
    /// <summary>An ordinary property.</summary>
    public uint Whatever { get; set; }
}

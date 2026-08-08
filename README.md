# cyclone-attributes (C#)

The official **Cyclone attributes** for C#: `[Network]` and `[Codec]`.

```
cyclone-attributes  →  your source  →  cyclonec  →  generated code
```

Two attributes, in the `Cyclone` namespace, and nothing else. This package is the
syntax half of Cyclone — the part that makes annotated C# carry the metadata
`cyclonec` reads.

## The one rule

> **Cyclone wire types are defined by the Cyclone Specification, not by the host
> programming language.**
>
> **Native C# types do not determine the Cyclone byte representation.**

```csharp
[Network("u32")]
public uint Id { get; set; }
```

| | |
|---|---|
| `u32` | the **Cyclone wire type** — 4 bytes, Little Endian, per the Specification |
| `uint` | the **C# representation** — how your program happens to store it |

The wire format always comes from the Specification. Change the C# type and
nothing on the wire moves:

```csharp
[Network("u32")] public uint  Id { get; set; }   // 4 bytes LE
[Network("u32")] public ulong Id { get; set; }   // still 4 bytes LE
```

Both are `u32`, because `u32` says so. Whether a `ulong` is a sensible place to
keep one is a question for the code generator and the C# compiler — it is not a
question about the wire, and this package never asks it.

## Usage

```csharp
using Cyclone;

[Network]
[Codec("edge", "unity")]
public class DeviceState
{
    [Network("u32")]
    [Codec("edge", "unity")]
    public uint Id { get; set; }

    [Network("f32")]
    [Codec("edge")]
    public float Temperature { get; set; }

    [Network("string")]
    [Codec("unity")]
    public string DisplayName { get; set; } = string.Empty;
}
```

| Where | Attribute | Says |
|-------|-----------|------|
| model | `[Network]` | this type is a Cyclone network model |
| model | `[Codec("edge", "unity")]` | generate these codecs for it |
| field | `[Network("u32")]` | this field's Cyclone wire type |
| field | `[Codec("edge")]` | which of the model's codecs this field appears in |

From the model above, `cyclonec` knows to generate:

```
DeviceStateEdgeCodec    →  Id, Temperature
DeviceStateUnityCodec   →  Id, DisplayName
```

Fields and properties both work, and a model may be a `class` or a `struct`.

### Codec names are yours

`edge`, `unity`, `orange_pi`, `database`, `custom_protocol` — this package has
never heard of any of them, holds no list of them, and gives none of them
meaning. They are identifiers you invent; `cyclonec` uses them to spell the type
it generates. The repeated form and the list form are the same thing:

```csharp
[Codec("edge", "unity")]      // ==  [Codec("edge")]
                              //     [Codec("unity")]
```

Order is preserved.

## Why a string, and not `typeof`

```csharp
[Network(typeof(uint))]     // ← not this
[Network("u32")]            // ← this
```

`typeof(uint)` would make the host language the source of truth, and it is not.
Cyclone defines `u32` as four bytes Little Endian for *every* language; C#
defines `uint` as whatever it likes. Deriving one from the other is how an
implementation ends up unable to decode what another implementation wrote:

```
Cyclone Specification          C# Type
        │ defines                 │ guess
        ▼                         ▼
   Wire Type                 Wire Type          ← wrong
        │                         │ guess
        ▼                         ▼
  Byte Layout                Byte Layout        ← wrong, and silently
        │
   ┌────┼────┐
   ▼    ▼    ▼
 Rust   C#   Go
   └── same bytes ──┘
```

So the string is stored **exactly as written** and is never interpreted here.
There is no list of valid wire types in this assembly, no mapping from C# types,
and no opinion about byte layout. `cyclonec` reads the string and resolves it
against the Specification.

## What this package does not contain

Everything else.

- **No runtime.** No `Writer`, no `Reader`, no buffer.
- **No encode, no decode.** No serialization of any kind.
- **No byte layout.** No endian conversion, no IEEE conversion, no string
  encoding, no array encoding, no length prefixes.
- **No type mapping.** Nothing anywhere says `typeof(uint) → "u32"`.
- **No schema, no IR, no compiler, no registry, no reflection system.**
- **No dependencies.**

The public surface is two types, both attributes, and there is a test that fails
if that ever stops being true.

## A note on wire type spelling

RFC-0001 §1 lists the scalars lowercase — `bool`, `i8`, `u32`, `f32` — and the
composites capitalised: `String`, `Bytes`, `Array<T>`, `Struct`, `Enum`. The
examples in this package's brief use lowercase `"string"`.

**This package does not resolve that.** It stores what you write, byte for byte,
and hands it back unchanged. Which spelling is canonical is a question for the
Specification and for `cyclonec`, and inventing an answer here — a validator, an
alias table, a normaliser — would be inventing a wire format. `TODO`: settle the
spelling in the Specification, then `cyclonec` enforces it.

## Layout

```
cyclone-attributes-csharp/
├── Cyclone.Attributes.csproj
├── src/
│   ├── NetworkAttribute.cs
│   └── CodecAttribute.cs
├── tests/
└── README.md
```

There is no `NetworkFieldAttribute.cs`. In C#, `[Network("u32")]` resolves to a
class named `NetworkAttribute`, so the model marker and the field wire type must
be the same class with two constructors — a second class would have to be spelled
`[NetworkField("u32")]`, which is not the syntax.

## Tests

Metadata only, because metadata is all there is. Model detection, wire type
extraction, model-level and field-level codecs, multiple codecs and their order,
and the case the brief names explicitly:

```csharp
[Network("u32")]
public ulong Value { get; set; }   // wire type is still "u32"
```

Plus two tests that guard the scope: the assembly exports exactly
`Cyclone.NetworkAttribute` and `Cyclone.CodecAttribute`, and every exported type
is an attribute.

```
dotnet test tests/Cyclone.Attributes.Tests
```

## Build

```
dotnet build
dotnet pack -c Release
```

Targets `netstandard2.0`, so it can be referenced from .NET Framework, .NET 8,
Unity and Godot alike.

## License

Apache-2.0

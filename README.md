# Fomoxa.Attributes (C#)

## 1. Overview

Fomoxa.Attributes defines two attribute types, `NetworkAttribute` and `CodecAttribute`, in the `Fomoxa` namespace.
They are the only public types in the assembly.
The package has no runtime behavior beyond ordinary .NET attribute semantics.
It contains no encoder, decoder, `Reader` or `Writer`, no byte-layout logic, and no reflection-based dispatch of its own.

The attributes exist so that `fomoxac`, the Fomoxa code generator, has source text to read.
`fomoxac` is a separate tool and is not part of this repository.
A class annotated with `[Network]` and `[Codec(...)]` compiles and runs unchanged whether or not `fomoxac` has processed it; the attributes are inert markers for a build step, and this library does not interpret them at run time.

```
fomoxa-attributes  ->  your source  ->  fomoxac  ->  generated code
```

## 2. Installation

The package is published on NuGet as `Fomoxa.Attributes`, currently at version `0.1.0`.

```sh
dotnet add package Fomoxa.Attributes
```

## 3. Wire type as a string

`[Network("u32")]`, applied to a field or property, declares that member's Fomoxa wire type as a string, stored exactly as written.
The string is never validated, parsed, or compared against the C# type of the member it sits on:

```csharp
[Network("u32")] public uint  Id { get; set; }   // wire type: u32
[Network("u32")] public ulong Id { get; set; }   // wire type: u32, unchanged
```

The constructor does not accept `typeof(uint)`, because that would make the C# type decide the wire representation.
The Fomoxa Specification defines a member's wire type independently of any host language, while a C# type such as `uint` or `ulong` is a storage decision local to one codebase.
If two implementations derived wire types from host-language types, they could infer different wire representations for what they believe is the same field, and then fail to decode each other's output.
A literal string avoids that inference: this assembly holds no list of valid wire types, no mapping from C# types to wire types, and no rules about byte layout.
Resolving the string against the Specification is left entirely to `fomoxac`.

## 4. Attributes

### 4.1 `NetworkAttribute`

```csharp
namespace Fomoxa;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class NetworkAttribute : Attribute
{
    public NetworkAttribute();
    public NetworkAttribute(string wireType);
    public string? WireType { get; }
}
```

`NetworkAttribute()`, with no argument, goes on a `class` or `struct` and marks it as a Fomoxa network model.
In this form `WireType` is `null`, since a model has no wire type of its own; only its fields do.
`NetworkAttribute(string wireType)` goes on a field or property and declares that member's Fomoxa wire type; `WireType` returns the string exactly as passed to the constructor.

### 4.2 `CodecAttribute`

```csharp
namespace Fomoxa;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = true,
    Inherited = false)]
public sealed class CodecAttribute : Attribute
{
    public CodecAttribute(params string[] names);
    public IReadOnlyList<string> Names { get; }
}
```

On a `class` or `struct`, `[Codec("edge", "unity")]` declares which codecs `fomoxac` generates for that model.
On a field or property, it declares which of the model's codecs the member belongs to.
`Names` returns the codec identifiers in the order they were passed to the constructor, and the constructor throws `ArgumentNullException` if `names` is `null`.

A codec name is an arbitrary identifier: this assembly holds no list of recognized codec names and gives none of them a meaning.
`AllowMultiple` is `true`, so the attribute may be repeated. The repeated form and the list form are equivalent and keep the same order:

```csharp
[Codec("edge")]
[Codec("unity")]
// is equivalent to:
[Codec("edge", "unity")]
```

## 5. Quick start

```csharp
using Fomoxa;

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

| Where | Attribute | Meaning |
|---|---|---|
| type | `[Network]` | this type is a Fomoxa network model |
| type | `[Codec("edge", "unity")]` | `fomoxac` generates codecs named `edge` and `unity` for this model |
| member | `[Network("u32")]` | this member's Fomoxa wire type is `u32` |
| member | `[Codec("edge")]` | this member is included in the `edge` codec |

From this model, `fomoxac` is expected to generate one codec type per name, each covering only the members that carry that codec: for example `DeviceStateEdgeCodec` with `Id` and `Temperature`, and `DeviceStateUnityCodec` with `Id` and `DisplayName`.
Both fields and properties accept the attributes, and a model may be a `class` or a `struct`.

## 6. Scope

The assembly exports exactly two types, `Fomoxa.NetworkAttribute` and `Fomoxa.CodecAttribute`, both attributes; the test suite asserts this directly (Section 9).
The package contains:

- No runtime: no `Writer`, no `Reader`, no buffer type.
- No encode or decode logic, and no serialization of any kind.
- No byte-layout logic: nothing in the assembly determines endianness, numeric encoding, string encoding, or length prefixes.
- No mapping from C# types to Fomoxa wire types.
- No schema representation, intermediate representation, compiler, or registry.
- No dependency that reaches a consumer at run time (see Section 7).

## 7. Package metadata

| Property | Value |
|---|---|
| `PackageId` | `Fomoxa.Attributes` |
| `Version` | `0.1.0` |
| `TargetFramework` | `netstandard2.0` |
| `PackageLicenseExpression` | `Apache-2.0` |
| `RepositoryUrl` | `https://github.com/fomoxa/attributes-csharp` |

The library targets `netstandard2.0`, so one build can be referenced from .NET Framework, .NET 8, Unity, and Godot.
`GenerateDocumentationFile` and `TreatWarningsAsErrors` are both enabled, and the build is `Deterministic`.
The project sets `EnableDefaultCompileItems` to `false` and compiles only `src/**/*.cs`; `tests/` is a separate project and is not part of the package.
The only `PackageReference` is `Microsoft.SourceLink.GitHub`, with `PrivateAssets="All"`: it takes part in the build, embedding source-link metadata, and does not become a dependency of consumers.

```sh
dotnet build
dotnet pack -c Release
```

## 8. Layout

```
attributes-csharp/
├── Fomoxa.Attributes.csproj
├── src/
│   ├── NetworkAttribute.cs
│   └── CodecAttribute.cs
├── tests/
│   └── Fomoxa.Attributes.Tests/
│       ├── Fomoxa.Attributes.Tests.csproj
│       ├── MetadataTests.cs
│       └── Models.cs
└── README.md
```

`NetworkAttribute` is both the model marker and the field-level wire-type declaration, because `[Network("u32")]` on a field and `[Network]` on a type resolve to the same class name in C#.
A second class would need a different attribute name, such as `[NetworkField("u32")]`, and this package does not use that syntax.

## 9. Tests

`tests/Fomoxa.Attributes.Tests` is an xUnit project (`xunit` 2.9.2, `Microsoft.NET.Test.Sdk` 17.11.1) that targets `net8.0` and references `Fomoxa.Attributes.csproj` directly.
`Models.cs` defines annotated classes and one struct as fixtures, and `MetadataTests.cs` reads their attributes back with `System.Reflection`. The tests cover:

- Model detection: a type carrying `[Network]` is detected as a model, and `WireType` is `null` on that model-level attribute.
- Wire type extraction: the string passed to `[Network(...)]` is read back unchanged, on both properties and fields, and an unannotated member has no `NetworkAttribute`.
- Independence from the C# type: a member declared `ulong` but annotated `[Network("u32")]` reports a wire type of `u32`; two members with different C# types (`ulong` and `uint`) can share a wire type; and a wire type may be narrower than the C# type it sits on (for example `f32` on a `double`, or `u8` on an `int`).
- Verbatim storage of arbitrary wire-type strings, including ones this package gives no meaning.
- Codec extraction at the model level and the member level, including members with no codec declared.
- Several codec names, passed to one `[Codec(...)]` or spread across repeated `[Codec(...)]` attributes, read back in the order written, with the repeated and list forms giving identical results.
- Codec names treated as arbitrary identifiers.
- `CodecAttribute`'s constructor throwing `ArgumentNullException` when `names` is `null`.
- The assembly's exported types being exactly `Fomoxa.NetworkAttribute` and `Fomoxa.CodecAttribute`, each deriving from `Attribute`.

```sh
dotnet test tests/Fomoxa.Attributes.Tests
```

## 10. Continuous integration and release

`.github/workflows/publish.yml` runs when a tag matching `v*.*.*` is pushed, and can also be triggered manually.
The job runs on `ubuntu-latest` with the .NET 8 SDK and performs, in order: `dotnet test tests/Fomoxa.Attributes.Tests -c Release`; `dotnet pack Fomoxa.Attributes.csproj -c Release -o artifacts`; an OIDC-based NuGet login (`NuGet/login@v1`, which uses the `id-token: write` permission to exchange a short-lived GitHub OIDC token for a temporary nuget.org API key valid for one hour); and `dotnet nuget push` of the package with `--skip-duplicate`.
No long-lived publishing credential is stored in the repository.

## 11. License

Apache-2.0. See [LICENSE](LICENSE).

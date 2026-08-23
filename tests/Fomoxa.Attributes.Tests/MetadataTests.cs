using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fomoxa;
using Xunit;

namespace Fomoxa.Attributes.Tests;

/// <summary>
/// What the attributes record, read back the way <c>fomoxac</c> reads it.
/// </summary>
/// <remarks>
/// Every test here is about metadata. There is nothing to encode with, and that
/// is the point: this package carries no <c>Writer</c>, no <c>Reader</c> and no
/// byte layout, so there is no serialization to test.
/// </remarks>
public sealed class MetadataTests
{
    // ============================================== §16 — model detection

    [Fact]
    public void NetworkModelsAreDetected()
    {
        Assert.NotNull(typeof(DeviceState).GetCustomAttribute<NetworkAttribute>());
        Assert.NotNull(typeof(Point).GetCustomAttribute<NetworkAttribute>());
        Assert.Null(typeof(NotAModel).GetCustomAttribute<NetworkAttribute>());
    }

    /// <summary>
    /// A model is marked, not typed: <c>[Network]</c> on a type carries no wire
    /// type, because a model is not one.
    /// </summary>
    [Fact]
    public void AModelCarriesNoWireType()
    {
        var marker = typeof(DeviceState).GetCustomAttribute<NetworkAttribute>();

        Assert.NotNull(marker);
        Assert.Null(marker!.WireType);
    }

    // ========================================= §16 — wire type extraction

    [Fact]
    public void WireTypesAreReadBackExactlyAsWritten()
    {
        Assert.Equal("u32", WireTypeOf<DeviceState>(nameof(DeviceState.Id)));
        Assert.Equal("f32", WireTypeOf<DeviceState>(nameof(DeviceState.Temperature)));
        Assert.Equal("string", WireTypeOf<DeviceState>(nameof(DeviceState.DisplayName)));
        Assert.Equal("i32", WireTypeOf<Point>(nameof(Point.X)));
        Assert.Equal("u64", WireTypeOf<UserNamedCodecs>(nameof(UserNamedCodecs.Sequence)));
    }

    /// <summary>A member with no <c>[Network]</c> is not a network field.</summary>
    [Fact]
    public void UnannotatedMembersHaveNoWireType()
    {
        Assert.Null(
            typeof(DeviceState)
                .GetProperty(nameof(DeviceState.Cache))!
                .GetCustomAttribute<NetworkAttribute>());
    }

    // ================== §16 — the native C# type does not change anything

    /// <summary>
    /// <b>The test §16 asks for by name.</b>
    /// </summary>
    /// <remarks>
    /// <c>[Network("u32")] public ulong Value</c> is a <c>u32</c> on the wire —
    /// four bytes, Little Endian — because <c>u32</c> says so. The <c>ulong</c>
    /// is a C# storage decision and reaches no byte.
    /// </remarks>
    [Fact]
    public void ANativeUlongDoesNotWidenAUInt32WireType()
    {
        var property = typeof(NativeTypeIsIrrelevant)
            .GetProperty(nameof(NativeTypeIsIrrelevant.Value))!;

        // The C# type really is ulong…
        Assert.Equal(typeof(ulong), property.PropertyType);

        // …and the Fomoxa wire type really is u32, unchanged by it.
        Assert.Equal("u32", property.GetCustomAttribute<NetworkAttribute>()!.WireType);
    }

    /// <summary>
    /// Two different C# types, one wire type. If the native type were consulted,
    /// these would disagree — and two implementations of Fomoxa would stop
    /// being able to read each other.
    /// </summary>
    [Fact]
    public void DifferentNativeTypesCanShareOneWireType()
    {
        string? asUlong = WireTypeOf<NativeTypeIsIrrelevant>(nameof(NativeTypeIsIrrelevant.Value));
        string? asUint =
            WireTypeOf<NativeTypeIsIrrelevant>(nameof(NativeTypeIsIrrelevant.SameWireType));

        Assert.Equal("u32", asUlong);
        Assert.Equal("u32", asUint);
        Assert.Equal(asUlong, asUint);

        Assert.NotEqual(
            typeof(NativeTypeIsIrrelevant).GetProperty(nameof(NativeTypeIsIrrelevant.Value))!
                .PropertyType,
            typeof(NativeTypeIsIrrelevant).GetProperty(nameof(NativeTypeIsIrrelevant.SameWireType))!
                .PropertyType);
    }

    /// <summary>
    /// The wire type may be narrower than the C# type, and the metadata says so
    /// without complaint. Whether the value fits is the code generator's and the
    /// C# compiler's question, not this package's.
    /// </summary>
    [Theory]
    [InlineData(nameof(NativeTypeIsIrrelevant.NarrowerOnTheWire), "f32", typeof(double))]
    [InlineData(nameof(NativeTypeIsIrrelevant.MuchNarrowerOnTheWire), "u8", typeof(int))]
    public void AWireTypeMayBeNarrowerThanItsNativeType(
        string member,
        string expectedWireType,
        Type expectedNativeType)
    {
        var property = typeof(NativeTypeIsIrrelevant).GetProperty(member)!;

        Assert.Equal(expectedNativeType, property.PropertyType);
        Assert.Equal(expectedWireType, property.GetCustomAttribute<NetworkAttribute>()!.WireType);
    }

    /// <summary>
    /// The wire type is a string the package never interprets. A name it has
    /// never seen is stored and handed back unchanged — resolving it against the
    /// Specification is <c>fomoxac</c>'s job.
    /// </summary>
    [Theory]
    [InlineData("u32")]
    [InlineData("String")]
    [InlineData("Array<u32>")]
    [InlineData("PlayerInfo")]
    [InlineData("something_this_package_has_never_heard_of")]
    public void AWireTypeIsStoredVerbatim(string wireType)
    {
        Assert.Equal(wireType, new NetworkAttribute(wireType).WireType);
    }

    // ================================= §16 — model-level codec extraction

    [Fact]
    public void ModelLevelCodecsAreExtracted()
    {
        Assert.Equal(new[] { "edge", "unity" }, CodecsOn(typeof(DeviceState)));
        Assert.Equal(new[] { "edge" }, CodecsOn(typeof(Point)));
        Assert.Empty(CodecsOn(typeof(NoCodecs)));
    }

    // ================================= §16 — field-level codec extraction

    [Fact]
    public void FieldLevelCodecsAreExtracted()
    {
        Assert.Equal(new[] { "edge", "unity" }, CodecsOnMember<DeviceState>(nameof(DeviceState.Id)));
        Assert.Equal(new[] { "edge" }, CodecsOnMember<DeviceState>(nameof(DeviceState.Temperature)));
        Assert.Equal(
            new[] { "unity" },
            CodecsOnMember<DeviceState>(nameof(DeviceState.DisplayName)));
    }

    /// <summary>A network field may belong to no codec.</summary>
    [Fact]
    public void AFieldMayNameNoCodec()
    {
        Assert.Empty(CodecsOnMember<DeviceState>(nameof(DeviceState.Unrouted)));
        Assert.Equal("u32", WireTypeOf<DeviceState>(nameof(DeviceState.Unrouted)));
    }

    // ==================================== §16 — multiple codec extraction

    [Fact]
    public void MultipleCodecsKeepTheOrderTheyWereWritten()
    {
        Assert.Equal(
            new[] { "orange_pi", "database", "custom_protocol", "godot" },
            CodecsOn(typeof(UserNamedCodecs)));

        Assert.Equal(
            new[] { "orange_pi", "database", "custom_protocol", "godot" },
            CodecsOnMember<UserNamedCodecs>(nameof(UserNamedCodecs.Sequence)));
    }

    /// <summary>
    /// §7 — the repeated form and the list form say the same thing, in the same
    /// order.
    /// </summary>
    [Fact]
    public void RepeatedAndListedCodecsAgree()
    {
        Assert.Equal(new[] { "edge", "unity" }, CodecsOn(typeof(RepeatedCodecAttributes)));
        Assert.Equal(
            CodecsOn(typeof(DeviceState)),
            CodecsOn(typeof(RepeatedCodecAttributes)));
    }

    /// <summary>
    /// A codec name is an identifier. The package holds no list of them and
    /// gives none of them meaning, so one invented here reads back the same as
    /// one from the specification's examples.
    /// </summary>
    [Fact]
    public void CodecNamesAreArbitraryIdentifiers()
    {
        var attribute = new CodecAttribute("edge", "a_name_nobody_has_used", "V2", "_internal");

        Assert.Equal(
            new[] { "edge", "a_name_nobody_has_used", "V2", "_internal" },
            attribute.Names);
    }

    // ================================================= fields, not just properties

    [Fact]
    public void FieldsCarryTheSameMetadataAsProperties()
    {
        var field = typeof(WithFields).GetField(nameof(WithFields.Id))!;

        Assert.Equal("u32", field.GetCustomAttribute<NetworkAttribute>()!.WireType);
        Assert.Equal(
            new[] { "edge" },
            field.GetCustomAttributes<CodecAttribute>().SelectMany(codec => codec.Names));
    }

    // ============================================================== scope

    /// <summary>
    /// §14 — the package is two attributes. Nothing else is public, because
    /// anything else would be a runtime, a schema or a registry by another name.
    /// </summary>
    [Fact]
    public void ThePublicSurfaceIsTwoAttributes()
    {
        string[] exported = typeof(NetworkAttribute).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Fomoxa.CodecAttribute", "Fomoxa.NetworkAttribute" }, exported);
    }

    /// <summary>
    /// §9 — no encode, no decode, no reader, no writer, no buffer. Not under
    /// those names, and not under any other: the assembly has two types and they
    /// are both attributes.
    /// </summary>
    [Fact]
    public void ThereIsNothingToSerializeWith()
    {
        Assembly assembly = typeof(NetworkAttribute).Assembly;

        Assert.All(
            assembly.GetExportedTypes(),
            type => Assert.True(
                typeof(Attribute).IsAssignableFrom(type),
                $"{type.FullName} is not an attribute"));

        Assert.DoesNotContain(
            assembly.GetReferencedAssemblies(),
            reference => reference.Name?.StartsWith("Fomoxa", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void CodecNamesCannotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CodecAttribute(null!));
    }

    // ============================================================ helpers

    private static string? WireTypeOf<T>(string member) =>
        MemberOf<T>(member).GetCustomAttribute<NetworkAttribute>()?.WireType;

    private static IEnumerable<string> CodecsOn(Type type) =>
        type.GetCustomAttributes<CodecAttribute>().SelectMany(codec => codec.Names);

    private static IEnumerable<string> CodecsOnMember<T>(string member) =>
        MemberOf<T>(member).GetCustomAttributes<CodecAttribute>().SelectMany(codec => codec.Names);

    private static MemberInfo MemberOf<T>(string member) =>
        (MemberInfo?)typeof(T).GetProperty(member)
        ?? typeof(T).GetField(member)
        ?? throw new InvalidOperationException($"no member {member} on {typeof(T)}");
}

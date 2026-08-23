using System;

namespace Fomoxa;

/// <summary>
/// Marks a Fomoxa network model, and gives a field its Fomoxa wire type.
/// </summary>
/// <remarks>
/// <para>
/// On a type, <c>[Network]</c> says "this is a Fomoxa network model". On a
/// field or property, <c>[Network("u32")]</c> says "this field is a <c>u32</c>
/// on the wire".
/// </para>
/// <code>
/// [Network]
/// [Codec("edge", "unity")]
/// public class DeviceState
/// {
///     [Network("u32")]
///     [Codec("edge", "unity")]
///     public uint Id { get; set; }
///
///     [Network("f32")]
///     [Codec("edge")]
///     public float Temperature { get; set; }
/// }
/// </code>
///
/// <para><b>The wire type is not the C# type.</b></para>
///
/// <para>
/// <see cref="WireType"/> holds a Fomoxa wire type identifier, defined by the
/// Fomoxa Specification. The C# type of the member it sits on is a separate
/// thing, and it does not decide a single byte:
/// </para>
/// <code>
/// [Network("u32")] public uint  Id { get; set; }   // u32 on the wire
/// [Network("u32")] public ulong Id { get; set; }   // still u32 on the wire
/// </code>
/// <para>
/// Both are four bytes, Little Endian, because <c>u32</c> says so. Whether a
/// <c>ulong</c> can hold one is a question for the code generator and the C#
/// compiler; it is not a question about the wire, and this attribute never asks
/// it.
/// </para>
///
/// <para><b>Why a string, and not a <see cref="Type"/>.</b></para>
///
/// <para>
/// <c>[Network(typeof(uint))]</c> would make the host language the source of
/// truth, and it is not. Fomoxa defines <c>u32</c> as four bytes Little Endian
/// for every language; C# defines <c>uint</c> as whatever it likes. Deriving one
/// from the other is how an implementation ends up unable to decode what another
/// implementation wrote — which is the failure this attribute exists to prevent.
/// </para>
///
/// <para>
/// So the string is stored exactly as written and is never interpreted here.
/// This assembly holds no list of valid wire types, no mapping from C# types,
/// and no opinion about byte layout. <c>fomoxac</c> reads the string and
/// resolves it against the Specification.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Field
        | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class NetworkAttribute : Attribute
{
    /// <summary>
    /// Marks a Fomoxa network model.
    /// </summary>
    /// <remarks>
    /// Used on a type. <see cref="WireType"/> is <see langword="null"/>, because
    /// a model is not a wire type — it is a sequence of fields that have them.
    /// </remarks>
    public NetworkAttribute()
    {
    }

    /// <summary>
    /// Declares the Fomoxa wire type of a field or property.
    /// </summary>
    /// <param name="wireType">
    /// A Fomoxa wire type identifier, spelled exactly as the Fomoxa
    /// Specification spells it — for example <c>"u32"</c>, <c>"f32"</c>,
    /// <c>"bool"</c>. It is stored verbatim and is not validated, mapped, or
    /// compared against the C# type of the member.
    /// </param>
    public NetworkAttribute(string wireType)
    {
        WireType = wireType;
    }

    /// <summary>
    /// The Fomoxa wire type, exactly as written, or <see langword="null"/> on a
    /// model.
    /// </summary>
    /// <remarks>
    /// This is a Fomoxa Specification identifier, not a C# type name. It is the
    /// only thing that decides the byte layout of the field, and nothing in this
    /// assembly reads it.
    /// </remarks>
    public string? WireType { get; }
}

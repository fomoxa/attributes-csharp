using System;
using System.Collections.Generic;

namespace Cyclone;

/// <summary>
/// Declares which codecs a model generates, and which of them a field belongs
/// to.
/// </summary>
/// <remarks>
/// <para>
/// On a type, <c>[Codec("edge", "unity")]</c> says which codecs <c>cyclonec</c>
/// must generate. On a field or property, it says which of those the field
/// appears in.
/// </para>
/// <code>
/// [Network]
/// [Codec("edge", "unity")]          // generate DeviceStateEdgeCodec and
/// public class DeviceState          // DeviceStateUnityCodec
/// {
///     [Network("u32")]
///     [Codec("edge", "unity")]      // in both
///     public uint Id { get; set; }
///
///     [Network("f32")]
///     [Codec("edge")]               // edge only
///     public float Temperature { get; set; }
///
///     [Network("string")]
///     [Codec("unity")]              // unity only
///     public string DisplayName { get; set; } = string.Empty;
/// }
/// </code>
///
/// <para><b>A codec name is an identifier, and nothing more.</b></para>
///
/// <para>
/// <c>edge</c>, <c>unity</c>, <c>orange_pi</c>, <c>database</c>,
/// <c>custom_protocol</c> — this assembly has never heard of any of them, holds
/// no list of them, and gives none of them meaning. They are names you invent,
/// and <c>cyclonec</c> uses them to spell the type it generates.
/// </para>
///
/// <para>
/// It knows nothing about byte layout either. Which fields a codec carries is a
/// question about <see cref="Names"/>; what those fields look like on the wire
/// is a question about <see cref="NetworkAttribute.WireType"/>. The two never
/// meet here.
/// </para>
///
/// <para>
/// The attribute may be repeated, and the order written is kept:
/// <c>[Codec("edge")] [Codec("unity")]</c> and <c>[Codec("edge", "unity")]</c>
/// say the same thing.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Field
        | AttributeTargets.Property,
    AllowMultiple = true,
    Inherited = false)]
public sealed class CodecAttribute : Attribute
{
    private readonly string[] _names;

    /// <summary>
    /// Declares one or more codec names.
    /// </summary>
    /// <param name="names">
    /// The codec identifiers, in the order they should be read. They are stored
    /// verbatim: nothing here resolves, imports, validates or interprets them.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="names"/> is <see langword="null"/>.
    /// </exception>
    public CodecAttribute(params string[] names)
    {
        _names = names ?? throw new ArgumentNullException(nameof(names));
    }

    /// <summary>
    /// The codec names, in the order they were written.
    /// </summary>
    public IReadOnlyList<string> Names => _names;
}

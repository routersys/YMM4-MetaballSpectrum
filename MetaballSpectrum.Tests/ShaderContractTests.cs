using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static MetaballSpectrum.MetaballSpectrumCustomEffect;

namespace MetaballSpectrum.Tests;

public sealed partial class ShaderContractTests
{
    const int RegisterByteSize = 16;

    static readonly string[] HeaderFields =
    [
        "FieldWidth",
        "FieldHeight",
        "ValueCount",
        "BlobRadius",
        "Threshold",
        "RimThickness",
        "RimBrightness",
        "Window",
        "Bipolar",
        "Pad0",
        "Pad1",
        "Pad2",
        "ColorR",
        "ColorG",
        "ColorB",
        "ColorA",
    ];

    static readonly Dictionary<string, string> Registers = new()
    {
        ["field"] = "FieldWidth",
        ["style"] = "Threshold",
        ["options"] = "Bipolar",
        ["tint"] = "ColorR",
    };

    static string Source { get; } = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Here())!, "..", "MetaballSpectrum.Shader", "MetaballSpectrum.hlsl"));

    static string Here([CallerFilePath] string path = "") => path;

    static Type ConstantBuffer => typeof(MetaballSpectrumCustomEffect).GetNestedType("EffectImpl", BindingFlags.NonPublic)!.GetNestedType("ConstantBuffer", BindingFlags.NonPublic)!;

    [GeneratedRegex(@"^\s*float4\s+(?<name>\w+)\s*(?:\[(?<length>\d+)\])?\s*:\s*packoffset\(c(?<register>\d+)\);", RegexOptions.Multiline)]
    private static partial Regex MemberPattern();

    static Dictionary<string, (int Offset, int Length)> Declared()
        => MemberPattern().Matches(Source).ToDictionary(
            match => match.Groups["name"].Value,
            match => (int.Parse(match.Groups["register"].Value) * RegisterByteSize, match.Groups["length"].Success ? int.Parse(match.Groups["length"].Value) : 1));

    [Fact]
    public void TheShaderDeclaresTheRegistersTheEffectFills()
    {
        var declared = Declared();

        Assert.Equal(Registers.Keys.Append("values").Order(), declared.Keys.Order());
        Assert.All(Registers, register => Assert.Equal(Array.IndexOf(HeaderFields, register.Value) * sizeof(float), declared[register.Key].Offset));
    }

    [Fact]
    public void TheHeaderFieldsFollowTheRegisterComponents()
    {
        var offsets = HeaderFields.Select(name => (int)Marshal.OffsetOf(ConstantBuffer, name)).ToArray();

        Assert.Equal(Enumerable.Range(0, HeaderFields.Length).Select(index => index * sizeof(float)), offsets);
    }

    [Fact]
    public void TheHeaderFillsWholeRegistersAndTheValuesFollowIt()
    {
        var declared = Declared();

        Assert.Equal(4 * RegisterByteSize, Marshal.SizeOf(ConstantBuffer));
        Assert.Equal(Marshal.SizeOf(ConstantBuffer), declared["values"].Offset);
    }

    [Fact]
    public void TheValueArrayHoldsFourValuesPerRegister()
    {
        var declared = Declared();

        Assert.Equal(MaxValues, declared["values"].Length * 4);
        Assert.Equal(ValueByteSize, declared["values"].Length * RegisterByteSize);
    }
}

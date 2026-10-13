namespace MetaballSpectrum;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/MetaballSpectrum;component/Resources/Shader/{shaderName}.cso", UriKind.Absolute);
}

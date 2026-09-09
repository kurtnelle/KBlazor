namespace KBlazor.Services;

/// <summary>
/// Platform-independent width estimator. Sums a per-character factor (in em,
/// approximating Helvetica/Arial metrics) and multiplies by the font size.
/// Used when the host registers no <see cref="ITextMeasurer"/>.
/// </summary>
public sealed class EstimatingTextMeasurer : ITextMeasurer
{
    public static readonly EstimatingTextMeasurer Instance = new();

    public float MeasureWidth(string text, string fontFamily, float fontSizePx)
    {
        if (string.IsNullOrEmpty(text) || fontSizePx <= 0f)
        {
            return 0f;
        }

        float em = 0f;
        foreach (var c in text)
        {
            em += Factor(c);
        }
        return em * fontSizePx;
    }

    private static float Factor(char c) => c switch
    {
        'i' or 'j' or 'l' or 't' or 'f' or 'r' => 0.30f,
        'm' or 'w' => 0.80f,
        >= 'a' and <= 'z' => 0.52f,
        'I' => 0.30f,
        'M' or 'W' => 0.85f,
        >= 'A' and <= 'Z' => 0.66f,
        >= '0' and <= '9' => 0.55f,
        ' ' => 0.28f,
        '.' or ',' or ':' or ';' or '\'' or '|' or '!' => 0.28f,
        _ => 0.60f
    };
}

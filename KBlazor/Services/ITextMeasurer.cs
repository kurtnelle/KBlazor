namespace KBlazor.Services;

/// <summary>
/// Measures rendered text width so FlexTable can pick default column widths.
/// Register an implementation to override the built-in estimator; when none is
/// registered, <see cref="EstimatingTextMeasurer.Instance"/> is used.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    /// Approximate rendered width of <paramref name="text"/> in CSS pixels.
    /// </summary>
    /// <param name="text">The text to measure. Null or empty returns 0.</param>
    /// <param name="fontFamily">CSS font family (may be ignored by estimators).</param>
    /// <param name="fontSizePx">Font size in CSS pixels.</param>
    float MeasureWidth(string text, string fontFamily, float fontSizePx);
}

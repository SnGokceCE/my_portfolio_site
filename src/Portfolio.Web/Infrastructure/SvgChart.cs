using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace Portfolio.Web.Infrastructure;

/// <summary>
/// Sunucuda çizilen, bağımlılıksız SVG çizgi grafiği. Renkleri CSS değişkenlerinden aldığı için
/// açık/karanlık temaya uyar; her nokta <c>title</c> ile değerini gösterir (erişilebilir ipucu).
/// </summary>
public static class SvgChart
{
    public sealed record Series(string Name, IReadOnlyList<int> Values, string CssClass, bool Area);

    public static IHtmlContent Line(
        IReadOnlyList<string> labels, IReadOnlyList<Series> series, int width = 760, int height = 220, string? ariaLabel = null)
    {
        const int padLeft = 36, padRight = 12, padTop = 12, padBottom = 28;
        var plotWidth = width - padLeft - padRight;
        var plotHeight = height - padTop - padBottom;
        var count = labels.Count;
        var max = Math.Max(1, series.SelectMany(s => s.Values).DefaultIfEmpty(0).Max());
        var niceMax = NiceCeiling(max);

        double X(int i) => padLeft + (count <= 1 ? plotWidth / 2.0 : plotWidth * i / (double)(count - 1));
        double Y(int v) => padTop + plotHeight - (plotHeight * v / (double)niceMax);
        string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        var svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture, $"<svg class=\"chart\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"{Encode(ariaLabel ?? string.Empty)}\" preserveAspectRatio=\"none\">");

        // Yatay kılavuz çizgileri ve değer etiketleri
        for (var step = 0; step <= 4; step++)
        {
            var value = niceMax * step / 4;
            var y = Y(value);
            svg.Append(CultureInfo.InvariantCulture, $"<line class=\"chart-grid\" x1=\"{padLeft}\" x2=\"{width - padRight}\" y1=\"{F(y)}\" y2=\"{F(y)}\" />");
            svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-axis\" x=\"{padLeft - 6}\" y=\"{F(y + 4)}\" text-anchor=\"end\">{value}</text>");
        }

        // X ekseni: ilk, orta ve son etiket (kalabalık olmasın)
        foreach (var i in new[] { 0, count / 2, count - 1 }.Distinct().Where(i => i >= 0 && i < count))
        {
            var anchor = i == 0 ? "start" : i == count - 1 ? "end" : "middle";
            svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-axis\" x=\"{F(X(i))}\" y=\"{height - 8}\" text-anchor=\"{anchor}\">{Encode(labels[i])}</text>");
        }

        foreach (var s in series)
        {
            var points = string.Join(' ', s.Values.Select((v, i) => $"{F(X(i))},{F(Y(v))}"));
            if (s.Area && count > 0)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<polygon class=\"{s.CssClass}-area\" points=\"{F(X(0))},{F(Y(0))} {points} {F(X(count - 1))},{F(Y(0))}\" />");
            }

            svg.Append(CultureInfo.InvariantCulture, $"<polyline class=\"{s.CssClass}\" points=\"{points}\" />");
            for (var i = 0; i < s.Values.Count; i++)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<circle class=\"{s.CssClass}-dot\" cx=\"{F(X(i))}\" cy=\"{F(Y(s.Values[i]))}\" r=\"3\"><title>{Encode(labels[i])} · {Encode(s.Name)}: {s.Values[i]}</title></circle>");
            }
        }

        svg.Append("</svg>");
        return new HtmlString(svg.ToString());
    }

    /// <summary>Küçük, eksensiz eğilim çizgisi (pano kartı için).</summary>
    public static IHtmlContent Sparkline(IReadOnlyList<int> values, int width = 160, int height = 40)
    {
        var max = Math.Max(1, values.DefaultIfEmpty(0).Max());
        string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
        var points = string.Join(' ', values.Select((v, i) =>
            $"{F(values.Count <= 1 ? width / 2.0 : width * i / (double)(values.Count - 1))},{F(height - 2 - ((height - 4) * v / (double)max))}"));
        return new HtmlString($"<svg class=\"sparkline\" viewBox=\"0 0 {width} {height}\" aria-hidden=\"true\" preserveAspectRatio=\"none\"><polyline points=\"{points}\" /></svg>");
    }

    /// <summary>
    /// Eksenin üst değeri: 4 kılavuz aralığının her biri "güzel" bir tam sayı olacak şekilde yuvarlanır
    /// (7 → 8, 23 → 24, 140 → 160). Böylece etiketler tam sayı olur ve çizgilerle hizalanır.
    /// </summary>
    internal static int NiceCeiling(int value)
    {
        var rawStep = Math.Max(1, value) / 4.0;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        foreach (var multiplier in new[] { 1, 2, 3, 4, 5, 6, 8, 10 })
        {
            var step = Math.Max(1, (int)Math.Round(multiplier * magnitude));
            if (step >= rawStep)
            {
                return step * 4;
            }
        }

        return (int)Math.Ceiling(rawStep) * 4;
    }

    private static string Encode(string text) => System.Net.WebUtility.HtmlEncode(text);
}

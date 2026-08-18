namespace CardStock.Web.Services;

/// <summary>
/// Presentation math for the browse wall's sprite normalization (D-113): each sprite is
/// cropped to its measured art box and drawn at the largest clean factor — ½, 1, 2, or 3 —
/// that fits the 68×56 slot. Clean factors keep pixel art crisp: integers map one source
/// pixel to a uniform block, and the half-sample (for the Gen 9-era 96×96 canvases) drops
/// every second pixel uniformly instead of the fractional squeeze object-fit produced.
/// The cap at 3 keeps the tiniest arts from turning comically chunky.
/// </summary>
public static class SpriteScale
{
    public const int SlotW = 68;
    public const int SlotH = 56;

    public static double Factor(int artW, int artH) =>
        artW > SlotW || artH > SlotH ? 0.5
        : artW * 3 <= SlotW && artH * 3 <= SlotH ? 3
        : artW * 2 <= SlotW && artH * 2 <= SlotH ? 2
        : 1;
}

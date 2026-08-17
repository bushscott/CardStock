namespace CardStock.Web.Services;

/// <summary>Deterministic accent pairs for set fan tiles — sets carry no stored
/// gradient (species do). Twelve muted pairs in the prototype's palette family;
/// the same set always draws the same pair.</summary>
public static class SetGradients
{
    private static readonly (string Start, string End)[] Palette =
    [
        ("#2B2D42", "#5C6B9E"), ("#3A4A5A", "#7E92A8"), ("#4A3A5A", "#8A7BA8"),
        ("#2D4238", "#5C9E7E"), ("#42352D", "#9E7E5C"), ("#2D3D42", "#5C8E9E"),
        ("#3D2D42", "#8E5C9E"), ("#42402D", "#9E965C"), ("#2D4242", "#5C9E9E"),
        ("#422D33", "#9E5C6B"), ("#33422D", "#6B9E5C"), ("#8A9BB8", "#D6E0EC"),
    ];

    public static (string Start, string End) For(long setId) =>
        Palette[(int)((ulong)setId % (ulong)Palette.Length)];
}

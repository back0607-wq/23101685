using UnityEngine;

namespace FireworkStudio
{
    public enum FireworkPropKind
    {
        Sparkler,
        Rocket
    }

    public enum FireworkPalette
    {
        Cyan,
        Pink,
        Violet,
        Ice,
        Mixed,
        Gold,
        Green,
        Red,
        Rainbow,
        Orange,
        Amber,
        RoyalBlue,
        Teal,
        Magenta,
        Lime,
        Sunset,
        Aurora,
        Candy
    }

    public enum FireworkPattern
    {
        Ring,
        DoubleRing,
        Flower,
        Peony,
        Willow,
        CometBurst,
        Star,
        Heart,
        Spiral,
        Crown,
        Snowflake,
        Butterfly,
        Palm,
        Galaxy,
        Diamond,
        FourLeaf,
        Cross,
        Fan,
        DoubleFlower
    }

    public static class FireworkPaletteUtil
    {
        // Rich, colored palettes: white is reserved for brief glints, not main burst particles.
        public static Color[] GetColors(FireworkPalette palette)
        {
            switch (palette)
            {
                case FireworkPalette.Cyan: return new[] { new Color(.06f,.92f,1f), new Color(.16f,.66f,1f), new Color(.08f,1f,.82f) };
                case FireworkPalette.Pink: return new[] { new Color(1f,.12f,.56f), new Color(1f,.28f,.82f), new Color(.98f,.14f,.31f) };
                case FireworkPalette.Violet: return new[] { new Color(.55f,.18f,1f), new Color(.8f,.23f,1f), new Color(.35f,.38f,1f) };
                case FireworkPalette.Ice: return new[] { new Color(.35f,.83f,1f), new Color(.58f,.77f,1f), new Color(.34f,1f,.94f) };
                case FireworkPalette.Gold: return new[] { new Color(1f,.61f,.05f), new Color(1f,.78f,.14f), new Color(1f,.38f,.04f) };
                case FireworkPalette.Green: return new[] { new Color(.08f,1f,.36f), new Color(.22f,.95f,.12f), new Color(.02f,.8f,.56f) };
                case FireworkPalette.Red: return new[] { new Color(1f,.09f,.15f), new Color(1f,.29f,.11f), new Color(.9f,.07f,.42f) };
                case FireworkPalette.Orange: return new[] { new Color(1f,.34f,.04f), new Color(1f,.52f,.09f), new Color(1f,.20f,.05f) };
                case FireworkPalette.Amber: return new[] { new Color(1f,.47f,.06f), new Color(1f,.69f,.09f), new Color(.95f,.26f,.05f) };
                case FireworkPalette.RoyalBlue: return new[] { new Color(.09f,.33f,1f), new Color(.24f,.52f,1f), new Color(.32f,.14f,1f) };
                case FireworkPalette.Teal: return new[] { new Color(.02f,.95f,.76f), new Color(.05f,.68f,.86f), new Color(.12f,1f,.44f) };
                case FireworkPalette.Magenta: return new[] { new Color(1f,.04f,.75f), new Color(.78f,.13f,1f), new Color(1f,.13f,.44f) };
                case FireworkPalette.Lime: return new[] { new Color(.62f,1f,.04f), new Color(.36f,1f,.09f), new Color(.84f,.92f,.07f) };
                case FireworkPalette.Sunset: return new[] { new Color(1f,.25f,.16f), new Color(1f,.56f,.06f), new Color(.96f,.14f,.62f) };
                case FireworkPalette.Aurora: return new[] { new Color(.08f,1f,.64f), new Color(.14f,.55f,1f), new Color(.72f,.2f,1f) };
                case FireworkPalette.Candy: return new[] { new Color(1f,.22f,.62f), new Color(.14f,.8f,1f), new Color(.68f,.3f,1f) };
                case FireworkPalette.Rainbow: return new[] { new Color(1f,.10f,.18f), new Color(1f,.56f,.08f), new Color(.36f,1f,.12f), new Color(.08f,.66f,1f), new Color(.73f,.16f,1f) };
                default: return new[] { new Color(.04f,.88f,1f), new Color(1f,.20f,.60f), new Color(.52f,.19f,1f), new Color(1f,.66f,.12f), new Color(.12f,1f,.54f) };
            }
        }

        public static Color RandomColor(FireworkPalette palette)
        {
            var colors = GetColors(palette);
            return colors[Random.Range(0, colors.Length)];
        }
    }
}

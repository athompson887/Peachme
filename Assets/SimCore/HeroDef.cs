// PeachMe SimCore - pure C#, no UnityEngine references allowed in this folder.
// Roster mirrors HEROES in tools/tune_peach.py.

using System.Collections.Generic;

namespace PeachMe.SimCore
{
    public enum SpecialKind
    {
        None,
        Charge,     // Teddy: smash through obstacles while active
        SaxDash,    // Bill: peach slows while active
        TapeErase   // Dick: instant gap gain
    }

    public sealed class HeroDef
    {
        public string Id;
        public string DisplayName;
        public string Strength;
        public string Weakness;

        public float SpeedMul = 1f;
        public float HalfWidth = 0.4f;
        public float LaneChangeTimeMul = 1f;
        public float JumpTimeMul = 1f;
        public bool CanSlide = true;
        public float MagnetRadius = 0f;
        public float PointsMul = 1f;
        public float HazardPenaltyMul = 1f;
        public float AccelTime = 1.6f;   // seconds from stopped to full speed

        public SpecialKind Special = SpecialKind.None;
        public float SpecialDuration = 0f;
        public float SpecialCooldown = 0f;

        public static readonly IReadOnlyList<HeroDef> Roster = new List<HeroDef>
        {
            new HeroDef
            {
                Id = "georgie", DisplayName = "Georgie Washingmachine",
                Strength = "Wide pickup magnet", Weakness = "No special move",
                MagnetRadius = 1.2f
            },
            new HeroDef
            {
                Id = "abe", DisplayName = "Abe Lincolnshire",
                Strength = "Long legs, big jumps", Weakness = "Slow to get going",
                SpeedMul = 1.02f, HalfWidth = 0.35f, JumpTimeMul = 1.4f, AccelTime = 2.0f
            },
            new HeroDef
            {
                Id = "teddy", DisplayName = "Teddy Roosterfelt",
                Strength = "Charge smashes obstacles", Weakness = "Can't slide",
                HalfWidth = 0.45f, CanSlide = false,
                Special = SpecialKind.Charge, SpecialDuration = 2.5f, SpecialCooldown = 12f
            },
            new HeroDef
            {
                Id = "donny", DisplayName = "Donny Trumpf",
                Strength = "Double points", Weakness = "Wide and slow to turn",
                HalfWidth = 0.45f, LaneChangeTimeMul = 1.15f, PointsMul = 2f
            },
            new HeroDef
            {
                Id = "bill", DisplayName = "Bill Clintonne",
                Strength = "Sax solo charms the peach", Weakness = "Long cooldown",
                Special = SpecialKind.SaxDash, SpecialDuration = 2f, SpecialCooldown = 30f
            },
            new HeroDef
            {
                Id = "dick", DisplayName = "Dick Nixxon",
                Strength = "Erase the tape (gain ground)", Weakness = "Scandals hurt more",
                PointsMul = 1.1f, HazardPenaltyMul = 1.5f,
                Special = SpecialKind.TapeErase, SpecialDuration = 0.1f, SpecialCooldown = 25f
            },
        };

        public static HeroDef ById(string id)
        {
            foreach (var h in Roster)
            {
                if (h.Id == id) return h;
            }
            return Roster[0];
        }
    }
}

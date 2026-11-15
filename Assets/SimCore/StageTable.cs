// PeachMe SimCore - pure C#, no UnityEngine references allowed in this folder.
// 50 states in order of admission to the Union (Delaware first, Hawaii last),
// then the Washington DC finale. Biome drives the Unity-side look only.

using System.Collections.Generic;

namespace PeachMe.SimCore
{
    public enum Biome { Coast, Forest, Plains, Desert, Mountain, Swamp, Snow, Tropical, City }

    public readonly struct StageInfo
    {
        public readonly string Code;
        public readonly string Name;
        public readonly Biome Biome;

        public StageInfo(string code, string name, Biome biome)
        {
            Code = code;
            Name = name;
            Biome = biome;
        }
    }

    public static class StageTable
    {
        public static readonly IReadOnlyList<StageInfo> Stages = new List<StageInfo>
        {
            new StageInfo("DE", "Delaware", Biome.Coast),
            new StageInfo("PA", "Pennsylvania", Biome.Forest),
            new StageInfo("NJ", "New Jersey", Biome.City),
            new StageInfo("GA", "Georgia", Biome.Forest),
            new StageInfo("CT", "Connecticut", Biome.Coast),
            new StageInfo("MA", "Massachusetts", Biome.City),
            new StageInfo("MD", "Maryland", Biome.Coast),
            new StageInfo("SC", "South Carolina", Biome.Swamp),
            new StageInfo("NH", "New Hampshire", Biome.Forest),
            new StageInfo("VA", "Virginia", Biome.Forest),
            new StageInfo("NY", "New York", Biome.City),
            new StageInfo("NC", "North Carolina", Biome.Forest),
            new StageInfo("RI", "Rhode Island", Biome.Coast),
            new StageInfo("VT", "Vermont", Biome.Snow),
            new StageInfo("KY", "Kentucky", Biome.Plains),
            new StageInfo("TN", "Tennessee", Biome.Forest),
            new StageInfo("OH", "Ohio", Biome.Plains),
            new StageInfo("LA", "Louisiana", Biome.Swamp),
            new StageInfo("IN", "Indiana", Biome.Plains),
            new StageInfo("MS", "Mississippi", Biome.Swamp),
            new StageInfo("IL", "Illinois", Biome.City),
            new StageInfo("AL", "Alabama", Biome.Swamp),
            new StageInfo("ME", "Maine", Biome.Coast),
            new StageInfo("MO", "Missouri", Biome.Plains),
            new StageInfo("AR", "Arkansas", Biome.Forest),
            new StageInfo("MI", "Michigan", Biome.Snow),
            new StageInfo("FL", "Florida", Biome.Tropical),
            new StageInfo("TX", "Texas", Biome.Desert),
            new StageInfo("IA", "Iowa", Biome.Plains),
            new StageInfo("WI", "Wisconsin", Biome.Snow),
            new StageInfo("CA", "California", Biome.Coast),
            new StageInfo("MN", "Minnesota", Biome.Snow),
            new StageInfo("OR", "Oregon", Biome.Forest),
            new StageInfo("KS", "Kansas", Biome.Plains),
            new StageInfo("WV", "West Virginia", Biome.Mountain),
            new StageInfo("NV", "Nevada", Biome.Desert),
            new StageInfo("NE", "Nebraska", Biome.Plains),
            new StageInfo("CO", "Colorado", Biome.Mountain),
            new StageInfo("ND", "North Dakota", Biome.Plains),
            new StageInfo("SD", "South Dakota", Biome.Plains),
            new StageInfo("MT", "Montana", Biome.Mountain),
            new StageInfo("WA", "Washington", Biome.Forest),
            new StageInfo("ID", "Idaho", Biome.Mountain),
            new StageInfo("WY", "Wyoming", Biome.Mountain),
            new StageInfo("UT", "Utah", Biome.Desert),
            new StageInfo("OK", "Oklahoma", Biome.Plains),
            new StageInfo("NM", "New Mexico", Biome.Desert),
            new StageInfo("AZ", "Arizona", Biome.Desert),
            new StageInfo("AK", "Alaska", Biome.Snow),
            new StageInfo("HI", "Hawaii", Biome.Tropical),
            new StageInfo("DC", "Washington DC", Biome.City),
        };

        public static int Count => Stages.Count;

        public static int ChapterOf(int stageIndex, int chapterSize)
        {
            return stageIndex / chapterSize;
        }

        public static int ChapterStart(int stageIndex, int chapterSize)
        {
            return (stageIndex / chapterSize) * chapterSize;
        }
    }
}

// PeachMe SimCore - pure C#, no UnityEngine references allowed in this folder.
// The sim never plays audio or VFX. It emits events; Unity maps them to
// ElevenLabs line IDs, sounds and particles.

namespace PeachMe.SimCore
{
    public enum SimEventType
    {
        RunStarted,
        StageStarted,       // Data = stage index
        StageCleared,       // Data = stage index
        ChapterStarted,     // Data = chapter index
        LifeRestored,       // Data = lives now
        PeachWoke,
        Jumped,
        Slid,
        LaneChanged,        // Data = new lane
        ObstacleHit,        // Data = (int)ItemKind
        ObstacleSmashed,    // Data = (int)ItemKind
        StarCollected,
        HotDogCollected,
        HazardHit,          // Data = (int)ItemKind, Value = points lost
        SpecialStarted,     // Data = (int)SpecialKind
        SpecialEnded,       // Data = (int)SpecialKind
        SpecialReady,       // Data = (int)SpecialKind
        PeachClose,         // Value = gap
        PeachBackedOff,     // Value = gap
        PeachCaught,        // Data = lives left
        RunWon,
        RunOver             // Data = stage index to retry from (chapter start)
    }

    public readonly struct SimEvent
    {
        public readonly SimEventType Type;
        public readonly int Data;
        public readonly float Value;

        public SimEvent(SimEventType type, int data = 0, float value = 0f)
        {
            Type = type;
            Data = data;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Type}({Data}, {Value:0.##})";
        }
    }
}

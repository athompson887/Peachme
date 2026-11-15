// PeachMe SimCore - pure C#, no UnityEngine references allowed in this folder.
// Values mirror Tools/tune_peach.py CFG. Python is the source of truth:
// tune there first, then copy the numbers here.

namespace PeachMe.SimCore
{
    public sealed class SimConfig
    {
        // --- Timing ---
        public float FixedDt = 1f / 60f;

        // --- Campaign ---
        public int ChapterSize = 6;             // lives refill + checkpoint at each chapter start
        public float StageLength = 450f;        // metres per state
        public float DifficultyExponent = 0.7f; // d = (idx/last)^exp ; <1 front-loads the ramp

        // --- Lanes & moves ---
        public int LaneCount = 3;
        public float LaneWidth = 2.0f;
        public float LaneChangeTime = 0.18f;    // seconds to cross one lane
        public float JumpTime = 0.6f;
        public float SlideTime = 0.6f;
        public float ObstacleHalfWidth = 0.8f;
        public float PickupHalfWidth = 0.5f;

        // --- Speed ---
        public float BaseRunSpeed = 9.0f;       // m/s at stage 0
        public float SpeedPerStage = 0.06f;     // m/s added per stage index
        public float StumbleSpeedFactor = 0.20f;
        public float BoostMul = 1.35f;
        public float BoostTime = 2.5f;

        // --- Lives ---
        public int StartLives = 3;
        public int MaxLives = 3;

        // --- Peach ---
        public float PeachStartGap = 16f;
        public float PeachMaxGap = 22f;
        public float PeachBaseFactor = 0.96f;        // fraction of nominal player speed
        public float PeachDifficultyFactor = 0.025f; // extra fraction at the final stage
        public float PeachWakeDelay = 2.0f;          // peach sits still at run start
        public float CatchInvuln = 1.5f;
        public float CaughtResetGap = 12f;
        public float PeachCloseGap = 6f;             // commentary trigger threshold
        public float MercyPerRetry = 0.015f;         // peach factor drop per game over in same chapter
        public int MercyMaxSteps = 6;

        // --- Hazards & specials ---
        public float HazardGapPenalty = 3f;
        public float TapeEraseGap = 3f;
        public float SaxPeachFactor = 0.8f;

        // --- Scoring ---
        public float PointsPerMetre = 1f;
        public int StarPoints = 10;
        public int HazardPoints = 50;
        public int SmashPoints = 25;

        // --- Track generation ---
        public float RowSpacingEasy = 24f;
        public float RowSpacingHard = 18f;
        public float StartClear = 30f;
        public float EndClear = 20f;
        public float DoubleObstacleChance = 0.8f;   // times difficulty
        public float HazardChanceBase = 0.15f;
        public float HazardChanceDiff = 0.30f;
        public float HotDogChance = 0.06f;
        public float StarLineChance = 0.6f;
        public int StarsPerLine = 4;
        public float StarSpacing = 2f;
    }
}

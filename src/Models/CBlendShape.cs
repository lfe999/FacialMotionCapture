using System.Collections.Generic;
using System.Linq;

namespace LFE.FacialMotionCapture.Models
{
    public static class CBlendShape
    {
        public const int BROW_INNER_UP = 0;
        public const int BROW_DOWN_LEFT = 1;
        public const int BROW_DOWN_RIGHT = 2;
        public const int BROW_OUTER_UP_LEFT = 3;
        public const int BROW_OUTER_UP_RIGHT = 4;
        public const int EYE_LOOK_UP_LEFT = 5;
        public const int EYE_LOOK_UP_RIGHT = 6;
        public const int EYE_LOOK_DOWN_LEFT = 7;
        public const int EYE_LOOK_DOWN_RIGHT = 8;
        public const int EYE_LOOK_IN_LEFT = 9;
        public const int EYE_LOOK_IN_RIGHT = 10;
        public const int EYE_LOOK_OUT_LEFT = 11;
        public const int EYE_LOOK_OUT_RIGHT = 12;
        public const int EYE_BLINK_LEFT = 13;
        public const int EYE_BLINK_RIGHT = 14;
        public const int EYE_SQUINT_LEFT = 15;
        public const int EYE_SQUINT_RIGHT = 16;
        public const int EYE_WIDE_LEFT = 17;
        public const int EYE_WIDE_RIGHT = 18;
        public const int CHEEK_PUFF = 19;
        public const int CHEEK_SQUINT_LEFT = 20;
        public const int CHEEK_SQUINT_RIGHT = 21;
        public const int NOSE_SNEER_LEFT = 22;
        public const int NOSE_SNEER_RIGHT = 23;
        public const int JAW_OPEN = 24;
        public const int JAW_FORWARD = 25;
        public const int JAW_LEFT = 26;
        public const int JAW_RIGHT = 27;
        public const int MOUTH_FUNNEL = 28;
        public const int MOUTH_PUCKER = 29;
        public const int MOUTH_LEFT = 30;
        public const int MOUTH_RIGHT = 31;
        public const int MOUTH_ROLL_UPPER = 32;
        public const int MOUTH_ROLL_LOWER = 33;
        public const int MOUTH_SHRUG_UPPER = 34;
        public const int MOUTH_SHRUG_LOWER = 35;
        public const int MOUTH_CLOSE = 36;
        public const int MOUTH_SMILE_LEFT = 37;
        public const int MOUTH_SMILE_RIGHT = 38;
        public const int MOUTH_FROWN_LEFT = 39;
        public const int MOUTH_FROWN_RIGHT = 40;
        public const int MOUTH_DIMPLE_LEFT = 41;
        public const int MOUTH_DIMPLE_RIGHT = 42;
        public const int MOUTH_UPPER_LEFT = 43;
        public const int MOUTH_UPPER_RIGHT = 44;
        public const int MOUTH_LOWER_DOWN_LEFT = 45;
        public const int MOUTH_LOWER_DOWN_RIGHT = 46;
        public const int MOUTH_PRESS_LEFT = 47;
        public const int MOUTH_PRESS_RIGHT = 48;
        public const int MOUTH_STRETCH_LEFT = 49;
        public const int MOUTH_STRETCH_RIGHT = 50;
        public const int MOUTH_TONGUE_OUT = 51;

        private static readonly Dictionary<int, string> _idToGroup = new Dictionary<int, string>()
        {
            { CBlendShape.BROW_DOWN_LEFT, "Brows" },
            { CBlendShape.BROW_DOWN_RIGHT, "Brows" },
            { CBlendShape.BROW_INNER_UP, "Brows" },
            { CBlendShape.BROW_OUTER_UP_LEFT, "Brows" },
            { CBlendShape.BROW_OUTER_UP_RIGHT, "Brows" },
            { CBlendShape.CHEEK_PUFF, "Cheeks" },
            { CBlendShape.CHEEK_SQUINT_LEFT, "Cheeks" },
            { CBlendShape.CHEEK_SQUINT_RIGHT, "Cheeks" },
            { CBlendShape.EYE_BLINK_LEFT, "Eyes" },
            { CBlendShape.EYE_BLINK_RIGHT, "Eyes" },
            { CBlendShape.EYE_SQUINT_LEFT, "Eyes" },
            { CBlendShape.EYE_SQUINT_RIGHT, "Eyes" },
            { CBlendShape.EYE_WIDE_LEFT, "Eyes" },
            { CBlendShape.EYE_WIDE_RIGHT, "Eyes" },
            { CBlendShape.EYE_LOOK_DOWN_LEFT, "Looking" },
            { CBlendShape.EYE_LOOK_DOWN_RIGHT, "Looking" },
            { CBlendShape.EYE_LOOK_IN_LEFT, "Looking" },
            { CBlendShape.EYE_LOOK_IN_RIGHT, "Looking" },
            { CBlendShape.EYE_LOOK_OUT_LEFT, "Looking" },
            { CBlendShape.EYE_LOOK_OUT_RIGHT, "Looking" },
            { CBlendShape.EYE_LOOK_UP_LEFT, "Looking" },
            { CBlendShape.EYE_LOOK_UP_RIGHT, "Looking" },
            { CBlendShape.JAW_FORWARD, "Jaw" },
            { CBlendShape.JAW_LEFT, "Jaw" },
            { CBlendShape.JAW_OPEN, "Jaw" },
            { CBlendShape.JAW_RIGHT, "Jaw" },
            { CBlendShape.MOUTH_CLOSE, "Mouth" },
            { CBlendShape.MOUTH_DIMPLE_LEFT, "Mouth" },
            { CBlendShape.MOUTH_DIMPLE_RIGHT, "Mouth" },
            { CBlendShape.MOUTH_FROWN_LEFT, "Mouth" },
            { CBlendShape.MOUTH_FROWN_RIGHT, "Mouth" },
            { CBlendShape.MOUTH_FUNNEL, "Mouth" },
            { CBlendShape.MOUTH_LEFT, "Mouth" },
            { CBlendShape.MOUTH_LOWER_DOWN_LEFT, "Mouth" },
            { CBlendShape.MOUTH_LOWER_DOWN_RIGHT, "Mouth" },
            { CBlendShape.MOUTH_PRESS_LEFT, "Mouth" },
            { CBlendShape.MOUTH_PRESS_RIGHT, "Mouth" },
            { CBlendShape.MOUTH_PUCKER, "Mouth" },
            { CBlendShape.MOUTH_RIGHT, "Mouth" },
            { CBlendShape.MOUTH_ROLL_LOWER, "Mouth" },
            { CBlendShape.MOUTH_ROLL_UPPER, "Mouth" },
            { CBlendShape.MOUTH_SHRUG_LOWER, "Mouth" },
            { CBlendShape.MOUTH_SHRUG_UPPER, "Mouth" },
            { CBlendShape.MOUTH_SMILE_LEFT, "Mouth" },
            { CBlendShape.MOUTH_SMILE_RIGHT, "Mouth" },
            { CBlendShape.MOUTH_STRETCH_LEFT, "Mouth" },
            { CBlendShape.MOUTH_STRETCH_RIGHT, "Mouth" },
            { CBlendShape.MOUTH_UPPER_LEFT, "Mouth" },
            { CBlendShape.MOUTH_UPPER_RIGHT, "Mouth" },
            { CBlendShape.NOSE_SNEER_LEFT, "Nose" },
            { CBlendShape.NOSE_SNEER_RIGHT, "Nose" },
            { CBlendShape.MOUTH_TONGUE_OUT, "Tongue" },
        };

        private static readonly Dictionary<string, List<int>> _groupToIds = _idToGroup
            .GroupBy(p => p.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pp => pp.Key).ToList()
            );

        private static readonly Dictionary<int, string> _idToName = new Dictionary<int, string>()
        {
            { CBlendShape.BROW_DOWN_LEFT, "Brow Down Left" },
            { CBlendShape.BROW_DOWN_RIGHT, "Brow Down Right" },
            { CBlendShape.BROW_INNER_UP, "Brow Inner Up" },
            { CBlendShape.BROW_OUTER_UP_LEFT, "Brow Outer Up Left" },
            { CBlendShape.BROW_OUTER_UP_RIGHT, "Brow Outer Up Right" },
            { CBlendShape.CHEEK_PUFF, "Cheek Puff" },
            { CBlendShape.CHEEK_SQUINT_LEFT, "Cheek Squint Left" },
            { CBlendShape.CHEEK_SQUINT_RIGHT, "Cheek Squint Right" },
            { CBlendShape.EYE_BLINK_LEFT, "Eye Blink Left" },
            { CBlendShape.EYE_BLINK_RIGHT, "Eye Blink Right" },
            { CBlendShape.EYE_LOOK_DOWN_LEFT, "Eye Look Down Left" },
            { CBlendShape.EYE_LOOK_DOWN_RIGHT, "Eye Look Down Right" },
            { CBlendShape.EYE_LOOK_IN_LEFT, "Eye Look In Left" },
            { CBlendShape.EYE_LOOK_IN_RIGHT, "Eye Look In Right" },
            { CBlendShape.EYE_LOOK_OUT_LEFT, "Eye Look Out Left" },
            { CBlendShape.EYE_LOOK_OUT_RIGHT, "Eye Look Out Right" },
            { CBlendShape.EYE_LOOK_UP_LEFT, "Eye Look Up Left" },
            { CBlendShape.EYE_LOOK_UP_RIGHT, "Eye Look Up Right" },
            { CBlendShape.EYE_SQUINT_LEFT, "Eye Squint Left" },
            { CBlendShape.EYE_SQUINT_RIGHT, "Eye Squint Right" },
            { CBlendShape.EYE_WIDE_LEFT, "Eye Wide Left" },
            { CBlendShape.EYE_WIDE_RIGHT, "Eye Wide Right" },
            { CBlendShape.JAW_FORWARD, "Jaw Forward" },
            { CBlendShape.JAW_LEFT, "Jaw Left" },
            { CBlendShape.JAW_OPEN, "Jaw Open" },
            { CBlendShape.JAW_RIGHT, "Jaw Right" },
            { CBlendShape.MOUTH_CLOSE, "Mouth Close" },
            { CBlendShape.MOUTH_DIMPLE_LEFT, "Mouth Dimple Left" },
            { CBlendShape.MOUTH_DIMPLE_RIGHT, "Mouth Dimple Right" },
            { CBlendShape.MOUTH_FROWN_LEFT, "Mouth Frown Left" },
            { CBlendShape.MOUTH_FROWN_RIGHT, "Mouth Frown Right" },
            { CBlendShape.MOUTH_FUNNEL, "Mouth Funnel" },
            { CBlendShape.MOUTH_LEFT, "Mouth Left" },
            { CBlendShape.MOUTH_LOWER_DOWN_LEFT, "Mouth Lower Down Left" },
            { CBlendShape.MOUTH_LOWER_DOWN_RIGHT, "Mouth Lower Down Right" },
            { CBlendShape.MOUTH_PRESS_LEFT, "Mouth Press Left" },
            { CBlendShape.MOUTH_PRESS_RIGHT, "Mouth Press Right" },
            { CBlendShape.MOUTH_PUCKER, "Mouth Pucker" },
            { CBlendShape.MOUTH_RIGHT, "Mouth Right" },
            { CBlendShape.MOUTH_ROLL_LOWER, "Mouth Roll Lower" },
            { CBlendShape.MOUTH_ROLL_UPPER, "Mouth Roll Upper" },
            { CBlendShape.MOUTH_SHRUG_LOWER, "Mouth Shrug Lower" },
            { CBlendShape.MOUTH_SHRUG_UPPER, "Mouth Shrug Upper" },
            { CBlendShape.MOUTH_SMILE_LEFT, "Mouth Smile Left" },
            { CBlendShape.MOUTH_SMILE_RIGHT, "Mouth Smile Right" },
            { CBlendShape.MOUTH_STRETCH_LEFT, "Mouth Stretch Left" },
            { CBlendShape.MOUTH_STRETCH_RIGHT, "Mouth Stretch Right" },
            { CBlendShape.MOUTH_TONGUE_OUT, "Mouth Tongue Out" },
            { CBlendShape.MOUTH_UPPER_LEFT, "Mouth Upper Left" },
            { CBlendShape.MOUTH_UPPER_RIGHT, "Mouth Upper Right" },
            { CBlendShape.NOSE_SNEER_LEFT, "Nose Sneer Left" },
            { CBlendShape.NOSE_SNEER_RIGHT, "Nose Sneer Right" },
        };

        private static readonly Dictionary<string, int> _nameToId = _idToName
            .GroupBy(p => p.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pp => pp.Key).FirstOrDefault()
            );

        public static string IdToName(int i)
        {
            return _idToName[i] ?? $"Blendshape {i}";
        }

        public static int? NameToId(string name)
        {
            return _nameToId[name];
        }

        public static IEnumerable<string> Names()
        {
            return _nameToId.Keys;
        }

        public static string IdToGroup(int i) {
            return _idToGroup[i] ?? $"Other";
        }

        public static IEnumerable<int> IdsInGroup(string group) {
            return _groupToIds[group] ?? Enumerable.Empty<int>();
        }

        public static IEnumerable<string> Groups()
        {
            return _groupToIds.Keys;
        }

    }
}

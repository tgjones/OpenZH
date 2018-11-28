﻿using System.Collections.Generic;
using OpenSage.Data.Ini;
using OpenSage.Data.Ini.Parser;

namespace OpenSage.Logic.Object
{
    [AddedIn(SageGame.Bfme)]
    public sealed class EmotionTrackerUpdateModuleData : UpdateModuleData
    {
        internal static EmotionTrackerUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<EmotionTrackerUpdateModuleData> FieldParseTable = new IniParseTable<EmotionTrackerUpdateModuleData>
        {
            { "TauntAndPointDistance", (parser, x) => x.TauntAndPointDistance = parser.ParseInteger() },
            { "TauntAndPointUpdateDelay", (parser, x) => x.TauntAndPointUpdateDelay = parser.ParseInteger() },
            { "TauntAndPointExcluded", (parser, x) => x.TauntAndPointExcluded = ObjectFilter.Parse(parser) },
            { "AfraidOf", (parser, x) => x.AfraidOf = ObjectFilter.Parse(parser) },
            { "AlwaysAfraidOf", (parser, x) => x.AlwaysAfraidOf = ObjectFilter.Parse(parser) },
            { "PointAt", (parser, x) => x.PointAt = ObjectFilter.Parse(parser) },
            { "FearScanDistance", (parser, x) => x.FearScanDistance = parser.ParseInteger() },
            { "AddEmotion", (parser, x) => x.Emotions.Add(Emotion.Parse(parser)) },
            { "HeroScanDistance", (parser, x) => x.HeroScanDistance = parser.ParseInteger() }
        };
        public int TauntAndPointDistance { get; private set; }
        public int TauntAndPointUpdateDelay { get; private set; }
        public ObjectFilter TauntAndPointExcluded { get; private set; }
        public ObjectFilter AfraidOf { get; private set; }
        public ObjectFilter AlwaysAfraidOf { get; private set; }
        public ObjectFilter PointAt { get; private set; }
        public int FearScanDistance { get; private set; }
        public List<Emotion> Emotions { get; } = new List<Emotion>();
        public int HeroScanDistance { get; private set; }
    }

    public sealed class Emotion
    {
        internal static Emotion Parse(IniParser parser)
        {
            var firstToken = parser.GetNextToken();
            var secondToken = parser.GetNextTokenOptional();

            var result = new Emotion();
            if (secondToken.HasValue)
            {
                result = parser.ParseBlock(FieldParseTable);
                result.Type = IniParser.ScanEnum<EmotionType>(firstToken);
                result.EmotionName = parser.ScanAssetReference(secondToken.Value);
            }
            else
            {
                result.Type = EmotionType.None;
                result.EmotionName = parser.ScanAssetReference(firstToken);
            }
            return result;
        }

        internal static readonly IniParseTable<Emotion> FieldParseTable = new IniParseTable<Emotion>
        {
            { "AttributeModifier", (parser, x) => x.AttributeModifier = parser.ParseAssetReference() },
        };

        public EmotionType Type { get; private set; }
        public string EmotionName { get; private set; }
        public string AttributeModifier { get; private set; }
    }
}

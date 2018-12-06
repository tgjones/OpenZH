﻿using OpenSage.Data.Ini.Parser;

namespace OpenSage.Logic.Object
{
    public sealed class KeepObjectDieModuleData : DieModuleData
    {
        internal static KeepObjectDieModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static new readonly IniParseTable<KeepObjectDieModuleData> FieldParseTable = DieModuleData.FieldParseTable
            .Concat(new IniParseTable<KeepObjectDieModuleData>
            {
                { "CollapsingTime", (parser, x) => x.CollapsingTime = parser.ParseInteger() },
            });

        [AddedIn(SageGame.Bfme2)]
        public int CollapsingTime { get; private set; }
    }
}

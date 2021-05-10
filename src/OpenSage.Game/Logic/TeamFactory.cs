﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenSage.Data.Sav;

namespace OpenSage.Logic
{
    internal sealed class TeamFactory
    {
        private readonly Dictionary<uint, TeamTemplate> _teamTemplatesById;
        private readonly Dictionary<string, TeamTemplate> _teamTemplatesByName;

        public TeamFactory()
        {
            _teamTemplatesById = new Dictionary<uint, TeamTemplate>();
            _teamTemplatesByName = new Dictionary<string, TeamTemplate>();
        }

        public void Initialize(Data.Map.Team[] mapTeams, IReadOnlyList<Player> players)
        {
            _teamTemplatesById.Clear();
            _teamTemplatesByName.Clear();

            foreach (var mapTeam in mapTeams)
            {
                var name = mapTeam.Properties["teamName"].Value as string;

                var ownerName = mapTeam.Properties["teamOwner"].Value as string;
                var owner = players.FirstOrDefault(player => player.Name == ownerName);

                var isSingleton = (bool) mapTeam.Properties["teamIsSingleton"].Value;

                AddTeamTemplate(name, owner, isSingleton);
            }

            // Adding this unknowm team makes the team template indices match.
            AddTeamTemplate("__Unknown", players.First(), true);
        }

        private void AddTeamTemplate(string name, Player owner, bool isSingleton)
        {
            var id = (uint) (_teamTemplatesById.Count + 1);

            var teamTemplate = new TeamTemplate(
                id,
                name,
                owner,
                isSingleton);

            _teamTemplatesById.Add(id, teamTemplate);
            _teamTemplatesByName.Add(name, teamTemplate);
        }

        public TeamTemplate FindTeamTemplateByName(string name)
        {
            if (_teamTemplatesByName.TryGetValue(name, out var result))
            {
                return result;
            }
            return null;
        }

        internal void Load(SaveFileReader reader)
        {
            reader.ReadVersion(1);

            var unknown1 = reader.ReadUInt32();

            var count = reader.ReadUInt16();
            if (count != _teamTemplatesById.Count)
            {
                throw new InvalidDataException();
            }

            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadUInt32();

                var teamTemplate = _teamTemplatesById[id];

                teamTemplate.Load(reader);
            }
        }
    }
}

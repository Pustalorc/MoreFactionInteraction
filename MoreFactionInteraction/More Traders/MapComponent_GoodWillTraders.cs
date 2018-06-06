﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace MoreFactionInteraction
{
    public class MapComponent_GoodWillTrader : MapComponent
    {
        //empty constructor
        public MapComponent_GoodWillTrader(Map map) : base(map)
        {
            if (TimesTraded.Count > NextFactionInteraction.Count) { }
        }

        //private IncidentQueue incidentQueue;
        private Dictionary<Faction, int> nextFactionInteraction = new Dictionary<Faction, int>();
        private Dictionary<Faction, int> timesTraded = new Dictionary<Faction, int>();

        /// <summary>
        /// Used to keep track of how many times the player traded with the faction and increase trader stock based on that.
        /// </summary>
        public Dictionary<Faction, int> TimesTraded
        {
            get
            {
                //intermingled :D
                foreach (Faction faction in NextFactionInteraction.Keys)
                {
                    if (!timesTraded.Keys.Contains(faction)) timesTraded.Add(faction, 0);
                }
                //trust betrayed, reset count.
                timesTraded.RemoveAll(x => x.Key.HostileTo(Faction.OfPlayer));
                return timesTraded;
            }
        }

        public Dictionary<Faction, int> NextFactionInteraction
        {
            get
            {
                //initialise values
                if (this.nextFactionInteraction.Count == 0)
                {
                    IEnumerable<Faction> friendlyFactions = from faction in Find.FactionManager.AllFactionsVisible
                                                     where !faction.HostileTo(Faction.OfPlayer) && !faction.IsPlayer
                                                     select faction;

                    foreach (Faction faction in friendlyFactions)
                    {
                        nextFactionInteraction.Add(faction, Find.TickManager.TicksGame + Rand.RangeInclusive(GenDate.TicksPerDay * 2, GenDate.TicksPerDay * 8));
                    }
                }
                //if a faction became hostile, remove
                //TODO: remove priorly scheduled incidents involving said faction
                nextFactionInteraction.RemoveAll(x => x.Key.HostileTo(Faction.OfPlayer));

                //and the opposite
                while ((from faction in Find.FactionManager.AllFactionsVisible
                     where !faction.HostileTo(Faction.OfPlayer) && !faction.IsPlayer && !nextFactionInteraction.ContainsKey(faction)
                     select faction).Any())
                {
                    nextFactionInteraction.Add(
                        key: (from faction in Find.FactionManager.AllFactionsVisible
                              where !faction.HostileTo(Faction.OfPlayer) && !faction.IsPlayer && !nextFactionInteraction.ContainsKey(faction)
                              select faction).First(),
                        value: Find.TickManager.TicksGame + Rand.RangeInclusive(GenDate.TicksPerDay * 2, GenDate.TicksPerDay * 4));
                }
                return nextFactionInteraction;
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            //We don't need to run all that often
            if (Find.TickManager.TicksGame % 531 == 0 && GenDate.DaysPassed > 8)
            {
                foreach (KeyValuePair<Faction, int> kvp in NextFactionInteraction)
                {
                    if (Find.TickManager.TicksGame >= kvp.Value)
                    {
                        Faction faction = kvp.Key;

                        IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(Find.Storyteller.def, IncidentCategory.AllyArrival, map);
                        incidentParms.forced = true;
                        incidentParms.faction = faction;
                        //trigger incident somewhere between half a day and 3 days from now
                        Find.Storyteller.incidentQueue.Add(IncidentDef(), Rand.Range(GenDate.TicksPerDay / 2, GenDate.TicksPerDay * 3), incidentParms);

                        NextFactionInteraction[faction] =
                                    Find.TickManager.TicksGame + (int)FactionInteractionTimeSeperator.TimeBetweenInteraction.Evaluate(faction.GoodwillWith(Faction.OfPlayer));

                        //kids, you shouldn't change values you iterate over.
                        break;
                    }
                }
            }
        }

        public static IncidentDef IncidentDef()
        {
            switch (Rand.RangeInclusive(0,4))
            {
                case 0: 
                case 1: return MFI_DefOf.ReverseTradeRequest;
                case 2: return IncidentDefOf.CaravanRequest;
                case 3:
                case 4: return IncidentDefOf.TraderCaravanArrival;


                default: return IncidentDefOf.TraderCaravanArrival;
            }
        }


        //working lists for ExposeData.
        List<Faction> factionsListforInteraction;
        List<Faction> factionsListforTimesTraded;
        List<int> intListForInteraction;
        List<int> intListforTimesTraded;

        public override void ExposeData()
        {
            Scribe_Collections.Look<Faction, int>(ref this.nextFactionInteraction, "MFI_nextFactionInteraction", LookMode.Reference, LookMode.Value, ref factionsListforInteraction, ref intListForInteraction);
            Scribe_Collections.Look<Faction, int>(ref this.timesTraded, "MFI_timesTraded", LookMode.Reference, LookMode.Value, ref factionsListforTimesTraded, ref intListforTimesTraded);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltraCal
{
    public enum LunarPhase
    {
        New,
        FirstQuarter,
        Full,
        ThirdQuarter,
        Other
    }

    class MoonHelper
    {
        const double AverageDaysPerCycle = 29.530587981;
        static TimeSpan LunarCycle = TimeSpan.FromDays(AverageDaysPerCycle);
        static DateTime NewMoonStart = DateTime.Parse("10/28/2019 3:37:00Z").ToUniversalTime();

        public static LunarPhase GetMoonPhase(DateTime date)
        {
            var daysPassed = date.ToUniversalTime() - NewMoonStart;

            var cycles = daysPassed.TotalMinutes / LunarCycle.TotalMinutes;
            var age = (cycles - (int)cycles) * AverageDaysPerCycle;

            var p = AverageDaysPerCycle;
            if (age < p && (age + 1) > p) return LunarPhase.New;
            p = AverageDaysPerCycle / 4;
            if (age < p && (age + 1) > p) return LunarPhase.FirstQuarter;
            p += AverageDaysPerCycle / 4;
            if (age < p && (age + 1) > p) return LunarPhase.Full;
            p += AverageDaysPerCycle / 4;
            if (age < p && (age + 1) > p) return LunarPhase.ThirdQuarter;
            return LunarPhase.Other;

        }
    }
}

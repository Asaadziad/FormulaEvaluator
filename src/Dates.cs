using System;
using System.Linq;

namespace FormulaEvaluator {
    public class Dates {
        public static DateTime AddDays(DateTime date, int count)
        {
            return date.AddDays(count);
        }
        public static DateTime AddMonths(DateTime date, int count)
        {
            return date.AddMonths(count);
        }
        public static DateTime StartWeek(DateTime date)
        {
            return date.AddDays((int)date.DayOfWeek * -1);
        }
        public static DateTime StartMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }
        
        public static DateTime StartYear(DateTime date)
        {
            int day = date.Day;
            int month = date.Month;
            date = AddMonths(date, month * -1 + 1);
            return AddDays(date, day * -1 + 1);
        }
        public static DateTime StartQuarterly(DateTime date)
        {
            DateTime[] startOfQuarters = new DateTime[] {
        new DateTime(date.Year, 3,1),
        new DateTime(date.Year, 6, 1),
        new DateTime(date.Year, 9, 1),
        new DateTime(date.Year, 12, 1)
        };
            return startOfQuarters.Where(d => d.Subtract(date).Days <= 0).Last();
        }

        public static DateTime EndWeek(DateTime date)
        {
            return date.AddDays((int)date.DayOfWeek * -1 + 6);
        }
        public static DateTime EndMonth(DateTime date)
        {
            date = new DateTime(date.Year, date.Month + 1, 1);
            return date.AddDays(-1);
        }
        public static DateTime EndYear(DateTime date)
        {
            date = new DateTime(date.Year + 1, 1, 1);
            return date.AddDays(-1);
        }

        public static DateTime EndQuarterly(DateTime date)
        {
            DateTime[] endOfQuarters = new DateTime[] {
                new DateTime(date.Year, 3, 31),
                new DateTime(date.Year, 6, 30),
                new DateTime(date.Year, 9, 30),
                new DateTime(date.Year, 12, 31)
            };
            return endOfQuarters.Where(d => d.Subtract(date).Days >= 0).First();
        }

        public static string GetDynamicDate(string type) {
            switch(type)
            {
                case "Now": return DateTime.Now.ToString();
                case "StartWeek": return StartWeek(DateTime.Now).ToString();
                case "StartMonth": return StartMonth(DateTime.Now).ToString();
                case "StartYear": return StartWeek(DateTime.Now).ToString();
                case "StartQuarterly": return StartQuarterly(DateTime.Now).ToString();
                case "EndWeek": return EndWeek(DateTime.Now).ToString();
                case "EndMonth": return EndMonth(DateTime.Now).ToString();
                case "EndYear": return EndWeek(DateTime.Now).ToString();
                case "EndQuarterly": return EndQuarterly(DateTime.Now).ToString();
            
            };
            return "";
        }
    }
}
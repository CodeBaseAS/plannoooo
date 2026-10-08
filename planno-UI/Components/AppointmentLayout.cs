using System.Globalization;
using static Models.Dtos.AppointmentDtos;

/// <summary>Hilfsfunktionen, um Termine pro Tag auszuwählen und nebeneinander anzuordnen.</summary>
public static class AppointmentLayout
{
    private static readonly CultureInfo De = new("de-DE");

    /// <summary>Ein Termin-Abschnitt an einem Tag (Minuten seit 00:00) inklusive Spalte bei Überschneidungen.</summary>
    public sealed record Placed(AppointmentResponse Item, int Start, int End, int Column, int Columns);

    /// <summary>Ganztägige Termine, die an diesem Tag stattfinden.</summary>
    public static List<AppointmentResponse> AllDayOn(IEnumerable<AppointmentResponse> items, DateTime day)
        => items.Where(a => a.IsAllDay && CoversDay(a, day))
                .OrderBy(a => a.StartTime).ThenBy(a => a.Title)
                .ToList();

    /// <summary>Alle Termine eines Tages (erst ganztägig, dann nach Startzeit) – für die Monatsansicht.</summary>
    public static List<AppointmentResponse> OnDay(IEnumerable<AppointmentResponse> items, DateTime day)
    {
        var list = items.ToList();
        var timed = list.Where(a => !a.IsAllDay && TrySegment(a, day, out _, out _))
                        .OrderBy(a => a.StartTime).ThenBy(a => a.Title);
        return AllDayOn(list, day).Concat(timed).ToList();
    }

    /// <summary>Termine mit Uhrzeit für die Zeitleiste. Überschneidende Termine werden in Spalten nebeneinander gelegt.</summary>
    public static List<Placed> Layout(IEnumerable<AppointmentResponse> items, DateTime day)
    {
        var segments = new List<(AppointmentResponse Item, int S, int E)>();
        foreach (var a in items.Where(x => !x.IsAllDay))
        {
            if (TrySegment(a, day, out int s, out int e)) segments.Add((a, s, e));
        }

        segments = segments.OrderBy(x => x.S).ThenByDescending(x => x.E).ThenBy(x => x.Item.Id).ToList();

        var result = new List<Placed>();
        var cluster = new List<(AppointmentResponse Item, int S, int E, int Col)>();
        var columnEnds = new List<int>();
        int clusterEnd = -1;

        void Flush()
        {
            foreach (var c in cluster) result.Add(new Placed(c.Item, c.S, c.E, c.Col, columnEnds.Count));
            cluster.Clear();
            columnEnds.Clear();
            clusterEnd = -1;
        }

        foreach (var seg in segments)
        {
            // Neuer Cluster, sobald sich nichts mehr überschneidet
            if (cluster.Count > 0 && seg.S >= clusterEnd) Flush();

            int col = columnEnds.FindIndex(end => end <= seg.S);
            if (col < 0)
            {
                col = columnEnds.Count;
                columnEnds.Add(seg.E);
            }
            else
            {
                columnEnds[col] = seg.E;
            }

            cluster.Add((seg.Item, seg.S, seg.E, col));
            clusterEnd = Math.Max(clusterEnd, seg.E);
        }

        Flush();
        return result;
    }

    // ---------- Texte ----------
    public static string DisplayTitle(AppointmentResponse a)
        => string.IsNullOrWhiteSpace(a.Title) ? "(Kein Titel)" : a.Title;

    public static string TimeLabel(int minutes)
    {
        minutes = ((minutes % 1440) + 1440) % 1440;
        var h = minutes / 60;
        var m = minutes % 60;
        var h12 = h % 12 == 0 ? 12 : h % 12;
        return $"{h12}:{m:00}{(h < 12 ? "AM" : "PM")}";
    }

    public static string TimeLabel(DateTime t) => TimeLabel(t.Hour * 60 + t.Minute);

    public static string Range(Placed p) => $"{TimeLabel(p.Start)} – {TimeLabel(p.End)}";

    public static string Tooltip(AppointmentResponse a)
    {
        var date = a.StartTime.ToString("dddd, d. MMMM", De);
        var when = a.IsAllDay ? $"{date} · Ganztägig" : $"{date} · {TimeLabel(a.StartTime)} – {TimeLabel(a.EndTime)}";
        var text = $"{DisplayTitle(a)}\n{when}";
        return string.IsNullOrWhiteSpace(a.Location) ? text : $"{text}\n{a.Location}";
    }

    // ---------- Intern ----------
    private static bool CoversDay(AppointmentResponse a, DateTime day)
    {
        var first = a.StartTime.Date;

        // Ganztägige Termine enden exklusiv um 00:00 des Folgetags (so speichert es das Termin-Fenster)
        var endExclusive = a.EndTime.TimeOfDay == TimeSpan.Zero && a.EndTime > a.StartTime
            ? a.EndTime.Date
            : a.EndTime.Date.AddDays(1);
        if (endExclusive <= first) endExclusive = first.AddDays(1);

        return day.Date >= first && day.Date < endExclusive;
    }

    private static bool TrySegment(AppointmentResponse a, DateTime day, out int start, out int end)
    {
        start = end = 0;

        DateTime dayStart = day.Date;
        DateTime dayEnd = dayStart.AddDays(1);
        DateTime s = a.StartTime;
        DateTime e = a.EndTime < a.StartTime ? a.StartTime : a.EndTime;

        bool overlaps = s < dayEnd && (e > dayStart || (e == s && s >= dayStart));
        if (!overlaps) return false;

        start = (int)Math.Max(0, (s - dayStart).TotalMinutes);
        end = (int)Math.Min(1440, (e - dayStart).TotalMinutes);
        end = Math.Max(end, Math.Min(start + 20, 1440));   // Mindestlänge, damit kurze Termine sichtbar bleiben
        return true;
    }
}

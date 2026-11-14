using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrbitHealth.Api.Data;
using OrbitHealth.Api.Models;

namespace OrbitHealth.Api.Services;

public class VitalsState
{
    public double Hr { get; set; }
    public double Spo2 { get; set; }
    public double Temp { get; set; }
    public double Sbp { get; set; }
    public double Dbp { get; set; }
    public double Rr { get; set; }

    public VitalsState Clone() => (VitalsState)MemberwiseClone();
}

public class SymptomState
{
    public string Id { get; set; } = "";
    public int Sev { get; set; }
    public long Ts { get; set; }
    public string Src { get; set; } = "manual";
    public string Note { get; set; } = "";

    public SymptomState Clone() => (SymptomState)MemberwiseClone();
}

public class RiskState
{
    public Dictionary<string, int> Scores { get; set; } = new();
    public int Max { get; set; }
    public string Top { get; set; } = "msk";
    public string Status { get; set; } = "nominal";

    public RiskState Clone() => new()
    {
        Scores = new Dictionary<string, int>(Scores),
        Max = Max,
        Top = Top,
        Status = Status
    };
}

public class CrewState
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Color { get; set; } = "#ff8a3d";
    public int Age { get; set; }
    public VitalsState Vitals { get; set; } = new();
    public double Sleep { get; set; }
    public double Exercise { get; set; }
    public double Eva { get; set; }
    public double Dose { get; set; }
    public double Co2 { get; set; }
    public List<SymptomState> Symptoms { get; set; } = new();
    public RiskState Risk { get; set; } = new();
    public string Status { get; set; } = "nominal";
    public List<int> Trend { get; set; } = new();
    public string Source { get; set; } = "sim";
    public long LastPacket { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public double LastSeen { get; set; }

    public CrewState Clone() => new()
    {
        Id = Id, Name = Name, Role = Role, Color = Color, Age = Age,
        Vitals = Vitals.Clone(), Sleep = Sleep, Exercise = Exercise, Eva = Eva,
        Dose = Dose, Co2 = Co2,
        Symptoms = Symptoms.Select(s => s.Clone()).ToList(),
        Risk = Risk.Clone(), Status = Status, Trend = new List<int>(Trend),
        Source = Source, LastPacket = LastPacket, LastSeen = LastSeen
    };
}

public class AlertState
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public string CrewId { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Level { get; set; } = "";
    public string Title { get; set; } = "";
    public string Msg { get; set; } = "";
    public long Ts { get; set; }
    public bool Ack { get; set; }

    public AlertState Clone() => (AlertState)MemberwiseClone();
}

public class MissionDto
{
    public string Name { get; set; } = "ARES-3";
    public string Destination { get; set; } = "المريخ";
    public int Day { get; set; } = 148;
    public int Minutes { get; set; } = 14 * 60 + 20;
    public bool Storm { get; set; }
}

public class StatsDto
{
    public double AvgSleep { get; set; }
    public int MaxDose { get; set; }
    public int ActiveAlerts { get; set; }
    public int Ready { get; set; }
}

public class SnapshotDto
{
    public string Type { get; set; } = "state";
    public long Ts { get; set; }
    public MissionDto Mission { get; set; } = new();
    public List<CrewState> Crew { get; set; } = new();
    public List<AlertState> Alerts { get; set; } = new();
    public StatsDto Stats { get; set; } = new();
    public bool Simulate { get; set; }
    public bool Auto { get; set; }
    public double CommDelay { get; set; }
    public List<SensorState> Sensors { get; set; } = new();
    public string Storage { get; set; } = "InMemory";
}

public class ReportSummaryDto
{
    public int Total { get; set; }
    public List<ReportRiskDto> AtRisk { get; set; } = new();
    public double AvgSleep { get; set; }
    public int MaxDose { get; set; }
    public int Alerts { get; set; }
}

public class ReportRiskDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string Top { get; set; } = "";
    public int Score { get; set; }
}

public class ReportCrewDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
    public VitalsState Vitals { get; set; } = new();
    public double Sleep { get; set; }
    public int Dose { get; set; }
    public List<ReportTopRiskDto> Top { get; set; } = new();
    public List<ReportSymptomDto> Symptoms { get; set; } = new();
    public string[] Recs { get; set; } = [];
}

public class ReportTopRiskDto
{
    public string Name { get; set; } = "";
    public int Score { get; set; }
}

public class ReportSymptomDto
{
    public string Label { get; set; } = "";
    public int Sev { get; set; }
    public string Src { get; set; } = "manual";
}

public class ReportDto
{
    public string Id { get; set; } = "";
    public string Scope { get; set; } = "mission";
    public string Title { get; set; } = "";
    public long Ts { get; set; }
    public int Day { get; set; }
    public string Status { get; set; } = "draft";
    public ReportSummaryDto Summary { get; set; } = new();
    public List<ReportCrewDto> Crew { get; set; } = new();
    public long? SentAt { get; set; }
    public long? Eta { get; set; }
    public long? ReceivedAt { get; set; }
    public long? AckedAt { get; set; }
    public string? Reply { get; set; }
}

public class SensorState
{
    public string CrewId { get; set; } = "";
    public string Kind { get; set; } = "vision";
    public long Ts { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
    public List<string> Flags { get; set; } = new();
    public string Level { get; set; } = "nominal";
    public string Note { get; set; } = "";

    public SensorState Clone() => new()
    {
        CrewId = CrewId, Kind = Kind, Ts = Ts, Level = Level, Note = Note,
        Metrics = new Dictionary<string, double>(Metrics), Flags = new List<string>(Flags)
    };
}

public class MissionState
{
    private readonly object _gate = new();
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<MissionState> _logger;
    private readonly Dictionary<string, Dictionary<string, long>> _autoSeen = new();
    private int _tickCounter;

    public MissionDto Mission { get; private set; } = new();
    public List<CrewState> Crew { get; private set; } = new();
    public List<AlertState> Alerts { get; private set; } = new();
    public List<ReportDto> Reports { get; private set; } = new();
    public List<SensorState> Sensors { get; private set; } = new();
    public bool Simulate { get; set; } = true;
    public bool Auto { get; set; } = true;
    public bool Storm { get; set; }
    public double CommDelay { get; }
    public bool DbOk { get; private set; }
    public string DbProvider { get; private set; } = "InMemory";

    public MissionState(IServiceScopeFactory scopes, IConfiguration config, ILogger<MissionState> logger)
    {
        _scopes = scopes;
        _config = config;
        _logger = logger;
        CommDelay = config.GetValue("CommDelaySeconds", 12.0);
        Simulate = config.GetValue("Simulate", true);
        Load();
    }

    // ---------------------------------------------------------------- seed / load
    private void Load()
    {
        try
        {
            LoadFromDb();
            DbOk = true;
            DbProvider = _config.GetValue("Database:Provider", "SqlServer");
        }
        catch (Exception e)
        {
            DbOk = false;
            DbProvider = "InMemory";
            _logger.LogWarning(e, "SQL Server unavailable - running with in-memory state (data will not persist)");
            Crew = SeedCrew();
            var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Crew[0].Symptoms.Add(new SymptomState { Id = "headache", Sev = 1, Ts = t - 3 * 3600_000L, Src = "manual", Note = "صداع خفيف بعد اجتماع" });
            Crew[2].Symptoms.Add(new SymptomState { Id = "insomnia", Sev = 2, Ts = t - 8 * 3600_000L, Src = "auto", Note = "نوم أقل من 6 ساعات" });
            Crew[3].Symptoms.Add(new SymptomState { Id = "back_pain", Sev = 1, Ts = t - 20 * 3600_000L, Src = "manual", Note = "" });
            InitializeRisk();
        }
    }

    private void LoadFromDb()
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // قاعدة موجودة من نسخة قديمة: نضيف جدول المستشعرات من غير ما نمسح البيانات
        if (db.Database.IsSqlServer())
        {
            db.Database.ExecuteSqlRaw("""
                IF OBJECT_ID(N'[Sensors]', N'U') IS NULL
                CREATE TABLE [Sensors] (
                    [Id] nvarchar(450) NOT NULL PRIMARY KEY,
                    [CrewId] nvarchar(450) NOT NULL,
                    [Kind] nvarchar(max) NOT NULL,
                    [Ts] bigint NOT NULL,
                    [Level] nvarchar(max) NOT NULL,
                    [FlagsJson] nvarchar(max) NOT NULL,
                    [MetricsJson] nvarchar(max) NOT NULL,
                    [Note] nvarchar(max) NOT NULL
                );
                """);
        }

        if (!db.Crew.Any())
        {
            var seed = SeedCrew();
            db.Crew.AddRange(seed.Select(ToEntity));
            db.SaveChanges();

            var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            db.Symptoms.AddRange(
                new SymptomEntity { CrewId = "c1", Code = "headache", Severity = 1, Source = "manual", Note = "صداع خفيف بعد اجتماع", Ts = t - 3 * 3600_000L },
                new SymptomEntity { CrewId = "c3", Code = "vision_change", Severity = 1, Source = "manual", Note = "صعوبة قراءة قريبة", Ts = t - 26 * 3600_000L },
                new SymptomEntity { CrewId = "c3", Code = "tinnitus", Severity = 1, Source = "manual", Note = "", Ts = t - 26 * 3600_000L },
                new SymptomEntity { CrewId = "c3", Code = "insomnia", Severity = 2, Source = "auto", Note = "نوم أقل من 6 ساعات", Ts = t - 8 * 3600_000L },
                new SymptomEntity { CrewId = "c4", Code = "back_pain", Severity = 1, Source = "manual", Note = "", Ts = t - 20 * 3600_000L });
            db.SaveChanges();
        }

        Crew = db.Crew.AsNoTracking().Select(FromEntity).ToList();
        foreach (var c in Crew)
        {
            var last = db.Telemetry.AsNoTracking().Where(x => x.CrewId == c.Id)
                .OrderByDescending(x => x.Ts).FirstOrDefault();
            if (last != null)
            {
                c.Vitals = new VitalsState { Hr = last.Hr, Spo2 = last.Spo2, Temp = last.Temp, Sbp = last.Sbp, Dbp = last.Dbp, Rr = last.Rr };
                c.Co2 = last.Co2;
                c.Dose = last.Dose;
                c.Source = last.Source;
                c.LastPacket = last.Ts;
            }
            c.Symptoms = db.Symptoms.AsNoTracking()
                .Where(s => s.CrewId == c.Id && s.Ts > DateTimeOffset.UtcNow.AddHours(-24).ToUnixTimeMilliseconds())
                .OrderByDescending(s => s.Ts)
                .Select(s => new SymptomState { Id = s.Code, Sev = s.Severity, Ts = s.Ts, Src = s.Source, Note = s.Note })
                .ToList();
        }

        Reports = db.Reports.AsNoTracking().OrderByDescending(r => r.Ts).Take(50)
            .Select(r => JsonSerializer.Deserialize<ReportDto>(r.PayloadJson, JsonOptions))
            .OfType<ReportDto>().ToList();

        Sensors = db.Sensors.AsNoTracking().OrderByDescending(x => x.Ts).Take(60)
            .Select(x => new SensorState
            {
                CrewId = x.CrewId, Kind = x.Kind, Ts = x.Ts, Level = x.Level, Note = x.Note,
                Flags = JsonSerializer.Deserialize<List<string>>(x.FlagsJson, JsonOptions) ?? new(),
                Metrics = JsonSerializer.Deserialize<Dictionary<string, double>>(x.MetricsJson, JsonOptions) ?? new()
            }).ToList();

        InitializeRisk();
    }

    private void InitializeRisk()
    {
        var initial = new Dictionary<string, VitalsState>
        {
            ["c1"] = new() { Hr = 74, Spo2 = 98, Temp = 36.8, Sbp = 118, Dbp = 76, Rr = 14 },
            ["c2"] = new() { Hr = 68, Spo2 = 99, Temp = 36.6, Sbp = 112, Dbp = 72, Rr = 13 },
            ["c3"] = new() { Hr = 80, Spo2 = 97, Temp = 36.9, Sbp = 121, Dbp = 79, Rr = 15 },
            ["c4"] = new() { Hr = 78, Spo2 = 98, Temp = 36.7, Sbp = 116, Dbp = 74, Rr = 14 }
        };
        foreach (var c in Crew)
        {
            if (c.Vitals.Hr <= 0 && initial.TryGetValue(c.Id, out var v)) c.Vitals = v;
            c.Risk = RiskEngine.Compute(c, Mission.Day, Storm);
            c.Status = c.Risk.Status;
            c.Trend = Enumerable.Range(0, 50).Select(_ => Math.Max(0, c.Risk.Max - 10 + Random.Shared.Next(-3, 4))).ToList();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static List<CrewState> SeedCrew() =>
    [
        new() { Id = "c1", Name = "سارة منصور", Role = "قائدة المهمة", Color = "#ff8a3d", Age = 41,
                Sleep = 7.1, Exercise = 150, Eva = 4, Dose = 78, Co2 = 820 },
        new() { Id = "c2", Name = "يوسف حداد", Role = "طبيب الطاقم", Color = "#3ddad7", Age = 38,
                Sleep = 6.8, Exercise = 130, Eva = 2, Dose = 72, Co2 = 760 },
        new() { Id = "c3", Name = "لينا فارس", Role = "مهندسة أنظمة", Color = "#c084fc", Age = 34,
                Sleep = 5.7, Exercise = 90, Eva = 3, Dose = 75, Co2 = 940 },
        new() { Id = "c4", Name = "عمر الشريف", Role = "أخصائي أبحاث", Color = "#5aa9ff", Age = 36,
                Sleep = 6.4, Exercise = 110, Eva = 6, Dose = 80, Co2 = 880 }
    ];

    private static CrewEntity ToEntity(CrewState c) => new()
    {
        Id = c.Id, Name = c.Name, Role = c.Role, Color = c.Color, Age = c.Age,
        SleepHours = c.Sleep, ExerciseMinutes = c.Exercise, EvaHours = c.Eva,
        RadiationDose = c.Dose, Co2Ppm = c.Co2
    };

    private static CrewState FromEntity(CrewEntity e) => new()
    {
        Id = e.Id, Name = e.Name, Role = e.Role, Color = e.Color, Age = e.Age,
        Sleep = e.SleepHours, Exercise = e.ExerciseMinutes, Eva = e.EvaHours,
        Dose = e.RadiationDose, Co2 = e.Co2Ppm
    };

    // ---------------------------------------------------------------- tick
    public void Tick()
    {
        lock (_gate)
        {
            Mission.Minutes++;
            if (Mission.Minutes >= 1440) { Mission.Minutes -= 1440; Mission.Day++; }
            _tickCounter++;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            foreach (var c in Crew)
            {
                if (c.Source == "device" && now - c.LastPacket < 30_000) continue;
                var v = c.Vitals;
                v.Hr = Math.Clamp(v.Hr + Rand(-1.6, 1.7), 54, 118);
                v.Spo2 = Math.Clamp(v.Spo2 + Rand(-0.35, 0.32), 92, 100);
                v.Temp = Math.Clamp(v.Temp + Rand(-0.04, 0.04), 36.2, 38.2);
                v.Sbp = Math.Clamp(v.Sbp + Rand(-1.6, 1.6), 96, 146);
                c.Co2 = Math.Clamp(c.Co2 + Rand(-45, 48), 480, 4200);
                c.Dose += 0.012;
                c.LastPacket = now;
            }

            AutoDetect();
            Evaluate();
            CheckDelivery();
            if (_tickCounter % 5 == 0) PersistTelemetry();
        }
    }

    private static double Rand(double a, double b) => a + Random.Shared.NextDouble() * (b - a);

    private void Evaluate()
    {
        foreach (var c in Crew)
        {
            var prev = c.Status;
            c.Risk = RiskEngine.Compute(c, Mission.Day, Storm);
            c.Status = c.Risk.Status;
            c.Trend.Add(c.Risk.Max);
            if (c.Trend.Count > 60) c.Trend.RemoveAt(0);
            if (c.Status != prev && c.Status is "urgent" or "critical")
            {
                var d = DomainCatalog.Get(c.Risk.Top);
                PushAlert(c, d.Id, c.Status, $"{c.Name}: {d.Name} {c.Risk.Scores[d.Id]}",
                    $"{LevelAr(c.Status)} — {RecommendationCatalog.All[d.Id][0]}");
            }
        }
    }

    public static string LevelAr(string level) => level switch
    {
        "nominal" => "طبيعي", "watch" => "مراقبة", "urgent" => "عاجل", "critical" => "حرج", _ => level
    };

    private void PushAlert(CrewState crew, string domain, string level, string title, string msg)
    {
        var key = $"{crew.Id}:{domain}:{level}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (Alerts.Any(a => a.Key == key && !a.Ack && now - a.Ts < 6 * 3600_000L)) return;
        Alerts.Insert(0, new AlertState
        {
            Id = $"a{now}{Random.Shared.Next(0, 999)}", Key = key, CrewId = crew.Id, Domain = domain,
            Level = level, Title = title, Msg = msg, Ts = now
        });
        if (Alerts.Count > 40) Alerts.RemoveRange(40, Alerts.Count - 40);
    }

    private void AutoDetect()
    {
        if (!Auto) return;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var c in Crew)
        {
            var v = c.Vitals;
            var seen = _autoSeen.TryGetValue(c.Id, out var d) ? d : _autoSeen[c.Id] = new();
            void Check(string sym, bool cond, int sev, string note)
            {
                if (!cond) return;
                if (now - seen.GetValueOrDefault(sym, 0) < 6 * 3600_000L) return;
                seen[sym] = now;
                AddSymptomInternal(c, sym, sev, "auto", note);
            }
            Check("headache", c.Co2 > 1800, 1, $"ارتفاع CO2: {Math.Round(c.Co2)} ppm");
            Check("breathlessness", v.Spo2 < 94, 2, $"انخفاض SpO2: {v.Spo2:F1}%");
            Check("fatigue", v.Hr > 108, 1, $"تسارع النبض: {Math.Round(v.Hr)}");
            Check("insomnia", c.Sleep < 6, 1, "نوم أقل من 6 ساعات");
        }
    }

    private void AddSymptomInternal(CrewState c, string sym, int sev, string src, string note)
    {
        if (!SymptomCatalog.All.ContainsKey(sym)) return;
        var entry = new SymptomState
        {
            Id = sym, Sev = Math.Clamp(sev, 1, 3),
            Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Src = src, Note = note
        };
        c.Symptoms.Insert(0, entry);
        if (c.Symptoms.Count > 100) c.Symptoms.RemoveAt(c.Symptoms.Count - 1);
        c.Risk = RiskEngine.Compute(c, Mission.Day, Storm);
        c.Status = c.Risk.Status;
        PersistSymptom(c.Id, entry);
    }

    public SymptomState? AddSymptom(string crewId, string sym, int sev, string src, string note)
    {
        lock (_gate)
        {
            var c = Crew.FirstOrDefault(x => x.Id == crewId);
            if (c == null || !SymptomCatalog.All.ContainsKey(sym)) return null;
            AddSymptomInternal(c, sym, sev, src, note);
            if (c.Status is "urgent" or "critical")
            {
                var d = DomainCatalog.Get(c.Risk.Top);
                PushAlert(c, d.Id, c.Status, $"{c.Name}: {d.Name} {c.Risk.Scores[d.Id]}",
                    $"{LevelAr(c.Status)} — {RecommendationCatalog.All[d.Id][0]}");
            }
            return c.Symptoms[0];
        }
    }

    public int ClearSymptom(string crewId, string sym)
    {
        lock (_gate)
        {
            var c = Crew.FirstOrDefault(x => x.Id == crewId);
            if (c == null) return 0;
            var n = c.Symptoms.RemoveAll(s => s.Id == sym);
            c.Risk = RiskEngine.Compute(c, Mission.Day, Storm);
            c.Status = c.Risk.Status;
            return n;
        }
    }

    // ---------------------------------------------------------------- sensors
    public SensorState? AddSensor(string crewId, string kind, Dictionary<string, double> metrics, string? note)
    {
        lock (_gate)
        {
            var c = Crew.FirstOrDefault(x => x.Id == crewId);
            if (c == null) return null;
            kind = kind.ToLowerInvariant();
            if (kind is not ("vision" or "voice" or "typing" or "mood")) return null;

            double M(string k) => metrics.TryGetValue(k, out var v) ? v : 0;
            var flags = new List<string>();
            string sym;
            string label;

            if (kind == "vision")
            {
                sym = "vision_strain"; label = "إجهاد بصري";
                if (M("zoom") >= 1.25) flags.Add($"تكبير الشاشة {M("zoom"):F2}x");
                if (M("fontPx") >= 20) flags.Add($"تكبير الخط إلى {M("fontPx"):F0}px");
                if (M("distanceCm") > 0 && M("distanceCm") < 35) flags.Add($"اقتراب من الشاشة ({M("distanceCm"):F0} سم)");
                if (M("blinkRate") > 0 && M("blinkRate") < 10) flags.Add($"انخفاض الرمش ({M("blinkRate"):F0}/د)");
                if (M("squint") >= 0.35) flags.Add("تضييق متكرر للعينين");
                if (M("gazeOff") >= 0.4) flags.Add("شرود نظر متكرر");
            }
            else if (kind == "voice")
            {
                sym = "voice_change"; label = "تغيّر في الصوت";
                if (M("rmsDb") < -38) flags.Add($"صوت ضعيف ({M("rmsDb"):F0} dB)");
                if (M("jitter") > 0.05) flags.Add($"تذبذب في النغمة ({M("jitter") * 100:F1}%)");
                if (M("pauseRatio") > 0.45) flags.Add($"توقفات كثيرة ({M("pauseRatio") * 100:F0}% من الوقت)");
                if (M("wordErrorRate") > 0.35) flags.Add($"نطق غير واضح ({M("wordErrorRate") * 100:F0}% أخطاء)");
                if (M("breaks") >= 4) flags.Add($"انقطاع الصوت {M("breaks"):F0} مرات");
            }
            else if (kind == "typing")
            {
                sym = "typing_error"; label = "أخطاء كتابة";
                if (M("errorRate") > 0.12) flags.Add($"أخطاء كتابة {M("errorRate") * 100:F0}%");
                if (M("backspaceRate") > 0.25) flags.Add($"تصحيح متكرر {M("backspaceRate") * 100:F0}%");
                if (M("intervalCv") > 0.7) flags.Add("تفاوت إيقاع الكتابة");
                if (M("wpm") > 0 && M("wpm") < 15) flags.Add($"بطء في الكتابة ({M("wpm"):F0} كلمة/د)");
            }
            else
            {
                sym = "mood_low"; label = "مؤشرات المزاج";
                var score = M("depressionScore");
                if (score >= 60) flags.Add($"مؤشر اكتئاب مرتفع ({score:F0}/100)");
                else if (score >= 35) flags.Add($"مؤشر اكتئاب متوسط ({score:F0}/100)");
                if (M("frown") > 0.18) flags.Add("تعبير حزين متكرر");
                if (M("brow") > 0.25) flags.Add("تقاطع الحواجب وشدها");
                if (M("smile") < 0.06 && M("seconds") > 15) flags.Add("قلة الابتسام أثناء الجلسة");
                if (M("gazeDown") > 0.45) flags.Add("نظر لأسفل متكرر");
                if (M("blinkRate") > 0 && M("blinkRate") < 8) flags.Add($"رمش منخفض ({M("blinkRate"):F0}/د)");
                if (M("flatAffect") > 0.7) flags.Add("جمود في تعبير الوجه");
                if (M("headDown") > 0.5) flags.Add("ميل الرأس لأسفل معظم الوقت");
            }

            var sev = flags.Count >= 3 ? 3 : flags.Count == 2 ? 2 : 1;
            var level = flags.Count >= 3 ? "urgent" : flags.Count == 2 ? "watch" : "nominal";
            var summary = flags.Count > 0 ? $"{label} — {string.Join("، ", flags)}" : $"{label}: القياسات طبيعية";
            var st = new SensorState
            {
                CrewId = crewId, Kind = kind, Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Metrics = new Dictionary<string, double>(metrics), Flags = flags,
                Level = flags.Count >= 2 ? "watch" : "nominal", Note = summary
            };
            Sensors.Insert(0, st);
            if (Sensors.Count > 60) Sensors.RemoveRange(60, Sensors.Count - 60);
            PersistSensor(st);

            if (flags.Count > 0)
            {
                AddSymptomInternal(c, sym, sev, "sensor", summary);
                if (c.Status is "urgent" or "critical")
                {
                    var d = DomainCatalog.Get(c.Risk.Top);
                    PushAlert(c, d.Id, c.Status, $"{c.Name}: {d.Name} {c.Risk.Scores[d.Id]}",
                        $"{LevelAr(c.Status)} — {summary}");
                }
            }
            return st.Clone();
        }
    }

    // ---------------------------------------------------------------- telemetry
    public CrewState? IngestTelemetry(string crewId, double? hr, double? spo2, double? temp,
        double? sbp, double? dbp, double? rr, double? co2, double? dose)
    {
        lock (_gate)
        {
            var c = Crew.FirstOrDefault(x => x.Id == crewId);
            if (c == null) return null;
            var v = c.Vitals;
            if (hr.HasValue) v.Hr = hr.Value;
            if (spo2.HasValue) v.Spo2 = spo2.Value;
            if (temp.HasValue) v.Temp = temp.Value;
            if (sbp.HasValue) v.Sbp = sbp.Value;
            if (dbp.HasValue) v.Dbp = dbp.Value;
            if (rr.HasValue) v.Rr = rr.Value;
            if (co2.HasValue) c.Co2 = co2.Value;
            if (dose.HasValue) c.Dose = dose.Value;
            c.Source = "device";
            c.LastPacket = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            c.Risk = RiskEngine.Compute(c, Mission.Day, Storm);
            c.Status = c.Risk.Status;
            PersistTelemetry(c);
            return c;
        }
    }

    // ---------------------------------------------------------------- reports
    public ReportDto CreateReport(string scope)
    {
        lock (_gate)
        {
            var crew = scope is "mission" or "" ? Crew : Crew.Where(c => c.Id == scope).ToList();
            if (crew.Count == 0) throw new ArgumentException("crew not found");
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var rep = new ReportDto
            {
                Id = $"r{now}",
                Scope = scope is "mission" or "" ? "mission" : scope,
                Title = crew.Count == 1 ? $"تقرير صحي — {crew[0].Name}" : "تقرير المهمة اليومي — ARES-3",
                Ts = now,
                Day = Mission.Day,
                Status = "draft",
                Summary = new ReportSummaryDto
                {
                    Total = crew.Count,
                    AtRisk = crew.Where(c => c.Status is "urgent" or "critical").Select(c => new ReportRiskDto
                    {
                        Id = c.Id, Name = c.Name, Status = c.Status, Top = c.Risk.Top,
                        Score = c.Risk.Scores[c.Risk.Top]
                    }).ToList(),
                    AvgSleep = Math.Round(crew.Average(c => c.Sleep), 1),
                    MaxDose = (int)Math.Round(crew.Max(c => c.Dose)),
                    Alerts = Alerts.Count(a => !a.Ack && crew.Any(c => c.Id == a.CrewId))
                }
            };

            foreach (var c in crew)
            {
                var top = c.Risk.Scores.OrderByDescending(kv => kv.Value).Take(3)
                    .Select(kv => new ReportTopRiskDto { Name = DomainCatalog.Get(kv.Key).Name, Score = kv.Value });
                rep.Crew.Add(new ReportCrewDto
                {
                    Id = c.Id, Name = c.Name, Role = c.Role, Status = c.Status,
                    Vitals = c.Vitals.Clone(), Sleep = c.Sleep, Dose = (int)Math.Round(c.Dose),
                    Top = top.ToList(),
                    Symptoms = c.Symptoms.Where(s => now - s.Ts < 24 * 3600_000L).Take(6)
                        .Select(s => new ReportSymptomDto { Label = SymptomCatalog.All[s.Id], Sev = s.Sev, Src = s.Src }).ToList(),
                    Recs = RecommendationCatalog.All[c.Risk.Top]
                });
            }

            Reports.Insert(0, rep);
            if (Reports.Count > 20) Reports.RemoveAt(Reports.Count - 1);
            PersistReport(rep);
            return rep;
        }
    }

    public ReportDto? SendReport(string id)
    {
        lock (_gate)
        {
            var r = Reports.FirstOrDefault(x => x.Id == id);
            if (r == null || r.Status != "draft") return null;
            r.Status = "sent";
            r.SentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            r.Eta = r.SentAt + (long)(CommDelay * 1000);
            PersistReport(r);
            return r;
        }
    }

    public ReportDto? AckReport(string id)
    {
        lock (_gate)
        {
            var r = Reports.FirstOrDefault(x => x.Id == id);
            if (r == null || r.Status != "received") return null;
            r.Status = "ack";
            r.AckedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            r.Reply = "استلمنا التقرير. نوصي بتثبيت المريض ومتابعة العلامات كل ساعة، وتجهيز خطة الإخلاء لو ظهرت أعراض عصبية جديدة. جلسة مراجعة في نافذة الاتصال القادمة.";
            PersistReport(r);
            return r;
        }
    }

    private void CheckDelivery()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var r in Reports.Where(r => r.Status == "sent" && r.Eta.HasValue && now >= r.Eta))
        {
            r.Status = "received";
            r.ReceivedAt = now;
            PersistReport(r);
        }
    }

    // ---------------------------------------------------------------- scenarios
    public void Scenario(string name)
    {
        lock (_gate)
        {
            CrewState C(string id) => Crew.First(c => c.Id == id);
            switch (name)
            {
                case "dcs":
                    C("c4").Eva = 10;
                    AddSymptomInternal(C("c4"), "numbness", 2, "manual", "بعد EVA بساعتين");
                    AddSymptomInternal(C("c4"), "joint_pain", 2, "manual", "ألم في الكتف والركبة");
                    AddSymptomInternal(C("c4"), "weakness", 2, "manual", "ضعف في اليد اليمنى");
                    AddSymptomInternal(C("c4"), "dizziness", 1, "manual", "");
                    PushAlert(C("c4"), "dcs", "critical", "عمر الشريف: اشتباه DCS عصبي",
                        "تنميل وألم مفاصل بعد EVA — أكسجين 100% وتجهيز غرفة الضغط");
                    break;
                case "co2":
                    C("c3").Co2 = 5200;
                    C("c3").Vitals.Spo2 = 92.4;
                    AddSymptomInternal(C("c3"), "headache", 2, "auto", "ارتفاع CO2: 5200 ppm");
                    AddSymptomInternal(C("c3"), "confusion", 2, "manual", "صعوبة تركيز في وحدة المختبر");
                    PushAlert(C("c3"), "resp", "critical", "لينا فارس: ارتفاع CO2",
                        "CO2 تجاوز 5000 ppm — تهوية فورية وإخلاء الوحدة");
                    break;
                case "sans":
                    AddSymptomInternal(C("c3"), "vision_change", 2, "manual", "صعوبة قراءة الشاشة القريبة");
                    AddSymptomInternal(C("c3"), "tinnitus", 2, "manual", "طنين مستمر");
                    PushAlert(C("c3"), "sans", "urgent", "لينا فارس: تغير نظر وطنين",
                        "متابعة SANS — فحص نظر ومقارنة بخط الأساس");
                    break;
                case "radiation":
                    Storm = true;
                    foreach (var c in Crew)
                    {
                        c.Dose += 35;
                        c.Risk = RiskEngine.Compute(c, Mission.Day, Storm);
                        c.Status = c.Risk.Status;
                    }
                    AddSymptomInternal(C("c1"), "fatigue", 1, "auto", "عاصفة إشعاعية — جرعة +35 mSv");
                    PushAlert(C("c1"), "radiation", "critical", "عاصفة إشعاعية: جرعة +35 mSv",
                        "انتقال الطاقم للملجأ المحمي وتقليل EVA");
                    break;
                case "sleep":
                    C("c1").Sleep = 4.2;
                    AddSymptomInternal(C("c1"), "insomnia", 3, "manual", "أرق حاد بسبب جدول المهام");
                    AddSymptomInternal(C("c1"), "fatigue", 2, "manual", "");
                    PushAlert(C("c1"), "behavioral", "urgent", "سارة منصور: ديون نوم",
                        "نوم 4.2 ساعة — تعديل الجدول ودعم سلوكي");
                    break;
                case "reset":
                    Reset();
                    break;
                default:
                    throw new ArgumentException("unknown scenario");
            }
            Evaluate();
        }
    }

    private void Reset()
    {
        Mission = new MissionDto();
        Crew = SeedCrew();
        var initial = new Dictionary<string, VitalsState>
        {
            ["c1"] = new() { Hr = 74, Spo2 = 98, Temp = 36.8, Sbp = 118, Dbp = 76, Rr = 14 },
            ["c2"] = new() { Hr = 68, Spo2 = 99, Temp = 36.6, Sbp = 112, Dbp = 72, Rr = 13 },
            ["c3"] = new() { Hr = 80, Spo2 = 97, Temp = 36.9, Sbp = 121, Dbp = 79, Rr = 15 },
            ["c4"] = new() { Hr = 78, Spo2 = 98, Temp = 36.7, Sbp = 116, Dbp = 74, Rr = 14 }
        };
        foreach (var c in Crew) c.Vitals = initial[c.Id];
        Alerts.Clear();
        Reports.Clear();
        Sensors.Clear();
        Storm = false;
        _autoSeen.Clear();
        foreach (var c in Crew)
        {
            c.Risk = RiskEngine.Compute(c, Mission.Day, Storm);
            c.Status = c.Risk.Status;
            c.Trend = Enumerable.Range(0, 50).Select(_ => Math.Max(0, c.Risk.Max - 10 + Random.Shared.Next(-3, 4))).ToList();
        }
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Reports.RemoveRange(db.Reports);
        db.Symptoms.RemoveRange(db.Symptoms);
        db.Sensors.RemoveRange(db.Sensors);
        db.SaveChanges();
    }

    // ---------------------------------------------------------------- snapshot
    public SnapshotDto Snapshot()
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var crew = Crew.Select(c =>
            {
                var copy = c.Clone();
                copy.LastSeen = Math.Round((now - c.LastPacket) / 1000.0, 1);
                return copy;
            }).ToList();
            return new SnapshotDto
            {
                Ts = now,
                Mission = new MissionDto
                {
                    Name = Mission.Name, Destination = Mission.Destination,
                    Day = Mission.Day, Minutes = Mission.Minutes, Storm = Mission.Storm
                },
                Crew = crew,
                Alerts = Alerts.Select(a => a.Clone()).ToList(),
                Stats = new StatsDto
                {
                    AvgSleep = Math.Round(crew.Average(c => c.Sleep), 1),
                    MaxDose = (int)Math.Round(crew.Max(c => c.Dose)),
                    ActiveAlerts = Alerts.Count(a => !a.Ack),
                    Ready = (int)Math.Round(100 - crew.Average(c => c.Risk.Max))
                },
                Simulate = Simulate,
                Auto = Auto,
                CommDelay = CommDelay,
                Sensors = Sensors.Take(30).Select(x => x.Clone()).ToList(),
                Storage = DbOk ? DbProvider : "InMemory"
            };
        }
    }

    // ---------------------------------------------------------------- persistence
    private void PersistSensor(SensorState st)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Sensors.Add(new SensorEntity
            {
                Id = $"s{st.Ts}{Random.Shared.Next(0, 9999)}",
                CrewId = st.CrewId, Kind = st.Kind, Ts = st.Ts, Level = st.Level, Note = st.Note,
                FlagsJson = JsonSerializer.Serialize(st.Flags, JsonOptions),
                MetricsJson = JsonSerializer.Serialize(st.Metrics, JsonOptions)
            });
            db.SaveChanges();
        }
        catch (Exception e) { _logger.LogWarning(e, "persist sensor failed"); }
    }

    private void PersistSymptom(string crewId, SymptomState s)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Symptoms.Add(new SymptomEntity
            {
                CrewId = crewId, Code = s.Id, Severity = s.Sev, Source = s.Src, Note = s.Note, Ts = s.Ts
            });
            db.SaveChanges();
        }
        catch (Exception e) { _logger.LogWarning(e, "persist symptom failed"); }
    }

    private void PersistTelemetry(CrewState c)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Telemetry.Add(new TelemetryEntity
            {
                CrewId = c.Id, Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Hr = c.Vitals.Hr, Spo2 = c.Vitals.Spo2, Temp = c.Vitals.Temp,
                Sbp = c.Vitals.Sbp, Dbp = c.Vitals.Dbp, Rr = c.Vitals.Rr,
                Co2 = c.Co2, Dose = c.Dose, Source = c.Source
            });
            db.SaveChanges();
        }
        catch (Exception e) { _logger.LogWarning(e, "persist telemetry failed"); }
    }

    private void PersistTelemetry()
    {
        foreach (var c in Crew) PersistTelemetry(c);
    }

    private void PersistReport(ReportDto r)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = db.Reports.FirstOrDefault(x => x.Id == r.Id);
            var json = JsonSerializer.Serialize(r, JsonOptions);
            if (entity == null)
                db.Reports.Add(new ReportEntity { Id = r.Id, Ts = r.Ts, Scope = r.Scope, Status = r.Status, PayloadJson = json });
            else
            {
                entity.Status = r.Status;
                entity.PayloadJson = json;
            }
            db.SaveChanges();
        }
        catch (Exception e) { _logger.LogWarning(e, "persist report failed"); }
    }
}

using OrbitHealth.Api.Models;

namespace OrbitHealth.Api.Services;

public static class DomainCatalog
{
    public record Domain(string Id, string Name, string Short, string Color);

    public static readonly Domain[] All =
    [
        new("dcs", "تخفيف الضغط", "DCS", "#ff8a3d"),
        new("resp", "تنفسي و CO2", "تنفسي", "#5aa9ff"),
        new("sans", "عين ومخ", "SANS", "#c084fc"),
        new("msk", "عظام وعضلات", "عظام", "#fbbf24"),
        new("cardio", "قلب وأوعية", "قلب", "#ff5d6c"),
        new("immune", "مناعة وعدوى", "مناعة", "#4ade80"),
        new("behavioral", "نفسي ونوم", "نفسي", "#38bdf8"),
        new("radiation", "إشعاع", "إشعاع", "#a3e635")
    ];

    public static Domain Get(string id) => All.First(d => d.Id == id);
}

public static class SymptomCatalog
{
    public static readonly Dictionary<string, string> All = new()
    {
        ["headache"] = "صداع", ["dizziness"] = "دوخة", ["confusion"] = "تشوش ذهني",
        ["vision_change"] = "تغيّر في النظر", ["numbness"] = "تنميل", ["weakness"] = "ضعف في طرف",
        ["tinnitus"] = "طنين", ["breathlessness"] = "ضيق نفس", ["chest_pain"] = "ألم في الصدر",
        ["cough"] = "كحة", ["abdominal_pain"] = "ألم في البطن", ["nausea"] = "غثيان",
        ["vomiting"] = "قيء", ["diarrhea"] = "إسهال", ["appetite_loss"] = "فقدان الشهية",
        ["rash"] = "طفح جلدي", ["itching"] = "حكة", ["swelling_face"] = "تورم الوجه",
        ["joint_pain"] = "ألم مفاصل", ["back_pain"] = "ألم ظهر", ["limb_pain"] = "ألم طرف",
        ["fracture"] = "اشتباه كسر", ["flank_pain"] = "ألم جانب", ["blood_urine"] = "دم في البول",
        ["fever"] = "حرارة", ["fatigue"] = "إجهاد", ["insomnia"] = "أرق", ["mood_low"] = "انخفاض مزاج",
        ["vision_strain"] = "إجهاد بصري / صعوبة قراءة", ["voice_change"] = "تغيّر في الصوت", ["typing_error"] = "أخطاء كتابة / رعشة يد"
    };
}

public static class RecommendationCatalog
{
    public static readonly Dictionary<string, string[]> All = new()
    {
        ["dcs"] = ["أوقف النشاط وأكسجين 100%", "بلّغ الأرض وجهّز غرفة الضغط", "سجّل وقت بداية الأعراض"],
        ["resp"] = ["افحص التهوية وفلاتر CO2", "أكسجين ومراقبة SpO2", "قلل المجهود وانقل لهواء أنقى"],
        ["sans"] = ["فحص نظر ومقارنة بخط الأساس", "سجّل الصداع والطنين يوميًا", "بلّغ الأرض لمتابعة ضغط السائل داخل الجمجمة"],
        ["msk"] = ["تمرين مقاومة مع تحميل تدريجي", "بروتين وكالسيوم وفيتامين د", "فحص كثافة العظام بعد المهمة"],
        ["cardio"] = ["مراقبة النبض والضغط كل ساعة", "أكسجين وراحة", "جهّز خطة إخلاء لو الألم مستمر"],
        ["immune"] = ["عزل المريض ومراقبة الحرارة", "سوائل وتغذية كافية", "مسح ميكروبيولوجي للوحدة"],
        ["behavioral"] = ["جدولة نوم ثابتة وإضاءة مناسبة", "اتصال بالعائلة وتقليل الحمل", "دعم سلوكي من الأرض"],
        ["radiation"] = ["انقل الطاقم للملجأ المحمي", "راقب الجرعة التراكمية", "قلل زمن EVA غير الضروري"]
    };
}

public static class RiskEngine
{
    public static RiskState Compute(CrewState crew, int missionDay, bool storm)
    {
        var v = crew.Vitals;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        double Sym(string id) => crew.Symptoms
            .Where(s => s.Id == id && now - s.Ts < 24 * 3600_000L)
            .Sum(s => s.Sev);

        var r = new Dictionary<string, double>
        {
            ["dcs"] = 4 + Sym("joint_pain") * 7 + Sym("numbness") * 9 + Sym("weakness") * 9
                      + Sym("rash") * 5 + Sym("dizziness") * 2 + (crew.Eva >= 6 ? 14 : 0),
            ["resp"] = 4 + Sym("breathlessness") * 8 + Sym("confusion") * 8 + Sym("headache") * 2
                       + Math.Max(0, (crew.Co2 - 900) / 25) + Math.Max(0, (98 - v.Spo2) * 9)
                       + Sym("voice_change") * 3,
            ["sans"] = 3 + Sym("vision_change") * 11 + Sym("tinnitus") * 7 + Sym("headache") * 2
                       + Sym("vision_strain") * 8 + Sym("typing_error") * 2
                       + Math.Min(20, missionDay * 0.08),
            ["msk"] = 8 + Sym("joint_pain") * 4 + Sym("back_pain") * 5 + Sym("limb_pain") * 4
                      + Sym("fracture") * 10 + Sym("typing_error") * 5 + Math.Min(24, missionDay * 0.12) + Math.Max(0, (150 - crew.Exercise) / 4),
            ["cardio"] = 4 + Sym("chest_pain") * 13 + Sym("breathlessness") * 4 + Sym("dizziness") * 3
                         + Math.Max(0, (v.Hr - 92) * 0.7) + (v.Sbp < 100 ? 10 : 0) + (v.Sbp > 142 ? 8 : 0),
            ["immune"] = 3 + Math.Max(0, (v.Temp - 37.8) * 40) + Sym("fever") * 9 + Sym("rash") * 4
                         + Sym("cough") * 4 + Sym("diarrhea") * 4,
            ["behavioral"] = 8 + Math.Max(0, (7 - crew.Sleep) * 9) + Sym("insomnia") * 6 + Sym("fatigue") * 3
                             + Sym("mood_low") * 7 + Sym("voice_change") * 2 + Math.Min(14, missionDay * 0.05),
            ["radiation"] = Math.Min(100, crew.Dose / 600 * 100) + (storm ? 18 : 0)
        };

        var scores = new Dictionary<string, int>();
        var max = 0;
        var top = "msk";
        foreach (var d in DomainCatalog.All)
        {
            var s = (int)Math.Clamp(Math.Round(r[d.Id]), 0, 100);
            scores[d.Id] = s;
            if (s > max) { max = s; top = d.Id; }
        }

        var status = max >= 70 ? "critical" : max >= 50 ? "urgent" : max >= 35 ? "watch" : "nominal";
        return new RiskState { Scores = scores, Max = max, Top = top, Status = status };
    }
}

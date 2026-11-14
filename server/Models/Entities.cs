namespace OrbitHealth.Api.Models;

public class CrewEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Color { get; set; } = "#ff8a3d";
    public int Age { get; set; }
    public double SleepHours { get; set; }
    public double ExerciseMinutes { get; set; }
    public double EvaHours { get; set; }
    public double RadiationDose { get; set; }
    public double Co2Ppm { get; set; }
}

public class SymptomEntity
{
    public long Id { get; set; }
    public string CrewId { get; set; } = "";
    public string Code { get; set; } = "";
    public int Severity { get; set; }
    public string Source { get; set; } = "manual";
    public string Note { get; set; } = "";
    public long Ts { get; set; }
}

public class TelemetryEntity
{
    public long Id { get; set; }
    public string CrewId { get; set; } = "";
    public long Ts { get; set; }
    public double Hr { get; set; }
    public double Spo2 { get; set; }
    public double Temp { get; set; }
    public double Sbp { get; set; }
    public double Dbp { get; set; }
    public double Rr { get; set; }
    public double Co2 { get; set; }
    public double Dose { get; set; }
    public string Source { get; set; } = "sim";
}

public class SensorEntity
{
    public string Id { get; set; } = "";
    public string CrewId { get; set; } = "";
    public string Kind { get; set; } = "";
    public long Ts { get; set; }
    public string Level { get; set; } = "nominal";
    public string FlagsJson { get; set; } = "[]";
    public string MetricsJson { get; set; } = "{}";
    public string Note { get; set; } = "";
}

public class ReportEntity
{
    public string Id { get; set; } = "";
    public long Ts { get; set; }
    public string Scope { get; set; } = "mission";
    public string Status { get; set; } = "draft";
    public string PayloadJson { get; set; } = "{}";
}

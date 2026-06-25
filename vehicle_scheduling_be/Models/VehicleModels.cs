namespace vehicle_scheduling_be.Models;

public class Depot   { public int ID { get; set; } public int MechanicHours { get; set; } }
public class Vehicle { public string TaskID { get; set; } = string.Empty; public int Duration { get; set; } public int Impact { get; set; } }

public class ScheduleResult {
    public int DepotID { get; set; }
    public int MechanicHoursAvailable { get; set; }
    public int TotalHoursUsed { get; set; }
    public int TotalImpactScore { get; set; }
    public List<Vehicle> SelectedTasks { get; set; } = new();
}

public class SchedulerResponse {
    public List<ScheduleResult> Results { get; set; } = new();
}

public class DepotResponse { public List<Depot> Depots { get; set; } = new(); }
public class VehicleResponse { public List<Vehicle> Vehicles { get; set; } = new(); }
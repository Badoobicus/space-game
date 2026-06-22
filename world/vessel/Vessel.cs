public class Vessel : IOrbitable
{
    public string VesselId { get; set; }
    public Trajectory Trajectory { get; set; }
    public Orbit Orbit { get; set; }
    public Vector3d Position { get; set; }
    public Vector3d Velocity { get; set; }
}

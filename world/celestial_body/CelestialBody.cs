public class CelestialBody : IOrbitable
{
    public string CelestialBodyId { get; set; }
    public double Mass { get; set; }
    public Orbit Orbit { get; set; }
    public OrbitSolver OrbitSolver { get; set; }
    public double Radius { get; set; }
    public double SoiRadius { get; set; }
    public Vector3d Position { get; set; }
    public Vector3d Velocity { get; set; }
}

public interface IOrbitable
{
    public Orbit Orbit { get; set; }
    public OrbitSolver OrbitSolver { get; set; }
    public Vector3d Position { get; set; }
    public Vector3d Velocity { get; set; }
}

using Godot;
using System;

public partial class UserInterface : Node
{
    [Export]
    private World _world;

    [Export]
    private Label _timeLabel;

    [Export]
    private Label _timeWarpLabel;

    [Export]
    private Label _fpsLabel;

    public override void _Ready()
    {
        _world.TimeChanged += _OnTimeChanged;
        _world.TimeWarpChange += _OnTimeWarpChanged;
    }

    public override void _Process(double delta)
    {
        _fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }

    private void _OnTimeChanged(double time)
    {
        double secPerMinute = 60;
        double secPerHour = secPerMinute * 60;
        double secPerDay = secPerHour * 24;
        double secPerYear = secPerDay * 365;

        double workingTime = time;
        int years = (int)(workingTime / secPerYear);
        workingTime -= years * secPerYear;
        int days = (int)(workingTime / secPerDay);
        workingTime -= days * secPerDay;
        int hours = (int)(workingTime / secPerHour);
        workingTime -= hours * secPerHour;
        int minutes = (int)(workingTime / secPerMinute);
        workingTime -= minutes * secPerMinute;
        int seconds = (int)workingTime;

        _timeLabel.Text = $"Year {years + 1}, Day {days + 1} {hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    private void _OnTimeWarpChanged(double timeWarp)
    {
        _timeWarpLabel.Text = (timeWarp < 1 ? "1/" + Math.Round(1 / timeWarp) : timeWarp) + "x";
    }
}

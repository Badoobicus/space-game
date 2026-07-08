using System;
using Godot;

public partial class UserInterface : Node
{
    [Export]
    private Universe _universe;

    [Export]
    private Label _timeLabel;

    [Export]
    private Label _timeWarpLabel;

    [Export]
    private Label _fpsLabel;

    [Export]
    private Button _resetTimeButton;

    [Export]
    private Button _warpToEpochShiftButton;

    public override void _Ready()
    {
        _resetTimeButton.Pressed += _OnResetTimeButtonPressed;
        _warpToEpochShiftButton.Pressed += _OnWarpToEpochShiftButtonPressed;

        _universe.TimeChanged += _OnTimeChanged;
        _universe.TimeWarpChange += _OnTimeWarpChanged;
    }

    public override void _Process(double delta)
    {
        _fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }

    private void _OnResetTimeButtonPressed()
    {
        _universe.ResetTime();
    }

    private void _OnWarpToEpochShiftButtonPressed()
    {
        _universe.SetTargetWarpTime(new UniverseTime(_universe.GetTime().MajorUnits + 1, -5));
    }

    private void _OnTimeChanged(long majorUnits, double seconds)
    {
        double secPerMinute = 60;
        double secPerHour = secPerMinute * 60;
        double secPerDay = secPerHour * 24;
        double secPerYear = secPerDay * 365;

        double majorUnitsToYears = UniverseTime.SecondsPerMajorUnit / secPerYear;
        double yearsToMajorUnits = secPerYear / UniverseTime.SecondsPerMajorUnit;

        long baseYears = (long)(majorUnits * majorUnitsToYears);
        double workingTime =
            (majorUnits - baseYears * yearsToMajorUnits) * UniverseTime.SecondsPerMajorUnit
            + seconds;
        long years = (long)(workingTime / secPerYear);
        workingTime -= years * secPerYear;
        long days = (long)(workingTime / secPerDay);
        workingTime -= days * secPerDay;
        long hours = (long)(workingTime / secPerHour);
        workingTime -= hours * secPerHour;
        long minutes = (long)(workingTime / secPerMinute);
        workingTime -= minutes * secPerMinute;
        long s = (long)workingTime;

        years += baseYears;

        _timeLabel.Text = $"Year {years + 1}, Day {days + 1} {hours:D2}:{minutes:D2}:{s:D2}";
    }

    private void _OnTimeWarpChanged(double timeWarp)
    {
        _timeWarpLabel.Text = (timeWarp < 1 ? "1/" + Math.Round(1 / timeWarp) : timeWarp) + "x";
    }
}

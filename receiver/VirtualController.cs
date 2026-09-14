using System;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

public sealed class VirtualController : IDisposable
{
    public readonly ViGEmClient client;
    private readonly IXbox360Controller controller;
    public VirtualController()
    {
        Console.WriteLine();
        Console.WriteLine("==========================");
        Console.WriteLine("VIRTUAL XBOX CONTROLLER");
        Console.WriteLine("==========================");

        Console.WriteLine("Membuat ViGEmclient...");

        client = new ViGEmClient();

        Console.WriteLine("ViGEmclient berhasil dibuat");

        Console.WriteLine("Membuat Xbox 360 controller...");

        controller = client.CreateXbox360Controller();

        Console.WriteLine("Xbox 360 controller berhasil dibuat");
        Console.WriteLine("Connecting...");

        controller.Connect();

        Console.WriteLine("Virtual Xbox 360 Controller Connected");
        Console.WriteLine();
    }

    // =======================================
    // button
    // =======================================

    public void setButton(string button, bool pressed)
    {
        switch (button.ToUpperInvariant())
        {
            case "A":
                controller.SetButtonState(Xbox360Button.A, pressed);
                break;

            case "B":
                controller.SetButtonState(Xbox360Button.B, pressed);
                break;

            case "X":
                controller.SetButtonState(Xbox360Button.X, pressed);
                break;

            case "Y":
                controller.SetButtonState(Xbox360Button.Y, pressed);
                break;

            case "LB":
                controller.SetButtonState(Xbox360Button.LeftShoulder, pressed);
                break;

            case "RB":
                controller.SetButtonState(Xbox360Button.RightShoulder, pressed);
                break;

            case "BACK":
                controller.SetButtonState(Xbox360Button.Back, pressed);
                break;

            case "START":
                controller.SetButtonState(Xbox360Button.Start, pressed);
                break;

            case "LS":
                controller.SetButtonState(Xbox360Button.LeftThumb, pressed);
                break;

            case "RS":
                controller.SetButtonState(Xbox360Button.RightThumb, pressed);
                break;
        }
    }

    //===========================================
    // LEFT STICK
    //===========================================

    public void SetLeftStick(
        short x,
        short y)
    {
        controller.SetAxisValue(
            Xbox360Axis.LeftThumbX,
            x
        );

        controller.SetAxisValue(
            Xbox360Axis.LeftThumbY,
            y
        );
    }

    // ============================================================
    // RIGHT STICK
    // ============================================================

    public void SetRightStick(
        short x,
        short y)
    {
        controller.SetAxisValue(
            Xbox360Axis.RightThumbX,
            x
        );

        controller.SetAxisValue(
            Xbox360Axis.RightThumbY,
            y
        );
    }

    // ============================================================
    // LEFT TRIGGER
    // ============================================================

    public void SetLeftTrigger(
        byte value)
    {
        controller.SetSliderValue(
            Xbox360Slider.LeftTrigger,
            value
        );
    }

    // ============================================================
    // RIGHT TRIGGER
    // ============================================================

    public void SetRightTrigger(
        byte value)
    {
        controller.SetSliderValue(
            Xbox360Slider.RightTrigger,
            value
        );
    }

    // ============================================================
    // D-PAD
    // ============================================================

    public void SetDPad(
        bool up,
        bool down,
        bool left,
        bool right)
    {
        controller.SetButtonState(
            Xbox360Button.Up,
            up
        );
        controller.SetButtonState(
            Xbox360Button.Down,
            down
        );
        controller.SetButtonState(
            Xbox360Button.Left,
            left
        );
        controller.SetButtonState(
            Xbox360Button.Right,
            right
        );
    }

    // ============================================================
    // RESET
    // ============================================================

    public void Reset()
    {
        setButton("A", false);
        setButton("B", false);
        setButton("X", false);
        setButton("Y", false);

        setButton("LB", false);
        setButton("RB", false);

        setButton("BACK", false);
        setButton("START", false);

        setButton("LS", false);
        setButton("RS", false);

        SetLeftTrigger(0);
        SetRightTrigger(0);

        SetLeftStick(0, 0);
        SetRightStick(0, 0);

        SetDPad(
            false,
            false,
            false,
            false
        );
    }

    // ============================================================
    // DISPOSE
    // ============================================================

    public void Dispose()
    {
        try
        {
            Reset();
        }
        catch
        {
        }

        try
        {
            controller.Disconnect();
        }
        catch
        {
        }

        try
        {
            client.Dispose();
        }
        catch
        {
        }

        Console.WriteLine();
        Console.WriteLine(
            "Virtual Xbox Controller disconnected."
        );
    }

}
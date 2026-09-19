using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ZZZController;

public partial class Form1 : Form
{
    private Process? receiverProcess;

    private readonly Color BackgroundColor = Color.FromArgb(18, 18, 18);
    private readonly Color PanelColor = Color.FromArgb(28, 28, 28);
    private readonly Color ButtonColor = Color.FromArgb(40, 40, 40);
    private readonly Color TextColor = Color.White;
    private readonly Color MutedTextColor = Color.FromArgb(160, 160, 160);

    private Label statusLabel = null!;
    private Label phoneStatusLabel = null!;
    private Label virtualStatusLabel = null!;
    private Button receiverButton = null!;

    public Form1()
    {
        InitializeComponent();
        SetupInterface();
    }

    private void SetupInterface()
    {
        Text = "ZZZ Controller";
        ClientSize = new Size(900, 560);
        MinimumSize = new Size(700, 450);
        BackColor = BackgroundColor;
        ForeColor = TextColor;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        StartPosition = FormStartPosition.CenterScreen;

        // =========================
        // TITLE
        // =========================

        var title = new Label
        {
            Text = "ZZZ CONTROLLER",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = TextColor,
            AutoSize = true,
            Location = new Point(40, 35)
        };

        Controls.Add(title);

        var subtitle = new Label
        {
            Text = "USB Phone Controller",
            Font = new Font("Segoe UI", 10),
            ForeColor = MutedTextColor,
            AutoSize = true,
            Location = new Point(43, 75)
        };

        Controls.Add(subtitle);

        // =========================
        // STATUS PANEL
        // =========================

        var statusPanel = new Panel
        {
            BackColor = PanelColor,
            Location = new Point(40, 125),
            Size = new Size(820, 160)
        };

        Controls.Add(statusPanel);

        statusLabel = new Label
        {
            Text = "●  RECEIVER STOPPED",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(220, 80, 80),
            AutoSize = true,
            Location = new Point(25, 20)
        };

        statusPanel.Controls.Add(statusLabel);

        phoneStatusLabel = new Label
        {
            Text = "Phone                 Disconnected",
            Font = new Font("Segoe UI", 11),
            ForeColor = MutedTextColor,
            AutoSize = true,
            Location = new Point(25, 75)
        };

        statusPanel.Controls.Add(phoneStatusLabel);

        virtualStatusLabel = new Label
        {
            Text = "Virtual Controller    Disconnected",
            Font = new Font("Segoe UI", 11),
            ForeColor = MutedTextColor,
            AutoSize = true,
            Location = new Point(25, 110)
        };

        statusPanel.Controls.Add(virtualStatusLabel);

        // =========================
        // RECEIVER BUTTON
        // =========================

        receiverButton = new Button
        {
            Text = "▶  JALANKAN RECEIVER",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = TextColor,
            BackColor = ButtonColor,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(300, 60),
            Location = new Point(300, 330),
            Cursor = Cursors.Hand
        };

        receiverButton.FlatAppearance.BorderSize = 0;
        receiverButton.Click += ReceiverButton_Click;

        Controls.Add(receiverButton);

        // =========================
        // FOOTER
        // =========================

        var footer = new Label
        {
            Text = "USB connection • AOA • Virtual Xbox Controller",
            Font = new Font("Segoe UI", 9),
            ForeColor = MutedTextColor,
            AutoSize = true,
            Location = new Point(40, 505)
        };

        Controls.Add(footer);

        FormClosing += Form1_FormClosing;
    }

    private void ReceiverButton_Click(object? sender, EventArgs e)
    {
        if (receiverProcess == null || receiverProcess.HasExited)
        {
            StartReceiver();
        }
        else
        {
            StopReceiver();
        }
    }

    private void StartReceiver()
    {
        try
        {
            string receiverPath = @"D:\gabut\controller\zzz-controller\ZZZController\receiver\receiver.exe";

            if (!File.Exists(receiverPath))
            {
                MessageBox.Show(
                    $"receiver.exe tidak ditemukan:\n\n{receiverPath}",
                    "ZZZ Controller",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            receiverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = receiverPath,
                    WorkingDirectory = Path.GetDirectoryName(receiverPath)!,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            receiverProcess.EnableRaisingEvents = true;

            receiverProcess.Exited += (_, _) =>
            {
                if (IsDisposed)
                    return;

                BeginInvoke(() =>
                {
                    SetReceiverStopped();
                });
            };

            receiverProcess.Start();

            SetReceiverRunning();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Gagal menjalankan receiver:\n\n{ex.Message}",
                "ZZZ Controller",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void StopReceiver()
    {
        try
        {
            if (receiverProcess != null && !receiverProcess.HasExited)
            {
                receiverProcess.Kill(true);
                receiverProcess.WaitForExit(3000);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Gagal menghentikan receiver:\n\n{ex.Message}",
                "ZZZ Controller",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            receiverProcess?.Dispose();
            receiverProcess = null;

            SetReceiverStopped();
        }
    }

    private void SetReceiverRunning()
    {
        statusLabel.Text = "●  RECEIVER RUNNING";
        statusLabel.ForeColor = Color.FromArgb(80, 200, 120);

        receiverButton.Text = "■  HENTIKAN RECEIVER";

        phoneStatusLabel.Text = "Phone                 Waiting for USB...";
        virtualStatusLabel.Text = "Virtual Controller    Starting...";
    }

    private void SetReceiverStopped()
    {
        statusLabel.Text = "●  RECEIVER STOPPED";
        statusLabel.ForeColor = Color.FromArgb(220, 80, 80);

        receiverButton.Text = "▶  JALANKAN RECEIVER";

        phoneStatusLabel.Text = "Phone                 Disconnected";
        virtualStatusLabel.Text = "Virtual Controller    Disconnected";
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (receiverProcess != null && !receiverProcess.HasExited)
        {
            try
            {
                receiverProcess.Kill(true);
            }
            catch
            {
                // Abaikan jika process sudah berhenti.
            }
        }
    }
}
namespace Ora;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\Ora.SingleUser", out bool first);
        ApplicationConfiguration.Initialize();
        if (!first)
        {
            MessageBox.Show("Az Óra már fut. Az ablakot a tálcaikon menüjéből nyithatod meg.", "Óra");
            return;
        }
        Application.Run(new MainForm());
    }
}

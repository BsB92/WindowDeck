using System.Runtime.InteropServices;
using WindowDeck.Interop;

namespace WindowDeck.Services;

// A single COM session opens all Excel workbooks from one saved set into
// one Excel.Application instance. No Office interop package is required.
internal sealed class ExcelWorkbookSession : IDisposable
{
    private readonly object application;
    private readonly bool created;
    private bool openedWorkbook;

    private ExcelWorkbookSession(object application, bool created)
    {
        this.application = application;
        this.created = created;
    }

    public static ExcelWorkbookSession? TryCreate()
    {
        object? instance = null;
        bool created = false;
        try
        {
            if (NativeMethods.CLSIDFromProgID("Excel.Application", out Guid clsid) != 0)
            {
                return null;
            }

            if (NativeMethods.GetActiveObject(ref clsid, 0, out object running) == 0)
            {
                instance = running;
            }
            else
            {
                // No registered running instance: create one for the entire
                // restored set instead of shell-launching each file separately.
                Type? excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType is null)
                {
                    return null;
                }

                instance = Activator.CreateInstance(excelType);
                created = true;
            }

            return instance is null ? null : new ExcelWorkbookSession(instance, created);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Release(instance);
            return null;
        }
    }

    public bool TryOpen(string file, out string? errorMessage)
    {
        object? workbooks = null;
        object? workbook = null;
        int? previousSecurity = null;
        try
        {
            dynamic excel = application;
            // Programmatic Workbooks.Open otherwise enables macros by default.
            // Respect the user's normal Excel macro-security configuration.
            previousSecurity = Convert.ToInt32(excel.AutomationSecurity);
            excel.AutomationSecurity = 2; // msoAutomationSecurityByUI
            excel.Visible = true;
            workbooks = excel.Workbooks;
            dynamic collection = workbooks;
            workbook = collection.Open(file);
            openedWorkbook = true;
            errorMessage = null;
            return true;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            errorMessage = error.Message;
            return false;
        }
        finally
        {
            if (previousSecurity.HasValue)
            {
                try
                {
                    dynamic excel = application;
                    excel.AutomationSecurity = previousSecurity.Value;
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    // An unavailable Excel instance must not mask the open result.
                }
            }

            Release(workbook);
            Release(workbooks);
        }
    }

    public void Dispose()
    {
        if (created && !openedWorkbook)
        {
            try
            {
                dynamic excel = application;
                excel.Quit();
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                // Leave a failing Office server alone; never force-kill it.
            }
        }

        Release(application);
    }

    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            try
            {
                Marshal.ReleaseComObject(value);
            }
            catch (COMException)
            {
                // The Office COM server may have exited before cleanup.
            }
        }
    }
}

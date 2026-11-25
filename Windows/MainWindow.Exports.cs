namespace MentorRouletteTracker
{
    public partial class MainWindow
    {
        private string lastExportMessage = string.Empty;
        private bool exportFailed;
        private Task<string?>? csvExportTask;
        private string csvExportPath = string.Empty;
        private int csvExportCount;
        private bool csvExportAnonymous;

        private void StartCsvExport()
        {
            try
            {
                var anonymous = config.AnonymousMode || config.AnonymizeCsvExports;
                var anonymizer = anonymous ? GetPlayerAnonymizer() : null;
                var useJobs = config.AnonymousUseJobs;
                var snapshot = anonymous
                    ? CsvExporter.CaptureAnonymous(config.History.Records, (_, player) =>
                        anonymizer!.Name(player.Name, player.World, useJobs, player.Job))
                    : CsvExporter.Capture(config.History.Records);
                var prefix = anonymous ? "noumenon_anonymous_export" : "noumenon_export";
                var path = Path.Combine(exportDirectory, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.csv");
                csvExportPath = path;
                csvExportCount = snapshot.Length;
                csvExportAnonymous = anonymous;
                exportFailed = false;
                lastExportMessage = $"Exporting {csvExportCount} records...";
                csvExportTask = Task.Run(() =>
                {
                    try
                    {
                        CsvExporter.ExportSnapshot(snapshot, path);
                        return (string?)null;
                    }
                    catch (Exception ex)
                    {
                        return ex.Message;
                    }
                });
            }
            catch (Exception ex)
            {
                exportFailed = true;
                lastExportMessage = $"Couldn't export CSV: {ex.Message}";
            }
        }

        private void UpdateCsvExport()
        {
            if (csvExportTask is not { IsCompleted: true } completed)
                return;
            csvExportTask = null;
            try
            {
                var error = completed.GetAwaiter().GetResult();
                exportFailed = error != null;
                lastExportMessage = error == null
                    ? $"Exported {csvExportCount} records{(csvExportAnonymous ? " (anonymous)" : string.Empty)} to {csvExportPath}"
                    : $"Couldn't export CSV: {error}";
            }
            catch (Exception ex)
            {
                exportFailed = true;
                lastExportMessage = $"Couldn't export CSV: {ex.Message}";
            }
        }
    }
}

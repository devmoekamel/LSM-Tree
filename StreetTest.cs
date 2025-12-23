using System.Diagnostics;
using LSMStorageEngine.StorageEngine;

public static class StressTester
{
    public static async Task RunStressTest(string dataPath)
    {
        var config = new StorageEngineConfig
        {
            Path = dataPath,
            MaxMemTableSize = 1024 * 10, 
            FlushIntervalMS = 1000,
            NumberOfFilesTOCompact = 4
        };

        using var engine = new StorageEngine(config);
        int totalRecords = 50000;
        int threadCount = 10;
        var timer = Stopwatch.StartNew();

        Console.WriteLine($"Starting stress test: {totalRecords} records across {threadCount} threads...");

        await Task.WhenAll(Enumerable.Range(0, threadCount).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < totalRecords / threadCount; i++)
            {
                string key = $"key_{t}_{i}";
                byte[] val = BitConverter.GetBytes(i);
                engine.Put(key, val);
            }
        })));

        timer.Stop();
        Console.WriteLine($"Writes finished in {timer.ElapsedMilliseconds}ms");

        Console.WriteLine("Verifying data integrity...");
        int missing = 0;
        for (int t = 0; t < threadCount; t++)
        {
            for (int i = 0; i < totalRecords / threadCount; i++)
            {
                var val = engine.Get($"key_{t}_{i}");
                if (val == null) missing++;
            }
        }

        Console.WriteLine($"Verification complete. Missing: {missing}");

        Console.WriteLine("Forcing flush to disk...");
        engine.FlushToSSTable();

        var sstFiles = Directory.GetFiles(dataPath, "*.sst").Length;
        var walFiles = Directory.GetFiles(dataPath, "*.wal").Length;

        Console.WriteLine($"Files on disk: {sstFiles} SSTables, {walFiles} WALs (should be 1 active)");
    }
}
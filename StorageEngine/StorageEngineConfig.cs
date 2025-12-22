namespace LSMStorageEngine.StorageEngine;

public class StorageEngineConfig
{
    public string Path { get; set; }            
    public int MaxMemTableSize { get; set; }

    public int FlushIntervalMS { get; set; }

    public int NumberOfFilesTOCompact { get; set; }
}
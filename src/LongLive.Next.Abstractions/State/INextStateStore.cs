namespace LongLive.Next.Abstractions.State;

public interface INextStateStore
{
    bool IsAvailable { get; }

    int GetInt(string key, int defaultValue = 0);

    void SetInt(string key, int value);

    string GetString(string key, string defaultValue = "");

    void SetString(string key, string value);

    int GetInt(string group, string key, int defaultValue = 0);

    void SetInt(string group, string key, int value);

    string GetString(string group, string key, string defaultValue = "");

    void SetString(string group, string key, string value);
}

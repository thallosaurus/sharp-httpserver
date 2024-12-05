enum LogLevel {
    Error,
    Info,
    Debug,
    Verbose,
}

static class Logger {
    private static void Log(LogLevel level, string msg) {
        Console.WriteLine(msg);
    }

    static public void Error(string msg) {
        Log(LogLevel.Error, msg);
    }

    static public void Info(string msg) {
        Log(LogLevel.Info, msg);
    }

    static public void Debug(string msg) {
        Log(LogLevel.Debug, msg);
    }

    static public void Verbose(string msg) {
        Log(LogLevel.Verbose, msg);
    }
}
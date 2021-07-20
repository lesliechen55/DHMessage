using YQTrackV6.Log;

namespace YQTrack.Backend.DHgateMessage
{
    internal class LogDefine
    {
        public readonly static LogDefinition InitializeSchedule = new LogDefinition(LogLevel.Info, "ScheduleInitialized");
        public readonly static LogDefinition InitializeTask = new LogDefinition(LogLevel.Info, "TaskInitialized");
        public readonly static LogDefinition ServerStarted = new LogDefinition(LogLevel.Info, "ServerStarted");
        public readonly static LogDefinition ServerStopping = new LogDefinition(LogLevel.Info, "ServerStopping");
        public readonly static LogDefinition ServerStopped = new LogDefinition(LogLevel.Info, "ServerStopped");
        public readonly static LogDefinition WaitForExit = new LogDefinition(LogLevel.Info, "WaitForExit...");

        public readonly static LogDefinition WaitNextSchedule = new LogDefinition(LogLevel.Info, "WaitNextSchedule");

        public readonly static LogDefinition SubmitSuccess = new LogDefinition(LogLevel.Notice, "SubmitSuccess");
        public readonly static LogDefinition SubmitWarn = new LogDefinition(LogLevel.Warn, "SubmitWarn");

        public readonly static LogDefinition SubmitException = new LogDefinition(LogLevel.Error, "SubmitException");


        public readonly static LogDefinition SyncStart = new LogDefinition(LogLevel.Debug, "SyncStart");
        public readonly static LogDefinition SyncMessageList = new LogDefinition(LogLevel.Notice, "SyncMessageList");
        public readonly static LogDefinition SyncReset = new LogDefinition(LogLevel.Notice, "SyncReset");
        public readonly static LogDefinition SyncEnd = new LogDefinition(LogLevel.Debug, "SyncEnd");

        public readonly static LogDefinition Heartbeat = new LogDefinition(LogLevel.Debug, "Heartbeat");
        public readonly static LogDefinition HearbeatException = new LogDefinition(LogLevel.Error, "HearbeatException");

        public readonly static LogDefinition AddJob = new LogDefinition(LogLevel.Debug, "AddJob");
        public readonly static LogDefinition RemoveJob = new LogDefinition(LogLevel.Debug, "RemoveJob");
        public readonly static LogDefinition RemoveJobException = new LogDefinition(LogLevel.Debug, "RemoveJobException");


        public readonly static LogDefinition SyncResetError = new LogDefinition(LogLevel.Error, "SyncResetError");
        public readonly static LogDefinition TokenEmpty = new LogDefinition(LogLevel.Error, "TokenEmpty");
        public readonly static LogDefinition TokenError = new LogDefinition(LogLevel.Error, "TokenError");
        public readonly static LogDefinition SyncError = new LogDefinition(LogLevel.Error, "SyncError");
        public readonly static LogDefinition SaveError = new LogDefinition(LogLevel.Error, "SaveError");
        public readonly static LogDefinition SyncWarn = new LogDefinition(LogLevel.Warn, "SyncWarn");
    }
}

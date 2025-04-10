public enum MessageHandlingResult
{
    Discard,
    SaveToDatabase,
    RequeueWithIncrement
}

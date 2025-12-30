// IRewindable.cs
public interface IRewindable
{
    object CaptureState();              // [Serializable] struct 반환
    void RestoreState(object state);    // 만든 struct를 받아 복구
}

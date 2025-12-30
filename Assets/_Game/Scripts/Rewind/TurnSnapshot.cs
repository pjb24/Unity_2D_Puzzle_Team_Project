// TurnSnapshot.cs
///
/// 요구사항 충족 포인트
/// turnIndex
/// rewindable별 상태 저장
/// 키: Guid
/// 상태: “type + json”
///
using System;
using System.Collections.Generic;

[Serializable]
public class TurnSnapshot
{
    public int _turnIndex;
    public List<Entry> _entries = new();

    [Serializable]
    public struct Entry
    {
        public Guid _keyGuid;     // RewindKey.Guid
        public string _typeName;    // AssemblyQualifiedName
        public string _json;        // JsonUtility.ToJson(...)
    }
}

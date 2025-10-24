using UnityEngine;
using System;


/// <summary>
/// 인스펙터에 표시할 정보 메시지의 타입을 정의합니다.
/// </summary>
public enum InfoBoxType
{
    None,    // 아이콘 없음 (기본값)
    Info,    // 정보 아이콘
    Warning, // 경고 아이콘
    Error    // 에러 아이콘
}

/// <summary>
/// 인스펙터에 정보, 경고, 에러 메시지 박스를 표시하는 어트리뷰트입니다.
/// VisibleIf를 통해 특정 조건에서만 표시되도록 할 수 있습니다.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class InfoBoxAttribute : PropertyAttribute
{
    public InfoBoxType Type { get; private set; }
    public string Message { get; private set; } // 정적 메시지를 저장
    public string MessageMemberName { get; private set; } // 동적 메시지를 제공할 멤버의 이름을 저장

    /// <summary>
    /// 이 InfoBox를 표시할 조건을 담은 멤버(필드, 프로퍼티, 메서드)의 이름입니다.
    /// </summary>
    public string VisibleIf { get; set; }

    public bool IsDynamicMessage => !string.IsNullOrEmpty(MessageMemberName);

    /// <summary>
    /// InfoBox를 생성합니다.
    /// </summary>
    /// <param name="message">표시할 메시지 내용 또는 '$'로 시작하는 멤버 이름</param>
    /// <param name="type">메시지 박스의 타입 (아이콘 및 색상)</param>
    public InfoBoxAttribute(string message, InfoBoxType type = InfoBoxType.None)
    {
        // '$'로 시작하면 동적 메시지로 간주합니다.
        if (message.StartsWith("$"))
        {
            // '$' 문자를 제외한 나머지 부분을 멤버 이름으로 저장합니다.
            MessageMemberName = message.Substring(1);
        }
        else
        {
            // 그렇지 않으면 정적 메시지로 저장합니다.
            Message = message;
        }

        Type = type;
    }
}
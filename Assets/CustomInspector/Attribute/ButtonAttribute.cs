using System;

// 버튼 크기 옵션을 정의하는 열거형
public enum ButtonSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// 이 어트리뷰트를 함수에 추가하면 인스펙터에 해당 함수를 실행하는 버튼이 생깁니다.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class ButtonAttribute : Attribute
{
    public string Name { get; }
    public ButtonSize Size { get; }

    /// <summary>
    /// 버튼에 함수 이름을 표시하고, 기본 크기(Medium)를 사용합니다.
    /// </summary>
    public ButtonAttribute()
    {
        this.Name = null; // 이름이 지정되지 않았음을 명시
        this.Size = ButtonSize.Medium; // 기본 크기 설정
    }

    /// <summary>
    /// 버튼에 원하는 텍스트를 표시하고, 기본 크기(Medium)를 사용합니다.
    /// </summary>
    /// <param name="name">버튼에 표시할 텍스트</param>
    public ButtonAttribute(string name) : this() // 기본 생성자 호출
    {
        this.Name = name;
    }

    /// <summary>
    /// 버튼에 함수 이름을 표시하고, 원하는 크기를 지정합니다.
    /// </summary>
    /// <param name="size">버튼의 크기</param>
    public ButtonAttribute(ButtonSize size) : this()
    {
        this.Size = size;
    }

    /// <summary>
    /// 버튼에 원하는 텍스트와 크기를 모두 지정합니다.
    /// </summary>
    /// <param name="name">버튼에 표시할 텍스트</param>
    /// <param name="size">버튼의 크기</param>
    public ButtonAttribute(string name, ButtonSize size)
    {
        this.Name = name;
        this.Size = size;
    }
}
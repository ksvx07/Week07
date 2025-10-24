using System.Reflection; // 리플렉션 API 사용을 위해 필요 (타입 정보, 멤버 호출 등)
using UnityEditor;
using UnityEditor.UIElements; // PropertyField 등 에디터용 UI 요소 사용
using UnityEngine;
using UnityEngine.UIElements; // UIElements 핵심 API (VisualElement, Button 등)

/// <summary>
/// 모든 MonoBehaviour 컴포넌트에 대한 커스텀 에디터를 정의합니다.
/// 'true' 인자는 상속받은 자식 클래스들에도 이 에디터가 적용되도록 합니다.
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class AttributeEditor : Editor
{
    /// <summary>
    /// 인스펙터 UI를 그리는 메인 메서드입니다. IMGUI 대신 UIElements를 사용하기 위해 재정의합니다.
    /// 이 메서드가 반환하는 VisualElement가 인스펙터 창의 내용이 됩니다.
    /// </summary>
    public override VisualElement CreateInspectorGUI()
    {
        // 1. [계층] UI의 최상위 부모(루트)를 생성합니다.
        // 이 'root'는 인스PECTOR에 표시될 모든 UI 요소들을 담는 컨테이너 역할을 합니다.
        var root = new VisualElement();
        // 리플렉션을 사용하기 위해 현재 인스펙터가 그리고 있는 컴포넌트의 타입을 가져옵니다.
        var targetType = target.GetType();

        // 2. [리플렉션] 클래스 자체에 [InfoBox] 어트리뷰트가 있는지 확인하고 그립니다.
        // 클래스 레벨의 InfoBox는 특정 필드에 종속되지 않으므로 root에 직접 추가합니다.
        DrawInfoBoxes(root, targetType, target);

        // 3. [데이터] 유니티의 직렬화 시스템을 통해 인스펙터에 표시될 모든 프로퍼티를 순회합니다.
        // 'serializedObject'는 'target'의 데이터를 안전하게 다루기 위한 래퍼(Wrapper) 객체입니다.
        SerializedProperty property = serializedObject.GetIterator();
        if (property.NextVisible(true)) // 첫 번째 보이는 프로퍼티로 이동
        {
            do
            {
                // 'm_Script' 필드는 유니티가 컴포넌트 종류를 표시하는 기본 필드이므로 그대로 그려줍니다.
                if (property.name == "m_Script")
                {
                    root.Add(new PropertyField(property));
                    continue; // 다음 프로퍼티로 넘어감
                }

                // --- ▼▼▼ [ShowIf/HideIf 수정] 필드별 UI 그룹화를 위한 컨테이너 생성 ▼▼▼ ---
                // ★★★ 핵심: 필드와 그에 관련된 모든 UI(InfoBox 등)를 담을 개별 컨테이너를 생성합니다.
                // 이렇게 그룹으로 묶어야 ShowIf/HideIf를 적용했을 때 모든 요소가 함께 보이고 사라집니다.
                var propertyContainer = new VisualElement();
                root.Add(propertyContainer); // 생성된 컨테이너를 최종 UI 계층(root)에 추가합니다.

                // [리플렉션] 현재 프로퍼티의 이름과 일치하는 실제 필드 정보를 가져옵니다. (private 필드 포함)
                var field = targetType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    // 해당 필드에 [InfoBox] 어트리뷰트가 있다면, 'root'가 아닌 'propertyContainer'에 추가합니다.
                    DrawInfoBoxes(propertyContainer, field, target);

                    // [ShowIf] 어트리뷰트가 있다면, 개별 PropertyField가 아닌 전체 'propertyContainer'에 적용합니다.
                    var showIfAttr = field.GetCustomAttribute<ShowIfAttribute>();
                    if (showIfAttr != null)
                    {
                        SetupConditionalDisplay(propertyContainer, showIfAttr.Condition, target);
                    }

                    // [HideIf] 어트리뷰트도 마찬가지로 전체 'propertyContainer'에 적용합니다.
                    var hideIfAttr = field.GetCustomAttribute<HideIfAttribute>();
                    if (hideIfAttr != null)
                    {
                        SetupConditionalDisplay(propertyContainer, hideIfAttr.Condition, target, true);
                    }
                }

                // [계층] 현재 프로퍼티를 그리는 UI 요소(PropertyField)를 'propertyContainer'에 추가합니다.
                propertyContainer.Add(new PropertyField(property));
                // --- ▲▲▲ [ShowIf/HideIf 수정] 로직 끝 ▲▲▲ ---

            } while (property.NextVisible(false)); // 다음 보이는 프로퍼티로 이동
        }

        // 4. [리플렉션] 스크립트에 정의된 모든 메서드를 가져옵니다.
        // 버튼이나 메서드용 InfoBox를 그리기 위함입니다.
        var methods = targetType.GetMethods(
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly); // 부모 클래스의 메서드는 제외

        foreach (var method in methods)
        {
            // 메서드에 대한 UI는 특정 필드와 관련이 없으므로 root에 직접 추가합니다.
            DrawInfoBoxes(root, method, target);

            // [리플렉션] 메서드에 [Button] 어트리뷰트가 있는지 확인합니다.
            var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
            if (buttonAttribute != null)
            {
                // 파라미터가 있는 메서드는 버튼으로 만들 수 없으므로 건너뜁니다.
                if (method.GetParameters().Length > 0) continue;

                // 버튼에 표시될 텍스트를 결정합니다.
                string buttonText = !string.IsNullOrEmpty(buttonAttribute.Name)
                    ? buttonAttribute.Name
                    : ObjectNames.NicifyVariableName(method.Name);

                // [계층] Button UI 요소를 생성하고 클릭 시 메서드를 호출하도록 설정합니다.
                var button = new Button(() => method.Invoke(method.IsStatic ? null : target, null))
                {
                    text = buttonText
                };

                // [스타일] 어트리뷰트에 지정된 크기에 따라 버튼의 높이를 설정합니다.
                switch (buttonAttribute.Size)
                {
                    case ButtonSize.Small: button.style.height = 20; break;
                    case ButtonSize.Medium: button.style.height = 25; break;
                    case ButtonSize.Large: button.style.height = 30; break;
                }

                // [계층] 완성된 버튼을 'root'에 추가합니다.
                root.Add(button);
            }
        }

        // 5. [데이터 바인딩] 만들어진 UI 계층 구조('root')를 실제 데이터('serializedObject')와 연결합니다.
        // 이 과정을 통해 PropertyField들이 실제 변수 값을 표시하고 수정할 수 있게 되며, Undo/Redo가 가능해집니다.
        root.Bind(serializedObject);

        // 6. [렌더링] 최종적으로 완성된 UI 계층 구조를 반환하여 인스펙터 창에 그리도록 합니다.
        return root;
    }


    /// <summary>
    /// 지정된 멤버(클래스, 필드, 메서드)에 연결된 [InfoBox] 어트리뷰트를 찾아 UI로 만들어 추가하는 헬퍼 메서드입니다.
    /// ★★★ 시그니처 수정: 'root' 대신 범용적인 'parentElement'를 받도록 변경하여 재사용성을 높였습니다.
    /// </summary>
    private void DrawInfoBoxes(VisualElement parentElement, MemberInfo memberInfo, object targetInstance)
    {
        // [리플렉션] 해당 멤버에 적용된 모든 InfoBoxAttribute를 가져옵니다.
        var infoBoxAttrs = memberInfo.GetCustomAttributes<InfoBoxAttribute>(true);
        foreach (var attr in infoBoxAttrs)
        {
            // 각 어트리뷰트에 대해 InfoBox UI 요소를 생성하여 지정된 부모(parentElement)에 추가합니다.
            parentElement.Add(CreateInfoBoxElement(attr, targetInstance));
        }
    }

    /// <summary>
    /// 'VisibleIf' 조건의 참/거짓 여부를 평가하는 헬퍼 메서드입니다.
    /// </summary>
    private bool EvaluateVisibleIf(object targetObject, string memberName)
    {
        if (string.IsNullOrEmpty(memberName))
        {
            return true; // 조건이 없으면 항상 참(보임)
        }

        var targetType = targetObject.GetType();
        // 모든 접근 수준(public, private 등)의 인스턴스 멤버를 찾기 위한 플래그
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // [리플렉션] 'memberName'과 이름이 같은 '필드'를 찾고, bool 타입이면 그 값을 반환합니다.
        var field = targetType.GetField(memberName, flags);
        if (field != null && field.FieldType == typeof(bool))
        {
            return (bool)field.GetValue(targetObject);
        }

        // [리플렉션] '프로퍼티'를 찾고, bool 타입이면 그 값을 반환합니다.
        var property = targetType.GetProperty(memberName, flags);
        if (property != null && property.PropertyType == typeof(bool))
        {
            return (bool)property.GetValue(targetObject);
        }

        // [리플렉션] '메서드'를 찾고, 반환 타입이 bool이고 파라미터가 없으면 호출하여 그 결과를 반환합니다.
        var method = targetType.GetMethod(memberName, flags);
        if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
        {
            return (bool)method.Invoke(targetObject, null);
        }

        Debug.LogWarning($"[VisibleIf] 조건 멤버 '{memberName}'를 '{targetType.Name}'에서 찾을 수 없거나 bool 타입이 아닙니다.");
        return true; // 조건 멤버를 찾지 못하면 일단 보이도록 처리
    }

    /// <summary>
    /// InfoBoxAttribute의 정보를 바탕으로 실제 InfoBox UI(VisualElement)를 생성하고 스타일을 적용하는 메서드입니다.
    /// 재사용 가능한 헬퍼 메서드를 사용하여 동적 메시지와 VisibleIf 조건을 지원합니다.
    /// </summary>
    private VisualElement CreateInfoBoxElement(InfoBoxAttribute infoBoxAttribute, object targetInstance)
    {
        // [계층] InfoBox의 모든 내용을 담을 최상위 컨테이너를 생성합니다.
        var container = new VisualElement();

        // --- [스타일] 컨테이너의 레이아웃과 박스 모델을 설정합니다. ---
        // [레이아웃] 자식 요소들(아이콘, 텍스트)을 가로(Row)로 배치합니다.
        container.style.flexDirection = FlexDirection.Row;
        // [레이아웃] 자식 요소들을 세로 중앙에 정렬합니다.
        container.style.alignItems = Align.Center;
        // [박스 모델] 내부 여백(Padding)을 설정합니다.
        container.style.paddingTop = 3;
        container.style.paddingBottom = 3;
        container.style.paddingLeft = 5;
        container.style.paddingRight = 5;
        // [박스 모델] 다른 UI 요소와의 외부 간격(Margin)을 설정합니다.
        container.style.marginBottom = 2;

        // [계층] 메시지를 표시할 Label 요소를 생성합니다.
        // 먼저 어트리뷰트가 동적 메시지를 사용하는지 확인합니다.
        string initialMessage = infoBoxAttribute.IsDynamicMessage
            ? EvaluateDynamicString(targetInstance, infoBoxAttribute.MessageMemberName) // 동적이면, 지정된 멤버를 호출해 초기 텍스트를 가져옵니다.
            : infoBoxAttribute.Message;                                             // 정적이면, 어트리뷰트에 저장된 텍스트를 그대로 사용합니다.

        var messageLabel = new Label(initialMessage);
        // --- [스타일] 레이블의 텍스트 관련 스타일을 설정합니다. ---
        messageLabel.style.fontSize = 11;
        messageLabel.style.whiteSpace = WhiteSpace.Normal; // 텍스트가 길어지면 자동 줄바꿈
                                                           // [레이아웃] 공간이 부족할 때 이 요소가 줄어들도록 허용합니다.
        messageLabel.style.flexShrink = 1;

        Color backgroundColor;
        Color borderColor;
        bool isProSkin = EditorGUIUtility.isProSkin;

        if (infoBoxAttribute.Type != InfoBoxType.None)
        {
            // [계층] 아이콘을 표시할 빈 VisualElement를 생성합니다.
            var iconElement = new VisualElement();
            // --- [스타일] 아이콘의 크기, 여백 등을 설정합니다. ---
            iconElement.style.width = 16;
            iconElement.style.height = 16;
            iconElement.style.marginRight = 5; // 아이콘과 텍스트 사이 간격
                                               // [레이아웃] 공간이 부족해도 아이콘 크기는 줄어들지 않도록 설정합니다.
            iconElement.style.flexShrink = 0;
            string iconName;

            // InfoBox 타입에 따라 아이콘 종류와 배경/테두리 색상을 결정합니다.
            switch (infoBoxAttribute.Type)
            {
                case InfoBoxType.Warning:
                    iconName = "console.warnicon.sml";
                    backgroundColor = isProSkin ? new Color(0.3f, 0.26f, 0.1f, 0.5f) : new Color(1f, 0.95f, 0.6f, 0.5f);
                    borderColor = isProSkin ? new Color(0.7f, 0.55f, 0.15f) : new Color(0.6f, 0.5f, 0.2f);
                    break;
                case InfoBoxType.Error:
                    iconName = "console.erroricon.sml";
                    backgroundColor = isProSkin ? new Color(0.3f, 0.15f, 0.15f, 0.5f) : new Color(1f, 0.7f, 0.7f, 0.5f);
                    borderColor = isProSkin ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.7f, 0.4f, 0.4f);
                    break;
                case InfoBoxType.Info:
                default:
                    iconName = "console.infoicon.sml";
                    backgroundColor = isProSkin ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : new Color(0.85f, 0.85f, 0.85f, 0.5f);
                    borderColor = isProSkin ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.6f, 0.6f, 0.6f);
                    break;
            }
            // [스타일] 아이콘 이미지를 배경으로 설정합니다.
            iconElement.style.backgroundImage = EditorGUIUtility.IconContent(iconName).image as Texture2D;
            // [계층] 아이콘을 컨테이너의 자식으로 추가합니다.
            container.Add(iconElement);
        }
        else
        {
            // 아이콘이 없는 경우의 기본 색상을 설정합니다.
            backgroundColor = isProSkin ? new Color(0.2f, 0.2f, 0.2f, 0.3f) : new Color(0.85f, 0.85f, 0.85f, 0.3f);
            borderColor = isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.65f, 0.65f, 0.65f);
        }

        // [계층] 텍스트 레이블을 컨테이너의 자식으로 추가합니다.
        container.Add(messageLabel);

        // --- [스타일] 컨테이너의 최종 외형(색상, 테두리 모양)을 설정합니다. ---
        container.style.backgroundColor = backgroundColor;
        container.style.borderTopWidth = 1;
        // ... (테두리 두께 및 색상, 모서리 둥글게 설정)
        container.style.borderBottomWidth = 1;
        container.style.borderLeftWidth = 1;
        container.style.borderRightWidth = 1;
        container.style.borderTopLeftRadius = 3;
        container.style.borderTopRightRadius = 3;
        container.style.borderBottomLeftRadius = 3;
        container.style.borderBottomRightRadius = 3;
        container.style.borderTopColor = container.style.borderBottomColor =
        container.style.borderLeftColor = container.style.borderRightColor = borderColor;

        // --- [리팩토링] 동적 기능 처리를 헬퍼 메서드로 분리 ---

        // 'VisibleIf' 조건이 설정되어 있다면, 공용 헬퍼 메서드를 호출하여 보임/숨김 상태를 동적으로 제어합니다.
        // 이렇게 하면 코드 재사용성이 높아지고, 이 함수는 InfoBox '생성'에만 집중할 수 있습니다.
        if (!string.IsNullOrEmpty(infoBoxAttribute.VisibleIf))
        {
            SetupConditionalDisplay(container, infoBoxAttribute.VisibleIf, targetInstance);
        }

        // '동적 메시지' 기능이 설정되어 있다면, 텍스트 내용을 주기적으로 업데이트하는 스케줄러를 등록합니다.
        if (infoBoxAttribute.IsDynamicMessage)
        {
            // [실시간 업데이트] 0.1초(100ms)마다 주기적으로 텍스트를 업데이트합니다.
            container.schedule.Execute(() =>
            {
                string newDynamicMessage = EvaluateDynamicString(targetInstance, infoBoxAttribute.MessageMemberName);

                // 현재 텍스트와 달라졌을 때만 값을 변경하여 불필요한 UI 업데이트를 방지합니다.
                if (messageLabel.text != newDynamicMessage)
                {
                    messageLabel.text = newDynamicMessage;
                }
            }).Every(100);
        }

        // 완성된 InfoBox UI 요소를 반환합니다.
        return container;
    }

    /// <summary>
    /// VisualElement의 보임/숨김 상태를 특정 조건에 따라 동적으로 업데이트하는 헬퍼 메서드입니다.
    /// </summary>
    /// <param name="element">제어할 UI 요소</param>
    /// <param name="conditionMemberName">bool을 반환하는 조건 멤버의 이름</param>
    /// <param name="targetInstance">조건 멤버를 포함하는 인스턴스</param>
    /// <param name="invertCondition">[HideIf]처럼 조건을 반대로 적용할지 여부</param>
    private void SetupConditionalDisplay(VisualElement element, string conditionMemberName, object targetInstance, bool invertCondition = false)
    {
        if (string.IsNullOrEmpty(conditionMemberName)) return;

        // 최초 상태를 먼저 설정합니다.
        bool conditionResult = EvaluateVisibleIf(targetInstance, conditionMemberName);
        bool isVisible = invertCondition ? !conditionResult : conditionResult;
        element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

        // 스케줄러를 시작하여 실시간으로 상태를 업데이트합니다.
        element.schedule.Execute(() =>
        {
            bool currentResult = EvaluateVisibleIf(targetInstance, conditionMemberName);
            bool shouldBeVisible = invertCondition ? !currentResult : currentResult;
            var newDisplayStyle = shouldBeVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (element.style.display != newDisplayStyle)
            {
                element.style.display = newDisplayStyle;
            }
        }).Every(100);
    }

    private string EvaluateDynamicString(object targetObject, string memberName)
    {
        if (string.IsNullOrEmpty(memberName))
        {
            return null;
        }

        var targetType = targetObject.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        // 필드에서 값을 가져옵니다.
        var field = targetType.GetField(memberName, flags);
        if (field != null)
        {
            return field.GetValue(targetObject)?.ToString();
        }

        // 프로퍼티에서 값을 가져옵니다.
        var property = targetType.GetProperty(memberName, flags);
        if (property != null)
        {
            return property.GetValue(targetObject)?.ToString();
        }

        // 메서드를 호출하여 값을 가져옵니다. (파라미터 없는 메서드만 지원)
        var method = targetType.GetMethod(memberName, flags);
        if (method != null && method.GetParameters().Length == 0)
        {
            return method.Invoke(targetObject, null)?.ToString();
        }

        // 멤버를 찾지 못한 경우 에러 메시지를 반환합니다.
        return $"[InfoBox] Error: Member '{memberName}' not found.";
    }
}
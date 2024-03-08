using System;
using System.Collections.Generic;
using System.Linq;
using ABI_RC.Core.InteractionSystem;
using BTKUILib.UIObjects.Objects;

namespace BTKUILib.UIObjects.Components;

/// <summary>
/// Custom element component can be used to create custom templates and functionality that gets injected into Cohtml
/// </summary>
public class CustomElement : QMUIElement
{
    /// <summary>
    /// Called when the custom element has completed its GenerateCohtml function, you can safely use engineOn functions from here
    /// </summary>
    public Action OnElementGenerated { get; set; }

    //btkUI-Custom-[UUID] required in "id" of root
    internal ElementType ElementType;

    private string _template;
    private Page _parentPage;
    private Category _parentCategory;
    private Dictionary<string, string> _actionFunctions = new();
    private List<CustomEngineOnFunction> _engineOnFunctions = new();

    /// <summary>
    /// Custom element constructor, most parts of a custom element cannot be changed after generation
    /// </summary>
    /// <param name="template">CVR QM template code</param>
    /// <param name="elementType">Type of custom element, controls where and how the element reacts</param>
    /// <param name="parentPage">Parent page of the element, only used for on page elements</param>
    /// <param name="parentCategory">Parent category of the element, only used for in category elements</param>
    public CustomElement(string template, ElementType elementType, Page parentPage = null, Category parentCategory = null)
    {
        _template = template;
        ElementType = elementType;
        _parentPage = parentPage;
        _parentCategory = parentCategory;

        if (parentCategory != null)
            Parent = parentCategory;
        if (parentPage != null)
            Parent = parentPage;

        ElementID = "btkUI-Custom-" + UUID;

        UserInterface.CustomElements.Add(this);
    }

    /// <summary>
    /// Creates an action that can be used within Cohtml, these must be added before generation occurs!
    /// </summary>
    /// <param name="actionName">Action name, used in the h: value of a template element</param>
    /// <param name="actionCode">Javascript code to be executed on click</param>
    public void AddAction(string actionName, string actionCode)
    {
        if (_actionFunctions.ContainsKey(actionName))
        {
            BTKUILib.Log.Error("Duplicate action name given for custom element!");
            return;
        }

        _actionFunctions.Add(actionName, actionCode);
    }

    /// <summary>
    /// Remove specific action from list, this only affects the C# side, it cannot be changed on the fly
    /// </summary>
    /// <param name="actionName"></param>
    public void RemoveAction(string actionName)
    {
        if (_actionFunctions.ContainsKey(actionName))
            _actionFunctions.Remove(actionName);
    }

    /// <summary>
    /// Clears all actions from list, this only affects the C# side, it cannot be changed on the fly
    /// </summary>
    public void ClearActions()
    {
        _actionFunctions.Clear();
    }

    /// <summary>
    /// Creates a engine.on function within Cohtml, these can be called from C# with parameters
    /// All must be added before GenerateCohtml is called as they cannot be added afterwards!
    ///
    /// You will want to store the reference to this CustomEngineOnFunction so you can call it later!
    /// </summary>
    /// <param name="function">CustomEngineOnFunction object containing code and parameters</param>
    public void AddEngineOnFunction(CustomEngineOnFunction function)
    {
        if (_engineOnFunctions.Any(x => x.FunctionName == function.FunctionName))
        {
            BTKUILib.Log.Error($"Duplicate function name, {function.FunctionName} already exists in CustomElement!");
            return;
        }

        _engineOnFunctions.Add(function);
    }

    /// <summary>
    /// Remove specific function from list, this only affects the C# side, it cannot be changed on the fly
    /// </summary>
    /// <param name="functionName"></param>
    public void RemoveEngineOnFunction(string functionName)
    {
        var function = _engineOnFunctions.FirstOrDefault(x => x.FunctionName == functionName);

        if (function == null) return;

        _engineOnFunctions.Remove(function);
    }

    /// <summary>
    /// Clears all functions from list, this only affects the C# side, it cannot be changed on the fly
    /// </summary>
    public void ClearEngineOnFunctions()
    {
        _engineOnFunctions.Clear();
    }

    internal override void DeleteInternal(bool tabChange = false)
    {
        UserInterface.CustomElements.Remove(this);

        base.DeleteInternal(tabChange);
    }

    internal override void GenerateCohtml()
    {
        if (!UIUtils.IsQMReady()) return;

        if (RootPage is { IsVisible: false } && ElementType != ElementType.GlobalElement) return;

        if (!IsGenerated)
        {
            foreach(var action in _actionFunctions)
                UIUtils.GetInternalView().TriggerEvent("btkAddCustomAction", action.Key, action.Value);

            foreach (var function in _engineOnFunctions)
                UIUtils.GetInternalView().TriggerEvent("btkAddCustomEngineFunction", function.FunctionName, function.JSCode, function.Parameters.Select(x=> x.ParameterName).ToArray());

            switch (ElementType)
            {
                case ElementType.GlobalElement:
                    UIUtils.GetInternalView().TriggerEvent("btkCreateCustomGlobal", UUID, _template);
                    break;
                case ElementType.CustomPage:
                    break;
                case ElementType.OnPageElement:
                    break;
                case ElementType.InCategoryElement:
                    if (_parentCategory == null)
                        throw new Exception("Cannot create a custom element with a category when no parent category is given!");

                    UIUtils.GetInternalView().TriggerEvent("btkCreateCustomElementCategory", _parentCategory.ElementID, UUID, _template);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            OnElementGenerated?.Invoke();
        }

        base.GenerateCohtml();
    }
}

/// <summary>
/// The element type determines what should be expected for this element, as well as controls if it appears in special places like btkUI-shared
/// </summary>
public enum ElementType
{
    /// <summary>
    /// GlobalElement makes this element generate with btkUI-Shared, which is always visible
    /// </summary>
    GlobalElement,
    /// <summary>
    /// CustomPage will make this element generate as a page
    /// </summary>
    CustomPage,
    /// <summary>
    /// OnPageElement makes this element generate within a page, expects a target page to be set
    /// </summary>
    OnPageElement,
    /// <summary>
    /// InCategoryElement makes this generate within a category, expects a target category to be set
    /// </summary>
    InCategoryElement,
}
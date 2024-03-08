using System;
using System.Linq;
using ABI_RC.Core.InteractionSystem;

namespace BTKUILib.UIObjects.Objects;

/// <summary>
/// Custom engine on functions exist within Javascript, this can be used to run code that effects your custom elements
/// </summary>
public class CustomEngineOnFunction
{
    //Max 8 parameters of type T
    //Must use the correct TriggerEvent function
    internal string FunctionName { get; private set; }
    internal string JSCode { get; private set; }
    internal Parameter[] Parameters { get; private set; }

    /// <summary>
    /// Function constructor, components of this cannot be modified after generation
    /// </summary>
    /// <param name="functionName">Function name, this must be unique</param>
    /// <param name="jsCode">Javascript code to be ran within Cohtml</param>
    /// <param name="parameters">Parameters that are sent with your function from C#, there is a max of 8 supported</param>
    public CustomEngineOnFunction(string functionName, string jsCode, params Parameter[] parameters)
    {
        FunctionName = functionName;
        JSCode = jsCode;
        Parameters = parameters;
    }

    /// <summary>
    /// TriggerEvent calls your function from C# with the supplied parameters
    /// </summary>
    /// <param name="parameters">Parameters to be sent with your function</param>
    /// <exception cref="Exception">Exception thrown if you pass in to many parameters</exception>
    public void TriggerEvent(params object[] parameters)
    {
        if (!UIUtils.IsQMReady()) return;

        if (parameters.Length == 0 && Parameters.Any(x=>x.Required))
            throw new Exception($"CustomEngineOnEvent {FunctionName} TriggerEvent was attempted with 0 parameters yet there are required parameters!");

        for (int i = 0; i < Parameters.Length; i++)
        {
            var funcParam = Parameters[i];

            if (funcParam.Required && parameters.Length < i + 1)
                throw new Exception($"CustomEngineOnEvent {FunctionName} TriggerEvent was attempted with a missing required parameter!");

            var parameter = parameters[i];

            if((parameter == null && !funcParam.Nullable) || (parameter!=null && parameter.GetType() != funcParam.ParameterType))
                throw new Exception($"CustomEngineOnEvent {FunctionName} TriggerEvent was attempted with parameter that is either null or not the expected type!");
        }

        //Param check complete, pass to JS
        switch (parameters.Length)
        {
            case 0:
                UIUtils.GetInternalView().TriggerEvent(FunctionName);
                break;
            case 1:
                UIUtils.GetInternalView().TriggerEvent(FunctionName, parameters[0]);
                break;
            case 2:
                UIUtils.GetInternalView().TriggerEvent(FunctionName, parameters[0], parameters[1]);
                break;
            case 3:
                UIUtils.GetInternalView().TriggerEvent(FunctionName, parameters[0], parameters[1], parameters[2]);
                break;
            case 4:
                UIUtils.GetInternalView().TriggerEvent(FunctionName, parameters[0], parameters[1], parameters[2], parameters[3]);
                break;
            case 5:
                UIUtils.GetInternalView().TriggerEvent(FunctionName, parameters[0], parameters[1], parameters[2], parameters[3], parameters[4]);
                break;
            case 6:
                UIUtils.GetInternalView().TriggerEvent(FunctionName, parameters[0], parameters[1], parameters[2], parameters[3], parameters[4], parameters[5]);
                break;
            case 7:
                UIUtils.GetInternalView().TriggerEvent(FunctionName, parameters[0], parameters[1], parameters[2], parameters[3], parameters[4], parameters[5], parameters[6]);
                break;
            case 8:
                UIUtils.GetInternalView().TriggerEvent(FunctionName, parameters[0], parameters[1], parameters[2], parameters[3], parameters[4], parameters[5], parameters[6], parameters[7]);
                break;
            default:
                throw new Exception($"CustomEngineOnEvent {FunctionName} TriggerEvent was attempted with too many parameters! Maximum parameters is 8!");
        }
    }
}

/// <summary>
/// Parameter struct, used to validate parameters being passed in
/// </summary>
public struct Parameter
{
    internal string ParameterName { get; private set; }
    internal Type ParameterType { get; private set; }
    internal bool Required { get; private set; }
    internal bool Nullable { get; private set; }

    /// <summary>
    /// Creates a parameter to be used with your custom function
    /// </summary>
    /// <param name="parameterName">Parameter name, make sure this matches your variable name used in JS</param>
    /// <param name="parameterType">Parameter type, this is used to validate the parameter against</param>
    /// <param name="required">Sets if this parameter is required</param>
    /// <param name="nullable">Sets if this parameter can be null</param>
    public Parameter(string parameterName, Type parameterType, bool required, bool nullable)
    {
        ParameterName = parameterName;
        ParameterType = parameterType;
        Required = required;
        Nullable = nullable;
    }
}
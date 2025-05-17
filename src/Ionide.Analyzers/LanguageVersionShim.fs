namespace Ionide.Analyzers

open System
open System.Reflection
open Microsoft.FSharp.Reflection
open FSharp.Compiler.CodeAnalysis

module private ReflectionDelegates =

    let public BindingFlagsToSeeAll: BindingFlags =
        BindingFlags.Static
        ||| BindingFlags.FlattenHierarchy
        ||| BindingFlags.Instance
        ||| BindingFlags.NonPublic
        ||| BindingFlags.Public

    let createFuncArity1<'returnType> (instanceType: Type) (arg1: Type) (getterName: string) =
        let method = instanceType.GetMethod(getterName, BindingFlagsToSeeAll)

        let getFunc =
            typedefof<Func<_, _, _>>.MakeGenericType(instanceType, arg1, typeof<'returnType>)

        let delegate2 = method.CreateDelegate(getFunc)
        // TODO: Emit IL for performance
        fun (instance, arg1) -> delegate2.DynamicInvoke [| instance; arg1 |] |> unbox<bool>

    let createGetter<'returnType> (instanceType: System.Type) (getterName: string) =
        let method =
            instanceType.GetProperty(getterName, BindingFlagsToSeeAll).GetGetMethod(true)

        let getFunc =
            typedefof<Func<_, _>>.MakeGenericType(instanceType, typeof<'returnType>)

        let delegate2 = method.CreateDelegate(getFunc)
        // TODO: Emit IL for performance
        fun instance -> delegate2.DynamicInvoke [| instance |] |> unbox<bool>

/// <summary>
/// Reflection Shim around the <see href="https://github.com/dotnet/fsharp/blob/7725ddbd61ab3e5bf7e2fc35d76a0ece3903a5d9/src/Compiler/Facilities/LanguageFeatures.fs#L18">LanguageFeature</see> in FSharp.Compiler.Service
/// </summary>
type LanguageFeatureShim(langFeature: string) =
    static let LanguageFeatureTy =
        lazy (Type.GetType("FSharp.Compiler.Features+LanguageFeature, FSharp.Compiler.Service"))

    static let cases =
        lazy (FSharpType.GetUnionCases(LanguageFeatureTy.Value, ReflectionDelegates.BindingFlagsToSeeAll))

    let case =
        lazy
            (let v = cases.Value |> Array.tryFind (fun c -> c.Name = langFeature)

             v
             |> Option.map (fun x -> FSharpValue.MakeUnion(x, [||], ReflectionDelegates.BindingFlagsToSeeAll)))

    member x.Case = case.Value

    static member Type = LanguageFeatureTy.Value

/// <summary>
/// Reflection Shim around the <see href="https://github.com/dotnet/fsharp/blob/7725ddbd61ab3e5bf7e2fc35d76a0ece3903a5d9/src/Compiler/Facilities/LanguageFeatures.fs#L76">LanguageVersion</see> in FSharp.Compiler.Service
/// </summary>
type LanguageVersionShim(versionText: string) =
    static let LanguageVersionTy =
        lazy (Type.GetType("FSharp.Compiler.Features+LanguageVersion, FSharp.Compiler.Service"))

    static let ctor = lazy (LanguageVersionTy.Value.GetConstructor([| typeof<string> |]))

    static let isPreviewEnabled =
        lazy (ReflectionDelegates.createGetter<bool> LanguageVersionTy.Value "IsPreviewEnabled")

    static let supportsFeature =
        lazy
            (ReflectionDelegates.createFuncArity1<bool>
                LanguageVersionTy.Value
                LanguageFeatureShim.Type
                "SupportsFeature")

    let realLanguageVersion = ctor.Value.Invoke([| versionText |])

    member x.IsPreviewEnabled = isPreviewEnabled.Value realLanguageVersion

    member x.SupportsFeature(featureId: LanguageFeatureShim) =
        match featureId.Case with
        | None -> false
        | Some x -> supportsFeature.Value(realLanguageVersion, x)

    member x.Real = realLanguageVersion
    static member Type = LanguageVersionTy.Value

// Worth keeping commented out as they shouldn't be used until we need to find other properties/methods to support
// member x.Properties = LanguageVersionShim.Type.GetProperties(ReflectionDelegates.BindingFlagsToSeeAll)
// member x.Methods = LanguageVersionShim.Type.GetMethods(ReflectionDelegates.BindingFlagsToSeeAll)
// member x.Fields = LanguageVersionShim.Type.GetFields(ReflectionDelegates.BindingFlagsToSeeAll)

module LanguageVersionShim =

    /// <summary>Default is "latest"</summary>
    /// <returns></returns>
    let private defaultLanguageVersion = lazy (LanguageVersionShim("latest"))

    /// <summary>Tries to parse out "--langversion:" from OtherOptions if it can't find it, returns defaultLanguageVersion</summary>
    /// <param name="fpo">The FSharpProjectOptions to use</param>
    /// <returns>A LanguageVersionShim from the parsed "--langversion:" or defaultLanguageVersion </returns>
    let fromFSharpProjectOptions (fpo: FSharpProjectOptions) =
        fpo.OtherOptions
        |> Array.tryFind (fun x -> x.StartsWith("--langversion:", StringComparison.Ordinal))
        |> Option.map (fun x -> x.Split(":")[1])
        |> Option.map (fun x -> LanguageVersionShim(x))
        |> Option.defaultWith (fun () -> defaultLanguageVersion.Value)

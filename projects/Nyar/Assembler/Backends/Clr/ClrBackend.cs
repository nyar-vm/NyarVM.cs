using System.Diagnostics;
using System.Text;
using Nyar.Types.Externals;
using Nyar.Types.Targets;
using Std.Data.Binary.Clr;
using Std.Data.Binary.Clr.Data;
using Std.Data.Text.Diagnostics;

namespace Nyar.Assembler.Backends.Clr;

/// <summary>
///     CLR 鍚庣锛屽疄鐨処CodeGenBackend 鎺ュ彛锛屽皢 Nyar 妯″潡缂栬瘧鐨凜LR/MSIL 绋嬪簭闆嗙殑///銆?///
/// </summary>
public sealed partial class ClrBackend : IStandardBackend<ClrModuleData>
{
    /// <inheritdoc />
    public string name => "CLR";

    /// <inheritdoc />
    public IReadOnlyList<TargetArch> supported_archs => [TargetArch.clr];

    /// <inheritdoc />
    public OutputSpec<ClrModuleData> compile(GenerateModule module, CompilationOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var fileExtension = ".exe";
        // 鍏ュ彛鍑芥暟鍚嶄紭鍏堜綔涓鸿緭鍑哄悕锛堝 legion.legion -> legion锛夛紝鍚﹀垯鐢ㄦā鍧楀悕
        // options.entry_function_name 鐢ㄤ簬澶氬叆鍙ｆā鍧楀満鏅紝鏄惧紡鎸囧畾缂栬瘧鐩爣
        var entryFunction = resolve_entry_function(module, options);
        var outputName = compute_output_name(entryFunction?.name, module.name);
        Console.WriteLine($"[ClrBackend] build_clr_module start: module={module.name}, output={outputName}");
        var clrModule = build_clr_module(module, fileExtension, entryFunction, outputName);
        Console.WriteLine($"[ClrBackend] build_clr_module completed in {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Restart();
        var assets = new List<AssemblerAsset>();
        if (options.generate_msil)
        {
            var msil = build_msil_text(clrModule);
            Console.WriteLine($"[ClrBackend] build_msil_text completed in {stopwatch.ElapsedMilliseconds} ms");
            assets.Add(new AssemblerAsset
            {
                name = $"{outputName}.msil",
                content = Encoding.UTF8.GetBytes(msil + Environment.NewLine),
                media_type = "text/plain"
            });
        }

        return new OutputSpec<ClrModuleData>
        {
            data = clrModule,
            file_extension = fileExtension,
            media_type = "application/octet-stream",
            output_name = outputName,
            assets = assets
        };
    }

    /// <inheritdoc />
    OutputSpec ICodeGenBackend.compile(GenerateModule module, CompilationOptions options)
    {
        return compile(module, options);
    }

    /// <inheritdoc />
    public bool validate(GenerateModule module, out List<Diagnostic> diagnostics)
    {
        diagnostics = [];
        if (module.has_witness_dispatch)
        {
            diagnostics.Add(new Diagnostic(
                default,
                "CLR 后端当前不支持 `trait/imply` 的 witness 分派，请改用 `NyarVM` 目标，或先完成静态单态化。",
                DiagnosticSeverity.error));
            return false;
        }

        if (!validate_branch_labels(module, diagnostics)) return false;
        return true;
    }

    private static ClrModuleData build_clr_module(GenerateModule module, string fileExtension,
        GenerateFunction? entryFunction, string outputName)
    {
        var stageStopwatch = Stopwatch.StartNew();
        var nonClrFunctions = new List<(int Index, GenerateFunction Function)>();
        var clrAttributeFunctions =
            new List<(int Index, GenerateFunction Function, ExternalClrMethodImport ImportLink)>();
        var otherExternalNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < module.functions.Count; i++)
        {
            var function = module.functions[i];
            // 调试：打印所有包含 console 的函数
            if (function.name.Contains("console", StringComparison.OrdinalIgnoreCase) ||
                function.name.Contains("__console", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine(
                    $"[ClrBackend] classify: checking function: {function.name}, has_external_import={function.has_external_import}, link_count={function.external_import_links.Count}");
            if (function.try_get_external_import_link(CallingConvention.clr, out var externalImportLink) &&
                externalImportLink is ExternalClrMethodImport clrImportLink)
            {
                Console.WriteLine(
                    $"[ClrBackend] classify: CLR external import: {function.name} -> {clrImportLink.assembly_name}::{clrImportLink.type_full_name}.{clrImportLink.method_name}");
                clrAttributeFunctions.Add((i, function, clrImportLink));
                continue;
            }

            if (function.has_external_import)
            {
                Console.WriteLine($"[ClrBackend] classify: other external import: {function.name}");
                otherExternalNames.Add(function.name);
                var shortName = get_short_function_name(function.name);
                if (!string.Equals(shortName, function.name, StringComparison.Ordinal))
                    otherExternalNames.Add(shortName);
                continue;
            }

            nonClrFunctions.Add((i, function));
        }

        Console.WriteLine($"[ClrBackend] classify functions completed in {stageStopwatch.ElapsedMilliseconds} ms");
        var methodTokenMap = new Dictionary<string, uint>(StringComparer.Ordinal);
        var externalRefs = new List<ClrExternalMethodRef>();
        var externalRefTokenMap = new Dictionary<string, uint>(StringComparer.Ordinal);
        var externalCtorFunctionNames = new HashSet<string>(StringComparer.Ordinal);
        var externalTypeRefs = new List<ClrExternalTypeRef>();
        var typeRefTokenMap = new Dictionary<string, uint>(StringComparer.Ordinal);
        var newObjectTokenMap = new Dictionary<string, uint>(StringComparer.Ordinal);
        var fieldTokenMap = new Dictionary<string, uint>(StringComparer.Ordinal);
        var userStrings = new List<string>();
        var userStringTokens = new Dictionary<string, uint>(StringComparer.Ordinal);
        for (var i = 0; i < nonClrFunctions.Count; i++)
        {
            var token = 0x06000001u + (uint)i;
            var functionName = nonClrFunctions[i].Function.name;
            methodTokenMap[functionName] = token;
            var shortName = get_short_function_name(functionName);
            if (!string.Equals(shortName, functionName, StringComparison.Ordinal))
                methodTokenMap.TryAdd(shortName, token);
        }

        var memberRefTokenBase = 0x0A000001u;
        var memberRefIndex = 0;
        for (var i = 0; i < clrAttributeFunctions.Count; i++)
        {
            var (_, function, importLink) = clrAttributeFunctions[i];
            var assemblyName = importLink.assembly_name;
            var methodName = importLink.method_name;
            var namedType = (ClrNamedType)importLink.clr_type;
            var typeFullName = namedType.full_name;
            var typeNamespace = namedType.@namespace;
            var shortTypeName = namedType.name;
            var methodSignature = build_import_signature(module, function, methodName);
            externalRefs.Add(new ClrExternalMethodRef
            {
                assembly_name = assemblyName,
                type_full_name = typeFullName,
                type_namespace = typeNamespace,
                type_name = shortTypeName,
                method_name = methodName,
                method_signature = methodSignature
            });
            var token = memberRefTokenBase + (uint)memberRefIndex;
            var extFuncName = function.name;
            externalRefTokenMap[extFuncName] = token;
            var shortExtName = get_short_function_name(extFuncName);
            if (!string.Equals(shortExtName, extFuncName, StringComparison.Ordinal))
                externalRefTokenMap.TryAdd(shortExtName, token);
            if (string.Equals(methodName, ".ctor", StringComparison.Ordinal))
            {
                externalCtorFunctionNames.Add(extFuncName);
                externalCtorFunctionNames.Add(shortExtName);
            }

            memberRefIndex++;
        }

        Console.WriteLine(
            $"[ClrBackend] register explicit clr refs completed in {stageStopwatch.ElapsedMilliseconds} ms");
        register_builtin_external_method_refs(module, externalRefs, externalRefTokenMap, externalCtorFunctionNames,
            ref memberRefIndex);
        // 注册内置外部类型引用，供 `unbox.any` 等需要 `TypeRef` token 的指令使用。
        register_builtin_external_type_refs(module, externalTypeRefs, typeRefTokenMap);
        Console.WriteLine(
            $"[ClrBackend] register builtin type refs completed in {stageStopwatch.ElapsedMilliseconds} ms");
        // 鎵弿鎵€鏈夊嚱鏁颁腑鐨?new_object/get_field/set_field 鎸囦护锛屾敹闆嗙被鍨嬪拰瀛楁寮曠敤
        var typeFields =
            collect_oop_references(module, externalRefs, newObjectTokenMap, fieldTokenMap, ref memberRefIndex);
        Console.WriteLine($"[ClrBackend] collect_oop_references completed in {stageStopwatch.ElapsedMilliseconds} ms");
        // 鎵弿鎵€鏈夊嚱鏁帮紝鏀堕泦琚暟缁勬搷浣滀娇鐢ㄧ殑瀛楁锛岀敤浜庡尯鍒?[] 绌烘暟缁勫拰 null
        var arrayFieldKeys = collect_array_field_names(module, fieldTokenMap);
        Console.WriteLine(
            $"[ClrBackend] collect_array_field_names completed in {stageStopwatch.ElapsedMilliseconds} ms");
        var userTypeDefs = new List<ClrTypeDef>();
        var fieldDefTokenBase = 0x04000001u;
        var fieldDefIndex = 0u;
        // 娉ㄥ唽 System.Object::.ctor() 澶栭儴寮曠敤锛屼緵鐢ㄦ埛瀹氫箟绫诲瀷鐨?.ctor 璋冪敤
        var systemObjectCtorSignature = new byte[] { 0x20, 0x00, 0x01 }; // HAS_THIS, 0 params, void return
        externalRefs.Add(new ClrExternalMethodRef
        {
            assembly_name = "System.Runtime",
            type_full_name = "System.Object",
            type_namespace = "System",
            type_name = "Object",
            method_name = ".ctor",
            method_signature = systemObjectCtorSignature
        });
        var systemObjectCtorToken = memberRefTokenBase + (uint)memberRefIndex;
        memberRefIndex++;
        foreach (var (typeName, fieldNames) in typeFields)
        {
            var fieldDefs = new List<ClrFieldDef>();
            foreach (var fieldName in fieldNames)
            {
                fieldDefs.Add(new ClrFieldDef
                {
                    name = fieldName,
                    flags = ClrFieldAttributes.@public,
                    signature_index = 0
                });
                // 鏇存柊 fieldTokenMap 涓殑鍗犱綅 Token 涓哄疄闄?FieldDef Token
                var qualifiedKey = $"{typeName}.{fieldName}";
                if (fieldTokenMap.TryGetValue(qualifiedKey, out _))
                    fieldTokenMap[qualifiedKey] = fieldDefTokenBase + fieldDefIndex;
                fieldDefIndex++;
            }

            // 涓烘瘡涓敤鎴峰畾涔夌被鍨嬬敓鎴愰粯璁ゆ瀯閫犲櫒锛歭darg.0 + call System.Object::.ctor() + ret
            var ctorInstructions = new List<ClrInstruction>
            {
                new() { opcode = ClrOpcode.ldarg_0 },
                new() { opcode = ClrOpcode.call, operand = new ClrTokenOperand { value = systemObjectCtorToken } },
                new() { opcode = ClrOpcode.ret }
            };
            var ctorMethod = new ClrMethodDef
            {
                name = ".ctor",
                flags = (ClrMethodAttributes)0x1886,
                instructions = assign_offsets(ctorInstructions),
                max_stack = 1,
                signature = [0x20, 0x00, 0x01], // HAS_THIS, 0 params, void return
                local_variable_types = []
            };
            userTypeDefs.Add(new ClrTypeDef
            {
                name = typeName,
                @namespace = string.Empty,
                flags = ClrTypeAttributes.@public,
                extends_index = 0,
                fields = fieldDefs,
                methods = [ctorMethod],
                properties = [],
                events = []
            });
        }

        // 灏嗘湰鍦扮被鍨嬬殑 new_object 鏄犲皠浠庡崰浣?Token 淇涓烘湰鍦?MethodDef
        // MethodDef token 椤哄簭锛歯onClrFunctions 鈫?entry wrapper锛堝鏈夛級 鈫?userTypeDefs 鐨?.ctor
        var needsEntryWrapper = entryFunction is not null && needs_entry_wrapper(entryFunction);
        var ctorMethodDefOffset = nonClrFunctions.Count + (needsEntryWrapper ? 1 : 0);
        for (var i = 0; i < userTypeDefs.Count; i++)
        {
            var methodDefToken = 0x06000001u + (uint)(ctorMethodDefOffset + i);
            newObjectTokenMap[userTypeDefs[i].name] = methodDefToken;
        }

        Console.WriteLine($"[ClrBackend] build user type defs completed in {stageStopwatch.ElapsedMilliseconds} ms");
        var methods = new List<ClrMethodDef>(nonClrFunctions.Count);
        foreach (var tuple in nonClrFunctions)
        {
            var functionStopwatch = Stopwatch.StartNew();
            Console.WriteLine($"[ClrBackend] build_method start: {tuple.Function.name}");
            try
            {
                methods.Add(build_method(
                    tuple.Function, module, methodTokenMap, externalRefTokenMap,
                    externalCtorFunctionNames,
                    typeRefTokenMap,
                    newObjectTokenMap, fieldTokenMap,
                    entryFunction is not null
                    && !needsEntryWrapper
                    && string.Equals(tuple.Function.name, entryFunction.name, StringComparison.Ordinal),
                    userStrings, userStringTokens, arrayFieldKeys));
                Console.WriteLine(
                    $"[ClrBackend] build_method completed: {tuple.Function.name} in {functionStopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ClrBackend] build_method FAILED: {tuple.Function.name}");
                Console.Error.WriteLine($"  Exception: {ex.GetType().FullName}: {ex.Message}");
                Console.Error.WriteLine($"  StackTrace:\n{ex.StackTrace}");
                if (ex.InnerException is not null)
                {
                    Console.Error.WriteLine(
                        $"  InnerException: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                    Console.Error.WriteLine($"  InnerStackTrace:\n{ex.InnerException.StackTrace}");
                }

                throw;
            }
        }

        Console.WriteLine($"[ClrBackend] build all methods completed in {stageStopwatch.ElapsedMilliseconds} ms");
        var dynamicFieldTokenFloor = fieldDefTokenBase + fieldDefIndex;
        var hasDynamicFields = fieldTokenMap.Values.Any(v => v >= dynamicFieldTokenFloor);
        var oldFieldTokens = hasDynamicFields
            ? new Dictionary<string, uint>(fieldTokenMap, StringComparer.Ordinal)
            : null;
        if (hasDynamicFields)
        {
            // 灏嗗姩鎬佸瓧娈佃ˉ鍏?typeFields
            // 娉ㄦ剰锛氳烦杩囪鎿﹂櫎绫诲瀷锛坋xternal_ref/any/object锛夌殑閿紝
            foreach (var kv in fieldTokenMap)
            {
                if (kv.Value < dynamicFieldTokenFloor) continue;
                var lastDot = kv.Key.LastIndexOf('.');
                if (lastDot < 0) continue;
                var typeName = kv.Key[..lastDot];
                var fieldName = kv.Key[(lastDot + 1)..];
                if (is_erased_object_type(typeName)) continue;
                if (!typeFields.ContainsKey(typeName))
                    typeFields[typeName] = [];
                if (!typeFields[typeName].Contains(fieldName))
                    typeFields[typeName].Add(fieldName);
            }

            // 閲嶅缓 userTypeDefs 鍜?fieldTokenMap
            fieldDefIndex = 0u;
            userTypeDefs.Clear();
            foreach (var (typeName, fieldNames) in typeFields)
            {
                var fieldDefs = new List<ClrFieldDef>();
                foreach (var fieldName in fieldNames)
                {
                    fieldDefs.Add(new ClrFieldDef
                    {
                        name = fieldName,
                        flags = ClrFieldAttributes.@public,
                        signature_index = 0
                    });
                    var qualifiedKey = $"{typeName}.{fieldName}";
                    if (fieldTokenMap.ContainsKey(qualifiedKey))
                        fieldTokenMap[qualifiedKey] = fieldDefTokenBase + fieldDefIndex;
                    fieldDefIndex++;
                }

                // 榛樿鏋勯€犲櫒锛歭darg.0 + call System.Object::.ctor() + ret
                var ctorInstructions = new List<ClrInstruction>
                {
                    new() { opcode = ClrOpcode.ldarg_0 },
                    new() { opcode = ClrOpcode.call, operand = new ClrTokenOperand { value = systemObjectCtorToken } },
                    new() { opcode = ClrOpcode.ret }
                };
                userTypeDefs.Add(new ClrTypeDef
                {
                    name = typeName,
                    @namespace = string.Empty,
                    flags = ClrTypeAttributes.@public,
                    extends_index = 0,
                    fields = fieldDefs,
                    methods =
                    [
                        new ClrMethodDef
                        {
                            name = ".ctor",
                            flags = (ClrMethodAttributes)0x1886,
                            instructions = assign_offsets(ctorInstructions),
                            max_stack = 1,
                            signature = [0x20, 0x00, 0x01],
                            local_variable_types = []
                        }
                    ],
                    properties = [],
                    events = []
                });
            }

            for (var i = 0; i < userTypeDefs.Count; i++)
            {
                var methodDefToken = 0x06000001u + (uint)(ctorMethodDefOffset + i);
                newObjectTokenMap[userTypeDefs[i].name] = methodDefToken;
            }
            // 褰撳瓨鍦ㄥ姩鎬佸瓧娈垫椂锛宐uild_method锛堝湪涓婃柟宸茶皟鐢級浣跨敤鐨勫瓧娈?Token
            // 鏄?collect_oop_references / 绗竴娆″垎閰嶉樁娈电殑鍗犱綅 Token锛?            // 鑰屼笂闈㈢殑閲嶅缓閫昏緫浼氶噸鏂板垎閰?FieldDef Token銆?            // 鍥犳闇€瑕侀亶鍘嗘墍鏈夊凡鐢熸垚鏂规硶鐨?IL锛屽皢 stfld/ldfld 鎸囦护涓殑
            // 鏃у瓧娈?Token 閲嶆槧灏勪负 fieldTokenMap 涓殑鏈€缁?Token銆?            remap_field_tokens_in_methods(methods, oldFieldTokens, fieldTokenMap);
        }

        Console.WriteLine($"[ClrBackend] dynamic field remap completed in {stageStopwatch.ElapsedMilliseconds} ms");
        var entryPointToken = 0u;
        if (entryFunction is not null && methodTokenMap.TryGetValue(entryFunction.name, out var entryToken))
        {
            if (needsEntryWrapper)
            {
                var wrapper = build_entry_wrapper(entryFunction, entryFunction.name, methodTokenMap);
                var wrapperIndex = methods.Count;
                var wrapperToken = 0x06000001u + (uint)wrapperIndex;
                methods.Add(wrapper);
                entryPointToken = wrapperToken;
            }
            else
            {
                entryPointToken = entryToken;
            }
        }

        // 妯″潡鍚嶄娇鐢ㄥ叆鍙ｅ嚱鏁板悕锛屽叆鍙ｅ嚱鏁板悕缁?compute_output_name 宸插鐞嗗ソ
        var moduleTypeName = outputName;
        var moduleTypeDef = new ClrTypeDef
        {
            name = "<Module>",
            @namespace = string.Empty,
            flags = ClrTypeAttributes.not_public,
            methods = methods
        };
        var moduleTypes = new List<ClrTypeDef> { moduleTypeDef };
        moduleTypes.AddRange(userTypeDefs);
        foreach (var kv in fieldTokenMap)
        {
        }

        foreach (var td in userTypeDefs)
        {
        }

        Console.WriteLine($"[ClrBackend] finalize module data completed in {stageStopwatch.ElapsedMilliseconds} ms");
        return new ClrModuleData
        {
            module_name = $"{moduleTypeName}{fileExtension}",
            version = "v4.0.30319",
            target_runtime_version = new Version(10, 0, 0, 0),
            clr_directory = new ClrDirectoryData
            {
                cb = ClrConstants.clr_directory_size,
                major_runtime_version = 2,
                minor_runtime_version = 5,
                flags = (uint)ClrDirectoryFlags.il_only,
                entry_point = entryPointToken
            },
            methods = methods,
            types = moduleTypes,
            external_method_refs = externalRefs,
            external_type_refs = externalTypeRefs,
            user_strings = userStrings,
            field_token_map = fieldTokenMap
        };
    }

    /// <summary>
    ///     鍒ゆ柇鍏ュ彛鍑芥暟鏄惁闇€瑕?CLR 鍏ュ彛鍖呰鍣ㄣ€?    ///     CLR 鍙墽琛岀洰鏍囩粺涓€閫氳繃 `Main` 鍖呰鍣ㄨ繘鍏ワ紝閬垮厤杩愯鏃跺叆鍙ｇ害鏉熸硠婕忓埌璇█鍑芥暟绛惧悕銆?    ///
    /// </summary>
    private static bool needs_entry_wrapper(GenerateFunction entryFunction)
    {
        _ = entryFunction;
        return true;
    }

    /// <summary>
    ///     鐢熸垚 CLR 鍏ュ彛鍖呰鍣ㄣ€?    ///     褰撳叆鍙ｅ嚱鏁版湁鏁扮粍绫诲瀷鍙傛暟鏃讹紝鐢熸垚 `static void Main(string[] args)` 骞朵紶閫掔湡瀹炲懡浠よ鍙傛暟锛?    ///
    ///     鍚﹀垯鐢熸垚 `static void Main()` 骞跺帇鍏ラ粯璁ゅ€笺€?    ///
    /// </summary>
    private static ClrMethodDef build_entry_wrapper(
        GenerateFunction entryFunction,
        string entryFunctionName,
        IReadOnlyDictionary<string, uint> methodTokenMap)
    {
        var instructions = new List<ClrInstruction>();
        var hasArgsParam = entryFunction.parameters.Count == 1
                           && string.Equals(entryFunction.parameters[0].name, "args",
                               StringComparison.OrdinalIgnoreCase)
                           && (is_array_parameter_type(entryFunction.parameters[0].type_ref)
                               || entryFunction.parameters[0].type_ref.kind == GenerateTypeKind.external_ref);
        foreach (var parameter in entryFunction.parameters)
            if (hasArgsParam && parameter == entryFunction.parameters[0])
                // 绗竴涓弬鏁版槸鏁扮粍绫诲瀷锛屼紶閫?Main 鐨?string[] args
                instructions.Add(new ClrInstruction { opcode = ClrOpcode.ldarg_0 });
            else
                emit_entry_wrapper_default_argument(instructions, parameter.type_ref);
        if (methodTokenMap.TryGetValue(entryFunctionName, out var entryToken))
            instructions.Add(new ClrInstruction
            {
                opcode = ClrOpcode.call,
                operand = new ClrTokenOperand { value = entryToken }
            });
        append_entry_wrapper_return_normalization(instructions, entryFunction.return_type_ref);
        instructions.Add(new ClrInstruction { opcode = ClrOpcode.ret });
        var wrapperReturnType = entryFunction.return_type_ref.is_void_like
            ? GenerateTypeReference.@void
            : GenerateTypeReference.i32;
        var paramCount = (ushort)entryFunction.parameters.Count;
        return new ClrMethodDef
        {
            name = "Main",
            signature = build_entry_wrapper_signature(wrapperReturnType, hasArgsParam),
            flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static | ClrMethodAttributes.hide_by_sig,
            max_stack = (ushort)Math.Max(paramCount + 1, 8),
            local_variable_types = [],
            instructions = assign_offsets(instructions)
        };
    }

    /// <summary>
    ///     鍒ゆ柇鍙傛暟绫诲瀷鏄惁涓烘暟缁勭被鍨嬶紙濡?[utf8]銆乕i32] 绛夛級銆?    ///
    /// </summary>
    private static bool is_array_parameter_type(GenerateTypeReference type)
    {
        return type.is_array_family;
    }

    /// <summary>
    ///     鏋勫缓 CLR 鍏ュ彛鍖呰鍣ㄧ鍚嶃€?    ///     褰?hasArgsParam 涓?true 鏃讹紝绛惧悕鍖呭惈涓€涓?string[] 鍙傛暟銆?    ///
    /// </summary>
    private static byte[] build_entry_wrapper_signature(GenerateTypeReference returnType, bool hasArgsParam = false)
    {
        if (!hasArgsParam) return [0x00, 0x00, map_element_type(returnType)];
        return [0x00, 0x01, map_element_type(returnType), 0x1D, 0x0E];
    }

    private static GenerateFunction? resolve_entry_function(GenerateModule module, CompilationOptions options)
    {
        // 鏄惧紡鎸囧畾鍏ュ彛鏃讹紝浼樺厛绮剧‘鍖归厤
        if (!string.IsNullOrWhiteSpace(options.entry_function_name))
        {
            foreach (var export in module.exports.Where(item => item.kind == GenerateExportKind.function))
                if (try_get_exported_function(module, export, out var entryFunction)
                    && string.Equals(entryFunction!.name, options.entry_function_name, StringComparison.Ordinal))
                    return entryFunction;
            // 鎸夊嚱鏁板悕鐩存帴鏌ユ壘锛堟湭閫氳繃 export 澹版槑鏃讹級
            var directMatch = module.functions.FirstOrDefault(f =>
                string.Equals(f.name, options.entry_function_name, StringComparison.Ordinal));
            if (directMatch is not null) return directMatch;
        }

        foreach (var export in
                 module.exports.Where(item => item is { kind: GenerateExportKind.function, name: "main" }))
            if (try_get_exported_function(module, export, out var entryFunction))
                return entryFunction;
        foreach (var export in module.exports.Where(item => item.kind == GenerateExportKind.function))
            if (try_get_exported_function(module, export, out var entryFunction))
                return entryFunction;
        return module.functions.FirstOrDefault(function =>
                   string.Equals(function.name, "main", StringComparison.Ordinal))
               ?? module.functions.FirstOrDefault();
    }

    /// <summary>
    ///     解析结构化类型在 CLR 下对应的宿主类型全名。
    /// </summary>
    private static string resolve_clr_type_full_name(GenerateModule module, GenerateTypeReference typeName)
    {
        if (module.try_get_type_external_import_link(typeName, CallingConvention.clr, out var externalImportLink) &&
            externalImportLink is ExternalClrTypeImport clrImportLink)
            return clrImportLink.clr_type is ClrNamedType namedType
                ? namedType.full_name
                : clrImportLink.type_full_name;
        return typeName.kind switch
        {
            GenerateTypeKind.utf8 or GenerateTypeKind.utf16 => "System.String",
            _ => typeName.display_name
        };
    }

    /// <summary>
    ///     将结构化类型映射为 CLR 签名使用的规范类型引用。
    /// </summary>
    private static GenerateTypeReference resolve_clr_signature_type_name(
        GenerateModule module,
        GenerateTypeReference typeName)
    {
        var clrTypeFullName = resolve_clr_type_full_name(module, typeName);
        return clrTypeFullName switch
        {
            "System.String" => GenerateTypeReference.utf16,
            "System.Object" => GenerateTypeReference.@object,
            _ => typeName
        };
    }

    private static bool try_get_exported_function(
        GenerateModule module,
        GenerateModuleExport export,
        out GenerateFunction? function)
    {
        if (export.function_index >= 0 && export.function_index < module.functions.Count)
        {
            function = module.functions[export.function_index];
            return true;
        }

        function = null;
        return false;
    }

    /// <summary>
    ///     閬嶅巻鎵€鏈夊凡鐢熸垚鏂规硶鐨?IL锛屽皢 stfld/ldfld 鎸囦护涓殑鏃у瓧娈?Token
    ///     閲嶆槧灏勪负 <paramref name="newFieldTokens" /> 涓殑鏈€缁?Token銆?    ///     褰?<paramref name="oldFieldTokens" /> 涓?null
    ///     鏃剁洿鎺ヨ繑鍥炪€?    ///
    /// </summary>
    private static void remap_field_tokens_in_methods(
        List<ClrMethodDef> methods,
        Dictionary<string, uint>? oldFieldTokens,
        IReadOnlyDictionary<string, uint> newFieldTokens)
    {
        if (oldFieldTokens is null) return;
        // 鏋勫缓 鏃oken 鈫?鏂癟oken 鐨勬槧灏勮〃
        var tokenRemap = new Dictionary<uint, uint>();
        foreach (var (key, oldToken) in oldFieldTokens)
            if (newFieldTokens.TryGetValue(key, out var newToken) && newToken != oldToken)
                tokenRemap[oldToken] = newToken;
        if (tokenRemap.Count == 0) return;
        // 閬嶅巻鎵€鏈夋柟娉曠殑鎵€鏈夋寚浠わ紝閲嶆槧灏?stfld/ldfld 鐨勫瓧娈?Token
        var remappedCount = 0;
        foreach (var method in methods)
        {
            if (method.instructions is null) continue;
            foreach (var instr in method.instructions)
                // stfld/ldfld/ldsfld/stsfld 鐨?operand 鏄?ClrTokenOperand
                if (instr.opcode is ClrOpcode.stfld or ClrOpcode.ldfld or ClrOpcode.ldsfld or ClrOpcode.stsfld &&
                    instr.operand is ClrTokenOperand tokenOperand)
                    if (tokenRemap.TryGetValue(tokenOperand.value, out var newToken))
                    {
                        tokenOperand.value = newToken;
                        remappedCount++;
                    }
        }
    }
}
